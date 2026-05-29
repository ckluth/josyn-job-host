namespace JOSYN.System.JobHost;

/// <summary>
/// Placeholder — intentionally unused. Reserved for a future conditional parallel execution feature:
/// given a job's arguments and a set of other pending jobs' arguments, decides whether parallel
/// execution is allowed. Do not remove until the feature is implemented or explicitly dropped.
/// </summary>
internal delegate bool ArgumentsComparer<in T>(T my, IEnumerable<T> others) where T : class;