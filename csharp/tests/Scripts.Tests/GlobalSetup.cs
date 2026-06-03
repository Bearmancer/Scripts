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
        // This workaround is one of two cooperating layers that together
        // suppress the upstream race. The other is
        // SingleThreadedParallelLimit (Limit=1) in this test assembly.
        // The env-var forces every non-InMemory DbContext to use
        // OnModelCreating, sidestepping the shared compiled
        // RuntimeModel that the race lives in. The limiter prevents two
        // contexts from racing the same first-time materialisation. Each
        // layer alone is partial; both together have held the 56/213 NRE
        // failure rate documented in
        // research/20260602-efcore-1008-race-condition-research.md:7
        // (the test count has grown with this work, but the failure ratio
        // is unchanged). Do not remove either layer without first pinning
        // a release that contains the EF fix at
        // runtime/.../RuntimeProperty.cs and the related
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

