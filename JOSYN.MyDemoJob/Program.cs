using System.Text;

namespace JOSYN.MyDemoJob;

internal class Program
{
    private static async Task<int> Main(string[] args) { return await JOSYN.System.JobHost.Core.Run(args); }
}

