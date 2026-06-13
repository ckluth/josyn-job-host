using JOSYN.Foundation.ResultPattern;
using JOSYN.Foundation.PropertyBag;
using JOSYN.Jap.Contract;
using JOSYN.JobHost.Attributes;

namespace JOSYN.JobHost.Test;

// ── Fake protocol ─────────────────────────────────────────────────────────────

internal sealed class FakeProtocol(string rawArguments = "") : IJosynApplicationProtocol
{
    public string? LastPutResult { get; private set; }

    public Task<Result> AcceptSession() => Task.FromResult(Result.Success);
    public Task<Result> RejectSession() => Task.FromResult(Result.Success);
    public Task<Result<string>> GetConcurrentSessionArguments()
        => Task.FromResult(Result<string>.Success("[]"));

    public Task<Result<string>> GetRawArguments()
        => Task.FromResult(Result<string>.Success(rawArguments));

    public Task<Result> PutRawResult(string result)
    {
        LastPutResult = result;
        return Task.FromResult(Result.Success);
    }

    public Task<Result> PutDomainError(string? description) => Task.FromResult(Result.Success);
    public Task<Result> PutError(string serializedError) => Task.FromResult(Result.Success);

    public Task<Result<RuntimeEnvironment>> GetEnvironment()
        => Task.FromResult(Result<RuntimeEnvironment>.Success(RuntimeEnvironment.DEV));
}

internal sealed class FailingGetArgumentsProtocol : IJosynApplicationProtocol
{
    public Task<Result> AcceptSession() => Task.FromResult(Result.Success);
    public Task<Result> RejectSession() => Task.FromResult(Result.Success);
    public Task<Result<string>> GetConcurrentSessionArguments()
        => Task.FromResult(Result<string>.Success("[]"));

    public Task<Result<string>> GetRawArguments()
        => Task.FromResult(Result<string>.Fail("Verbindung verloren"));

    public Task<Result> PutRawResult(string result) => Task.FromResult(Result.Success);
    public Task<Result> PutDomainError(string? description) => Task.FromResult(Result.Success);
    public Task<Result> PutError(string serializedError) => Task.FromResult(Result.Success);

    public Task<Result<RuntimeEnvironment>> GetEnvironment()
        => Task.FromResult(Result<RuntimeEnvironment>.Success(RuntimeEnvironment.DEV));
}

// ── Stub types ────────────────────────────────────────────────────────────────

internal sealed record StubArguments(string Name, int Value);
internal sealed record StubResult(string Echo, int Doubled);

// ── Job stubs ─────────────────────────────────────────────────────────────────

internal static class StubVoidJob
{
    [JobEntryPoint]
    public static void Run() { }
}

internal static class StubJobAlpha
{
    [JobEntryPoint]
    public static void Run() { }
}

internal static class StubJobBeta
{
    [JobEntryPoint]
    public static void Run() { }
}

internal static class StubRecordJob
{
    [JobEntryPoint]
    public static StubResult Run(StubArguments args)
        => new(args.Name + "!", args.Value * 2);
}

internal static class StubThrowingJob
{
    [JobEntryPoint]
    public static void Run() => throw new InvalidOperationException("Test-Fehler");
}
