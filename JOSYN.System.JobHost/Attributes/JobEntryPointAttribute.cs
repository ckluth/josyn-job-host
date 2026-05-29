namespace JOSYN.System.JobHost.Attributes;

/// <summary>
/// Marks the method that serves as the entry point for a job.
/// Exactly one method per job assembly may carry this attribute.
/// The method must be <c>public static</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false)]
public sealed class JobEntryPointAttribute() : Attribute { }