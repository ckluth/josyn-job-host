using System.Reflection;
using System.Text;
using JOSYN.Foundation.PropertyBag;
using JOSYN.Foundation.ResultPattern;
using JOSYN.Jap.Shared.Contract;
using JOSYN.Jap.JobHost.Attributes;

namespace JOSYN.Jap.JobHost;

//public static class JobInvoker<T> where T : class
//{
//    public static ArgumentsComparer<T>? ConditionalParallelExecutionAllowed { get; set; }
//}

internal static class JobInvoker
{
    internal static async Task<Result> InvokeJob(IJosynApplicationProtocol japClient, Type? entrypointType = null)
    {
        try
        {
            var findEntrypointAssembly = FindEntryPointAssembly(entrypointType);
            if (!findEntrypointAssembly.Succeeded)
                return Result.Propagate(findEntrypointAssembly.ToResult());

            return await InvokeJob(japClient, findEntrypointAssembly.Value.GetExportedTypes());
        }
        catch (Exception ex) { return ex; }
    }

    internal static async Task<Result> InvokeJob(IJosynApplicationProtocol japClient, IEnumerable<Type> types)
    {
        try
        {
            //
            // Einsprungs-Methode finden
            //
            var findJobFunc = FindJobFunction(types);
            if (!findJobFunc.Succeeded)
                return Result.Propagate(findJobFunc.ToResult());

            //
            // Aufrufsargumente erzeugen
            //
            var getInvocationArgs = await CreateInvocationArguments(findJobFunc.Value, japClient);
            if (!getInvocationArgs.Succeeded)
                return Result.Propagate(getInvocationArgs.ToResult());
            var invocationArgs = getInvocationArgs.Value;

            //
            // Hier kommt endlich der Aufruf der Job-Methode
            //
            object? res = null;
            try
            {
                res = findJobFunc.Value.Invoke(null, invocationArgs);
            }
            catch (Exception ex)
            {
                return Result.Error("Der Job hat eine unbehandelte Exception durchgelassen.", ex);
            }

            //
            // Jetzt noch das Result verarbeiten
            //
            var processJobResult = await ProcessJobResult(res, findJobFunc.Value, japClient);
            return !processJobResult.Succeeded ? Result.Propagate(processJobResult) : Result.Success;
        }
        catch (Exception ex) { return ex; }
    }

    #region private

    private static async Task<Result> ProcessJobResult(object? jobResult, MethodInfo func, IJosynApplicationProtocol japClient)
    {
        var resultType = func.ReturnType;
        var nullabilityInfo = new NullabilityInfoContext().Create(func.ReturnParameter);
        var hasNullableAnnotation = nullabilityInfo.WriteState == NullabilityState.Nullable ||
                                    nullabilityInfo.ReadState == NullabilityState.Nullable;

        if (resultType == typeof(void) || (jobResult == null && hasNullableAnnotation))
            return Result.Success;

        if (jobResult == null)
            return Result.Error("Job hat unerwartet NULL zurückgegeben.");

        var getResultAsString = PropertyBag.Serialize(jobResult, resultType, IniDictionarySerializer.Serialize);

        if (!getResultAsString.Succeeded)
            return Result.Propagate(getResultAsString.ToResult());

        var res = await japClient.PutRawResult(getResultAsString.Value);

#if DEBUG
        // ReSharper disable once InvertIf
        if (res.Succeeded)
            DbgPrint("JobResult successfuly processed", getResultAsString.Value);
#endif
        return !res.Succeeded ? Result.Propagate(res) : Result.Success;
    }

    private static Result<Assembly> FindEntryPointAssembly(Type? entrypointType = null)
    {
        try
        {
            var asm = entrypointType != null ? Assembly.GetAssembly(entrypointType) : Assembly.GetEntryAssembly();
            if (asm != null) return asm;

            var sb = new StringBuilder();
            sb.AppendLine("Das Entrypoint-Assembly wurde nicht gefunden.");
            sb.AppendLine($"Explicit Enrypoint-Type: {(entrypointType == null ? "<NULL>" : entrypointType.FullName)}");
            return Result.Error(sb.ToString());

        }
        catch (Exception ex) { return ex; }
    }

    internal static Result<MethodInfo> FindJobFunction(IEnumerable<Type> types)
    {
        try
        {
            var methods = types
                .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static))
                .Where(method => method.GetCustomAttribute<JobEntryPointAttribute>() is not null).ToList();

            return methods.Count switch
            {
                0 => Result.Error($"Keine Methode mit dem Attibut [{nameof(JobEntryPointAttribute)}] gefunden."),
                > 1 => Result.Error($"Mehrere Methoden mit dem Attibut [{nameof(JobEntryPointAttribute)}] gefunden."),
                _ => methods.First()
            };
        }
        catch (Exception ex) { return ex; }
    }

    private static async Task<Result<object[]?>> CreateInvocationArguments(MethodInfo func, IJosynApplicationProtocol japClient)
    {
        var parameters = func.GetParameters();
        if (parameters.Length == 0) return Result<object[]?>.Success(null);

        var rawArguments = await japClient.GetRawArguments();
        if (!rawArguments.Succeeded)
            return Result<object[]?>.Propagate(rawArguments.ToResult<object[]?>());
#if DEBUG
        DbgPrint("JobArguments successfuly retrieved", rawArguments.Value);
#endif
        var createInvicationArguments = RetrieveInvocationArguments(func, rawArguments.Value);
        if (!createInvicationArguments.Succeeded)
            return Result<object[]?>.Propagate(createInvicationArguments.ToResult<object[]?>());

        return createInvicationArguments.Value;
    }

#if DEBUG
    private static void DbgPrint(string head, string content)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"\n[{head}]");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(content);
        Console.ResetColor();
    }
#endif    

    private static Result<object[]> RetrieveInvocationArguments(MethodInfo func, string rawArguments)
    {
        try
        {
            var parameters = func.GetParameters();
            if (parameters.Length == 0)
                return Array.Empty<object>();

            var isSingleRecord = (parameters.Length == 1) && parameters.Single().ParameterType.GetMethod("<Clone>$") is not null;

            if (!isSingleRecord)
            {
                var getInvokeArgs = PropertyBag.Deserialize(rawArguments, parameters);
                if (!getInvokeArgs.Succeeded)
                    return Result<object[]>.Propagate(getInvokeArgs);
                return getInvokeArgs.Value;
            }

            var getArgumentsRecord = PropertyBag.Deserialize(rawArguments, parameters.First().ParameterType);
            if (!getArgumentsRecord.Succeeded)
                return Result<object[]>.Propagate(getArgumentsRecord.ToResult<object[]>());

            return new List<object> { getArgumentsRecord.Value }.ToArray();

        }
        catch (Exception ex) { return ex; }
    }

    #endregion
}
