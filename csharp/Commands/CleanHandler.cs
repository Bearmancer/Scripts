namespace CSharpScripts.Commands;

internal static class CleanHandler
{
    internal static void Execute()
    {
        var binDir = Combine(Paths.ProjectRoot, "csharp", "bin");
        var objDir = Combine(Paths.ProjectRoot, "csharp", "obj");

        var hadBin = Directory.Exists(binDir);
        var hadObj = Directory.Exists(objDir);

        if (hadBin)
        {
            Delete(binDir, recursive: true);
            Logger.Info("Deleted bin/");
        }

        if (hadObj)
        {
            Delete(objDir, recursive: true);
            Logger.Info("Deleted obj/");
        }

        if (!hadBin && !hadObj)
        {
            Logger.Info("No build artifacts to clean.");
            return;
        }

        Logger.NewLine();
        Logger.Info("Rebuilding...");

        var csprojDir = Combine(Paths.ProjectRoot, "csharp");
        var process = Process.Start(
            new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "build",
                WorkingDirectory = csprojDir,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            }
        );

        process?.WaitForExit();

        if (process?.ExitCode == 0)
            Logger.Success("Build completed successfully.");
        else
            Logger.Error("Build failed. Run 'dotnet build' manually to see errors.");
    }
}
