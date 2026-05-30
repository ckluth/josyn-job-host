namespace JOSYN.Jap.JobHost.Attributes;

/// <summary>
/// Declares that the associated job method may be executed in parallel.
/// The <paramref name="isAllowed"/> parameter controls whether parallel execution is
/// enabled (<c>true</c>, default) or explicitly disabled (<c>false</c>).
/// </summary>
/// <remarks>
/// Not yet implemented — this attribute is not evaluated by the runtime.
/// Parallel execution decisions are the responsibility of the job scheduler, which is
/// out of scope for this library. Planned for a future milestone.
/// </remarks>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class ParallelExecutionAllowedAttribute(bool isAllowed = true) : Attribute
{
    /// <summary>Indicates whether parallel execution is permitted.</summary>
    public bool IsAllowed => isAllowed;
}