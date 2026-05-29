namespace CSharpScripts.Tests;

internal sealed class GlobalSetup
{
    [Before(Assembly)]
    public static async Task LoadDotEnvAsync()
    {
        var envFile = Path.Combine(
            FindRepoRoot(),
            ".env"
        );

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

    private static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is { })
        {
            if (File.Exists(Path.Combine(dir, ".env")))
                return dir;
            dir = Path.GetDirectoryName(dir);
        }
        return AppContext.BaseDirectory;
    }
}
