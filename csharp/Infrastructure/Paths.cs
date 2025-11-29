namespace CSharpScripts.Infrastructure;

internal static class Paths
{
    internal static readonly string ProjectRoot = GetDirectoryName(
        GetDirectoryName(GetDirectoryName(GetDirectoryName(AppContext.BaseDirectory)!)!)!
    )!;

    internal static readonly string LogDirectory = Combine(ProjectRoot, "logs");
    internal static readonly string StateDirectory = Combine(ProjectRoot, "state");
    internal static readonly string ArchivesDirectory = Combine(LogDirectory, "archives");
}
