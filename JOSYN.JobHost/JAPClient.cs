using JOSYN.Foundation.JIP;
using JOSYN.Foundation.PropertyBag;
using JOSYN.Foundation.ResultPattern;
using JOSYN.Jap.Shared.Contract;

namespace JOSYN.JobHost;

internal sealed class JAPClient : IJosynApplicationProtocol
{
    private JAPClient() { }
    
    internal required ClientPipes Pipes { get; init; }
    
    internal static async Task<Result<JAPClient>> CreateConnectedClient(string[] args)
    {
        var sessionKey = PipesProtocol.ParseSessionKeyCLIArguments(args);
        if (sessionKey == Guid.Empty)
            return Result.Error("Der Anwendung wurde kein Pipes-SessionKey übergeben");
        
        var getPipes = await PipesClient.ConnectAsync(sessionKey);
        if (!getPipes.Succeeded)
            return Result<JAPClient>.Propagate(getPipes.ToResult<JAPClient>());

        var client = new JAPClient { Pipes = getPipes.Value };
        return client;
    }

    async Task<Result<string>> IJosynApplicationProtocol.GetRawArguments()
    {
        var getConfig = await JipClient.SendAsync(Pipes, nameof(IJosynApplicationProtocol.GetRawArguments));

        if (!getConfig.Succeeded)
            return Result<string>.Propagate(getConfig.ToResult<string>());

        return getConfig.Value ??
               Result<string>.Fail("Server lieferte keine Daten zurück.");
    }

    async Task<Result> IJosynApplicationProtocol.PutRawResult(string result)
    {
        var putJobResult = await JipClient.SendAsync(Pipes, nameof(IJosynApplicationProtocol.PutRawResult), result);

        return !putJobResult.Succeeded
            ? Result.Propagate(putJobResult.ToResult())
            : Result.Success;
    }

    async Task<Result> IJosynApplicationProtocol.PutError(string serializedError)
    {
        var result = await JipClient.SendAsync(Pipes, nameof(IJosynApplicationProtocol.PutError), serializedError);
        return !result.Succeeded ? Result.Propagate(result.ToResult()) : Result.Success;
    }
    
    internal async Task<Result> PutError(ErrorReport report)
    {
        var serialized = PropertyBag.Serialize(report, JsonDictionarySerializer.Serialize);
        if (!serialized.Succeeded)
            return Result.Propagate(serialized.ToResult());
        IJosynApplicationProtocol protocolImpl = this; 
        var put = await protocolImpl.PutError(serialized.Value);        
        return !put.Succeeded ? Result.Propagate(put) : Result.Success;
    }
}