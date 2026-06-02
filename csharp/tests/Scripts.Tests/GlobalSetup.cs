using Scripts.Tests;

namespace Scripts.Tests;

internal sealed class GlobalSetup
{
    [Before(Assembly)]
    public static async Task LoadDotEnvAsync(AssemblyHookContext context)
    {
        // Force ScriptsDbContext to skip the shared compiled model for tests — its
        // shared lazy state races under concurrent first-access, producing NRE deep
        // inside EF Core (RuntimeProperty.GetValueComparer, OriginalValuesFactory).
        // Using OnModelCreating per DbContext options instance keeps the test safe.
        System.Environment.SetEnvironmentVariable("SCRIPTS_NO_COMPILED_MODEL", "1");

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

