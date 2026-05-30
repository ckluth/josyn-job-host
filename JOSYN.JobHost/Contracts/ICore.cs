namespace JOSYN.JobHost;

/// <summary>
/// Contract for the entry point of a job executable.
/// </summary>
public interface ICore
{
    /// <summary>
    /// Entry point of every job executable. Reads the IPC session arguments, connects to the
    /// JAPServer, invokes the job via reflection, and returns the result.
    /// </summary>
    /// <param name="args">Command-line arguments containing the IPC session key.</param>
    /// <returns>
    /// <c>0</c> on success; negative values encode specific failure scenarios:
    /// <c>-1</c> = IPC connection failed, <c>-2</c> = job invocation failed.
    /// </returns>
    public static abstract Task<int> Run(string[] args);
}