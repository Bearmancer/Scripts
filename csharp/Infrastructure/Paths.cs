namespace CSharpScripts.Infrastructure;

internal static class Paths
{
    internal static readonly string ProjectRoot = FindAncestorContaining(".git");
    internal static readonly string LogDirectory = Combine(ProjectRoot, "logs");
    internal static readonly string StateDirectory = Combine(ProjectRoot, "state");

    static string FindAncestorContaining(string marker)
    {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (
                Directory.Exists(Combine(dir.FullName, marker))
                || File.Exists(Combine(dir.FullName, marker))
            )
                return dir.FullName;
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException($"Could not find ancestor containing '{marker}'");
    }
}
