using System.Reflection;
using System.Text;
using JOSYN.Foundation.PropertyBag;
using JOSYN.Foundation.ResultPattern;
using JOSYN.Jap.Shared.Contract;
using JOSYN.Jap.Shared.Log;

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
                LocalLog.Error(createJAPClient.ToResult());
                return -1;
            }

            var invokeResult = await JobInvoker.InvokeJob(createJAPClient.Value);
            if (invokeResult.Succeeded) return 0;

            LocalLog.Error(invokeResult);
            await ReportErrorToServer(createJAPClient.Value, invokeResult);
            return -2;
        }
        finally
        {
#if DEBUG
            Console.Write("\n[PRESS ANY KEY TO EXIT...]");
            Console.ReadKey(true);
#endif
        }
    }


    // -------------------------------------------------------------------------

    /// <summary>
    /// Name of the entry assembly (the job executable filename without extension).
    /// Used as the error causer identifier when reporting errors to the server.
    /// </summary>
    public static readonly string ProcessName = Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly()?.Location ?? "unknown");

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
            LocalLog.Error($"PutError an Server fehlgeschlagen: {put.ErrorMessage}");
    }
}

