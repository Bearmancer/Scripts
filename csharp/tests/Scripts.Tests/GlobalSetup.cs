using Scripts.Tests;

namespace Scripts.Tests;

internal sealed class GlobalSetup
{
    [Before(Assembly)]
    public static async Task LoadDotEnvAsync(AssemblyHookContext context)
    {
        // TestPaths.RepoRoot is compiler-anchored via [CallerFilePath] and validated
        // against AGENTS.md — reliable regardless of output directory layout.
        var envFile = Path.Combine(TestPaths.RepoRoot, ".env");

        if (!File.Exists(envFile))
            return;

        foreach (var line in await File.ReadAllLinesAsync(envFile))
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                continue;

            var eq = line.IndexOf('=');
            if (eq <= 0)
                continue;

            var key = line[..eq].Trim();
            var value = line[(eq + 1)..].Trim();

            if (System.Environment.GetEnvironmentVariable(key) is null)
                System.Environment.SetEnvironmentVariable(key, value);
        }
    }
}

