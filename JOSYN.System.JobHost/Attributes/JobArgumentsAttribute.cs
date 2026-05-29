namespace JOSYN.System.JobHost.Attributes;

/// <summary>
/// Marks a class as the arguments type for a job.
/// Used to explicitly designate the parameter type of the <see cref="JobEntryPointAttribute"/>
/// method as job arguments.
/// </summary>
/// <remarks>
/// Not yet implemented — this attribute is not validated by the runtime and serves as a
/// documentation marker only. Planned for a future milestone.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class JobArgumentsAttribute() : Attribute { }