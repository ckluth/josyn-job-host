using JOSYN.Foundation.JIP;
using JOSYN.Foundation.PropertyBag;
using JOSYN.Foundation.ResultPattern;
using JOSYN.Jap.Contract;

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

    // -------------------------------------------------------------------------
    // Session start negotiation
    // -------------------------------------------------------------------------

    async Task<Result> IJosynApplicationProtocol.AcceptSession()
    {
        var result = await JipClient.SendAsync(Pipes, nameof(IJosynApplicationProtocol.AcceptSession));
        return !result.Succeeded ? Result.Propagate(result.ToResult()) : Result.Success;
    }

    async Task<Result> IJosynApplicationProtocol.RejectSession()
    {
        var result = await JipClient.SendAsync(Pipes, nameof(IJosynApplicationProtocol.RejectSession));
        return !result.Succeeded ? Result.Propagate(result.ToResult()) : Result.Success;
    }

    async Task<Result<string>> IJosynApplicationProtocol.GetConcurrentSessionArguments()
    {
        var result = await JipClient.SendAsync(Pipes, nameof(IJosynApplicationProtocol.GetConcurrentSessionArguments));
        if (!result.Succeeded)
            return Result<string>.Propagate(result.ToResult<string>());
        return Result<string>.Success(result.Value ?? "[]");
    }

    // -------------------------------------------------------------------------
    // Job execution
    // -------------------------------------------------------------------------

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

    async Task<Result> IJosynApplicationProtocol.PutDomainError(string? description)
    {
        var result = await JipClient.SendAsync(Pipes, nameof(IJosynApplicationProtocol.PutDomainError), description ?? string.Empty);
        return !result.Succeeded ? Result.Propagate(result.ToResult()) : Result.Success;
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

    async Task<Result<RuntimeEnvironment>> IJosynApplicationProtocol.GetEnvironment()
    {
        var getEnv = await JipClient.SendAsync(Pipes, nameof(IJosynApplicationProtocol.GetEnvironment));

        if (!getEnv.Succeeded)
            return Result<RuntimeEnvironment>.Propagate(getEnv.ToResult<RuntimeEnvironment>());

        if (!Enum.TryParse<RuntimeEnvironment>(getEnv.Value, out var env))
            return Result<RuntimeEnvironment>.Fail($"Ungültiger RuntimeEnvironment-Wert: '{getEnv.Value}'");

        return env;
    }

    async Task<Result<string>> IJosynApplicationProtocol.GetConfigValue(string settingPath)
    {
        var result = await JipClient.SendAsync(Pipes, nameof(IJosynApplicationProtocol.GetConfigValue), settingPath);
        if (!result.Succeeded)
            return Result<string>.Propagate(result.ToResult<string>());
        return result.Value ?? Result<string>.Fail("Server lieferte keine Daten zurück.");
    }
}