namespace JOSYN.Jap.JobHost.Attributes;

/// <summary>
/// Marks a class as the result type of a job.
/// Used to explicitly designate the return type of the <see cref="JobEntryPointAttribute"/>
/// method as the job result.
/// </summary>
/// <remarks>
/// Not yet implemented — this attribute is not validated by the runtime and serves as a
/// documentation marker only. Planned for a future milestone.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class JobResultAttribute() : Attribute { }