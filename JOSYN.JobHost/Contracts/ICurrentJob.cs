using JOSYN.Jap.Contract;

#pragma warning disable IDE0130
namespace JOSYN.JobHost;
#pragma warning restore IDE0130

/// <summary>
/// Exposes read-only runtime information about the currently executing job.
/// Implemented by <see cref="CurrentJob"/> — the single static API surface
/// available to job authors at runtime.
/// </summary>
public interface ICurrentJob
{
    /// <summary>
    /// The assembly name of the running job executable
    /// (e.g. <c>Contoso.DemoProduct.DemoJob</c>).
    /// </summary>
    static abstract string Name { get; }

    /// <summary>
    /// The runtime environment in which the job is executing,
    /// as reported by the JAPServer at session start
    /// (<c>DEV</c>, <c>INT</c> or <c>PROD</c>).
    /// </summary>
    static abstract RuntimeEnvironment Environment { get; }
}