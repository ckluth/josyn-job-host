using NUnit.Framework;
using JOSYN.Foundation.ResultPattern;
using JOSYN.Foundation.PropertyBag;
using JOSYN.JobHost.Attributes;

namespace JOSYN.JobHost.Test;

[TestFixture]
public sealed class JobInvokerTests
{
    // ── Entry point discovery ──────────────────────────────────────────────────

    [Test]
    public async Task InvokeJob_NoEntryPoint_Fails()
    {
        var result = await JobInvoker.InvokeJob(new FakeProtocol(), Array.Empty<Type>());

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain(nameof(JobEntryPointAttribute)));
    }

    [Test]
    public async Task InvokeJob_MultipleEntryPoints_Fails()
    {
        var result = await JobInvoker.InvokeJob(new FakeProtocol(), [typeof(StubJobAlpha), typeof(StubJobBeta)]);

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("Mehrere"));
    }

    // ── Successful invocation ──────────────────────────────────────────────────

    [Test]
    public async Task InvokeJob_VoidEntryPoint_Succeeds()
    {
        var result = await JobInvoker.InvokeJob(new FakeProtocol(), [typeof(StubVoidJob)]);

        Assert.That(result.Succeeded, Is.True);
    }

    [Test]
    public async Task InvokeJob_RecordArgument_DeserializesAndExecutes()
    {
        var args = new StubArguments("Hallo", 21);
        var serialize = PropertyBag.Serialize(args, IniDictionarySerializer.Serialize);
        Assert.That(serialize.Succeeded, Is.True, serialize.ErrorMessage);

        var protocol = new FakeProtocol(serialize.Value!);
        var result = await JobInvoker.InvokeJob(protocol, [typeof(StubRecordJob)]);

        Assert.That(result.Succeeded, Is.True, result.ErrorMessage);
    }

    [Test]
    public async Task InvokeJob_RecordReturn_PutsSerializedResult()
    {
        var args = new StubArguments("Test", 5);
        var serialize = PropertyBag.Serialize(args, IniDictionarySerializer.Serialize);
        var protocol = new FakeProtocol(serialize.Value!);

        var result = await JobInvoker.InvokeJob(protocol, [typeof(StubRecordJob)]);

        Assert.That(result.Succeeded, Is.True, result.ErrorMessage);
        Assert.That(protocol.LastPutResult, Is.Not.Null);
        Assert.That(protocol.LastPutResult, Does.Contain("Test!"));
    }

    // ── Error propagation ─────────────────────────────────────────────────────

    [Test]
    public async Task InvokeJob_GetArgumentsFails_PropagatesError()
    {
        var result = await JobInvoker.InvokeJob(new FailingGetArgumentsProtocol(), [typeof(StubRecordJob)]);

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("Verbindung verloren"));
    }

    [Test]
    public async Task InvokeJob_JobThrowsUnhandledException_ReturnsGermanError()
    {
        var result = await JobInvoker.InvokeJob(new FakeProtocol(), [typeof(StubThrowingJob)]);

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("unbehandelte Exception"));
    }
}

