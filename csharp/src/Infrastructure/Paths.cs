namespace CSharpScripts.Infrastructure;

public static class Paths
{
    public static readonly string ProjectRoot = FindAncestorContaining(marker: ".git");
    public static readonly string LogDirectory = Combine(path1: ProjectRoot, path2: "logs");
    public static readonly string StateDirectory = Combine(path1: ProjectRoot, path2: "state");
    public static readonly string DumpsDirectory = Combine(path1: StateDirectory, path2: "dump");
    public static readonly string CacheDirectory = Combine(path1: StateDirectory, path2: "cache");
    public static readonly string ExportsDirectory = Combine(path1: ProjectRoot, path2: "exports");

    private static string FindAncestorContaining(string marker)
    {
        DirectoryInfo? dir = new(path: AppContext.BaseDirectory);
        while (dir is { })
        {
            if (
                Directory.Exists(Combine(path1: dir.FullName, path2: marker))
                || File.Exists(Combine(path1: dir.FullName, path2: marker))
            )
                return dir.FullName;

            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException($"Could not find ancestor containing '{marker}'");
    }
}
