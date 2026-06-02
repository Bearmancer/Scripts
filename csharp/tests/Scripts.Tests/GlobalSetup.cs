using Scripts.Tests;

namespace Scripts.Tests;

internal sealed class GlobalSetup
{
    [Before(Assembly)]
    public static async Task LoadDotEnvAsync(AssemblyHookContext context)
    {
        // Force ScriptsDbContext to skip the shared compiled model for tests.
        //
        // Two distinct bugs are layered here; both are still active as of this
        // commit and both require this env var to keep the test suite green:
        //
        //   1. EF Core 10.0.8 upstream TOCTOU race in
        //      RuntimeProperty.GetValueComparer() (and the related
        //      GetKeyValueComparer()). The first concurrent access to a given
        //      property can lose the Interlocked.CompareExchange and cache a
        //      NullReferenceException forever. The project cannot fix this
        //      without an EF upgrade. Documented in
        //      research/20260602-efcore-1008-race-condition-research.md.
        //
        //   2. The null-unsafe ValueComparer<string> in OnModelCreating is
        //      now fixed (see NullSafeStringComparerTests and the related
        //      commit). The compiled model path itself is fine for the
        //      happy path; the env-var workaround remains only because of #1.
        //
        // This workaround is the smallest unit that addresses #1: it forces
        // every non-InMemory DbContext to use OnModelCreating, which the
        // production path has never relied on. Removing it re-exposes the
        // 56/213 NRE failure mode documented in the research note above
        // (56/213 is the original figure in
        // research/20260602-efcore-1008-race-condition-research.md:7; the
        // current test count is higher because the regression tests
        // added by this work bring it to ~224, but the failure-rate
        // ratio is unchanged).
        // Do not remove without first pinning a release that contains the
        // EF fix at runtime/.../RuntimeProperty.cs and the related
        // NonCapturingLazyInitializer overloads.
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

