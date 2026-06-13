using System.Reflection;
using System.Text;
using System.Text.Json;
using JOSYN.Foundation.JIP;
using JOSYN.Foundation.ResultPattern;
using JOSYN.Jap.Contract;
using JOSYN.Commons.Log;
using JOSYN.JobHost.Attributes;

namespace JOSYN.JobHost;

/// <inheritdoc cref="ICore"/>
public sealed class Core : ICore
{
    
    /// <inheritdoc/>
    public static async Task<int> Run(string[] args)
    {
        Console.InputEncoding = new UTF8Encoding();
        Console.OutputEncoding = new UTF8Encoding();
#if DEBUG
        LocalLog.EnableConsoleOutput = true;
#endif

        try
        {
            var createJAPClient = await JAPClient.CreateConnectedClient(args);
            if (!createJAPClient.Succeeded)
            {
                LocalLog.WriteError(createJAPClient.ToResult());
                return -1;
            }

            var client = createJAPClient.Value;
            try
            {
                // Negotiation: evaluate parallel-execution policy and accept or reject (ADR-008).
                var negotiateResult = await NegotiateSession(client);
                if (!negotiateResult.Succeeded)
                {
                    LocalLog.WriteError(negotiateResult.ErrorMessage ?? string.Empty);
                    return -1;
                }
                if (!negotiateResult.Value)
                    return 0; // rejected — clean exit, no error

                var getEnv = await (client as IJosynApplicationProtocol).GetEnvironment();
                if (!getEnv.Succeeded)
                {
                    await ReportErrorToServer(client, getEnv.ToResult());
                    return -1;
                }
                Environment = getEnv.Value;

                var invokeResult = await JobInvoker.InvokeJob(client);
                if (invokeResult.Succeeded) return 0;

                // TODO: Local nur, wenn ReportErrorToServer failed
                LocalLog.WriteError(invokeResult);
                await ReportErrorToServer(client, invokeResult);

                return -1;
            }
            finally
            {
                await PipesClient.DisconnectAsync(client.Pipes, sendShutdownRequest: true);
            }
        }
        finally
        {
#if DEBUG
            Console.Write("\n[PRESS ANY KEY TO EXIT...]");
            Console.ReadKey(true);
#endif
        }
    }           

    internal static readonly string ProcessName = Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly()?.Location ?? "unknown");

    internal static RuntimeEnvironment Environment { get; private set; }

    // -------------------------------------------------------------------------

    /// <summary>
    /// Evaluates the job's parallel-execution policy and calls
    /// <see cref="IJosynApplicationProtocol.AcceptSession"/> or
    /// <see cref="IJosynApplicationProtocol.RejectSession"/> accordingly.
    /// Returns <c>true</c> if accepted, <c>false</c> if rejected (ADR-008).
    /// </summary>
    private static async Task<Result<bool>> NegotiateSession(JAPClient client)
    {
        IJosynApplicationProtocol protocol = client;

        var findEntrypoint = JobInvoker.FindJobFunction(
            Assembly.GetEntryAssembly()?.GetExportedTypes() ?? []);

        // [ParallelExecutionAllowed] (or [ParallelExecutionAllowed(true)]) → always accept.
        if (findEntrypoint.Succeeded)
        {
            var attr = findEntrypoint.Value.GetCustomAttribute<ParallelExecutionAllowedAttribute>();
            if (attr is not null && attr.IsAllowed)
            {
                var accept = await protocol.AcceptSession();
                return !accept.Succeeded ? Result<bool>.Propagate(accept.ToResult<bool>()) : true;
            }
        }

        // Default policy: never parallel — reject if any sibling session is running.
        var getSiblings = await protocol.GetConcurrentSessionArguments();
        if (!getSiblings.Succeeded)
        {
            // Cannot determine sibling state — reject conservatively.
            await protocol.RejectSession();
            return Result<bool>.Fail($"GetConcurrentSessionArguments fehlgeschlagen: {getSiblings.ErrorMessage}");
        }

        var siblings = JsonSerializer.Deserialize<List<string>>(getSiblings.Value) ?? [];
        if (siblings.Count > 0)
        {
            var reject = await protocol.RejectSession();
            return !reject.Succeeded ? Result<bool>.Propagate(reject.ToResult<bool>()) : false;
        }

        var acceptDefault = await protocol.AcceptSession();
        return !acceptDefault.Succeeded ? Result<bool>.Propagate(acceptDefault.ToResult<bool>()) : true;
    }

    private static async Task ReportErrorToServer(JAPClient client, Result error)
    {
        var report = new ErrorReport(
            ProcessName,
            error.ErrorMessage ?? string.Empty,
            error.CallStackAsString,
            error.Exception?.ToString(),
            DateTimeOffset.Now);

        var put = await client.PutError(report);
        
        if (!put.Succeeded)
            LocalLog.WriteError($"PutError an Server fehlgeschlagen: {put.ErrorMessage}");
    }
}

