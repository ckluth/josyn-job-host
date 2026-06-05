using System.Reflection;
using System.Text;
using JOSYN.Foundation.ResultPattern;
using JOSYN.Jap.Shared.Contract;
using JOSYN.Commons.Log;

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
            
            var getEnv = await (createJAPClient.Value as IJosynApplicationProtocol).GetEnvironment();
            if (!getEnv.Succeeded)
            {
                await ReportErrorToServer(createJAPClient.Value, getEnv.ToResult());
                return -1;
            }
            Environment = getEnv.Value;

            var invokeResult = await JobInvoker.InvokeJob(createJAPClient.Value);
            if (invokeResult.Succeeded) return 0;

            // TODO: Local nur, wenn ReportErrorToServer failed
            LocalLog.WriteError(invokeResult);
            await ReportErrorToServer(createJAPClient.Value, invokeResult);
            
            return -1;
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

