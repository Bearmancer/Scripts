namespace CSharpScripts.Infrastructure;

public static class Paths
{
    public static readonly string ProjectRoot = FindAncestorContaining(".git");
    public static readonly string LogDirectory = Combine(ProjectRoot, "logs");
    public static readonly string StateDirectory = Combine(ProjectRoot, "state");
    public static readonly string DumpsDirectory = Combine(ProjectRoot, "dumps");

    private static string FindAncestorContaining(string marker)
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
