using System.Runtime.CompilerServices;

namespace Scripts.Tests;















internal static class TestPaths
{
    
    
    public static readonly string RepoRoot = ComputeRepoRoot();

    
    public static string CSharpRoot => Combine("csharp");

    
    public static string SrcRoot => Combine("csharp", "src");

    
    public static string TestsRoot => Combine("csharp", "tests", "Scripts.Tests");

    
    public static string Combine(params string[] parts) =>
        Path.GetFullPath(Path.Combine([RepoRoot, .. parts]));

    
    
    
    
    public static string Relative(string absoluteOrRelative) =>
        Path.GetRelativePath(RepoRoot, Path.GetFullPath(absoluteOrRelative));

    
    
    

    private static string ComputeRepoRoot([CallerFilePath] string thisFile = "")
    {
        var root = Path.GetFullPath(
            Path.Combine(Path.GetDirectoryName(thisFile)!, "..", "..", ".."));

        ThrowIfInvalid(root);
        return root;
    }

    
    
    
    
    
    
    private static void ThrowIfInvalid(string root)
    {
        
        
        
        var sentinels = new[]
        {
            Path.Combine(root, "csharp", "Scripts.slnx"),
            Path.Combine(root, "AI", "plans", "INDEX.md"),
        };
        var missing = sentinels.Where(p => !File.Exists(p)).ToArray();
        if (missing.Length > 0)
        {
            throw new InvalidOperationException(
                $"TestPaths resolved repo root to '{root}', but sentinel(s) not found: " +
                string.Join(", ", missing.Select(p => $"'{p}'")) + ". " +
                "If TestPaths.cs was moved, update the number of '..' segments in ComputeRepoRoot().");
        }
    }
}

