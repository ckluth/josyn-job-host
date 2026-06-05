using JOSYN.Jap.Shared.Contract;

namespace JOSYN.JobHost;


/// <summary>
/// The single static API surface available to job authors at runtime.
/// Provides read-only information about the currently executing job session.
/// </summary>
/// <remarks>
/// Populated by <see cref="Core"/> before the <c>[JobEntryPoint]</c> method is invoked.
/// No instance is required — access properties directly via the class name.
/// </remarks>
public sealed class CurrentJob : ICurrentJob
{
    /// <inheritdoc/>
    public static string Name => Core.ProcessName;
    
    /// <inheritdoc/>
    public static RuntimeEnvironment Environment => Core.Environment;
}