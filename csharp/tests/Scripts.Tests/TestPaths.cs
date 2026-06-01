using System.Runtime.CompilerServices;

namespace Scripts.Tests;

/// <summary>
/// Cross-platform repo-root resolution using <see cref="CallerFilePathAttribute"/>.
/// The compiler embeds the absolute path of this source file at compile time,
/// so navigation is always correct regardless of output directory structure.
/// </summary>
/// <remarks>
/// All paths are derived from this file's own compile-time location, not from
/// <c>AppContext.BaseDirectory</c> or hand-counted <c>..</c> segments relative
/// to the build output. This makes resolution immune to TFM changes, custom
/// output paths, and CI vs. local layout differences.
///
/// File location: <c>csharp/tests/Scripts.Tests/TestPaths.cs</c>
/// Three <c>..</c> segments from its own directory → repo root.
/// </remarks>
internal static class TestPaths
{
    // Compiler-embedded source path → repo root (3 levels up from this file's directory).
    // csharp/tests/Scripts.Tests/TestPaths.cs  → csharp/tests/Scripts.Tests → csharp/tests → csharp → repo root
    public static readonly string RepoRoot = ComputeRepoRoot();

    /// <summary>csharp/ directory (project language root).</summary>
    public static string CSharpRoot => Combine("csharp");

    /// <summary>csharp/src/ directory (all production source projects).</summary>
    public static string SrcRoot => Combine("csharp", "src");

    /// <summary>csharp/tests/Scripts.Tests/ directory (this test project).</summary>
    public static string TestsRoot => Combine("csharp", "tests", "Scripts.Tests");

    /// <summary>Joins <paramref name="parts"/> relative to <see cref="RepoRoot"/>.</summary>
    public static string Combine(params string[] parts) =>
        Path.GetFullPath(Path.Combine([RepoRoot, .. parts]));

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private static string ComputeRepoRoot([CallerFilePath] string thisFile = "")
    {
        var root = Path.GetFullPath(
            Path.Combine(Path.GetDirectoryName(thisFile)!, "..", "..", ".."));

        ThrowIfInvalid(root);
        return root;
    }

    /// <summary>
    /// Fails fast with a clear message if the resolved root does not contain
    /// the expected repo sentinel. Catches moves of this file without a
    /// corresponding update to the <c>..</c> count.
    /// </summary>
    private static void ThrowIfInvalid(string root)
    {
        // AGENTS.md is the single source of truth — always present at the repo root.
        var sentinel = Path.Combine(root, "AGENTS.md");
        if (!File.Exists(sentinel))
        {
            throw new InvalidOperationException(
                $"TestPaths resolved repo root to '{root}', but '{sentinel}' was not found. " +
                "If TestPaths.cs was moved, update the number of '..' segments in ComputeRepoRoot().");
        }
    }
}

