using FluentAssertions;
using TUnit;

namespace Scripts.Tests.SignOff;

#pragma warning disable CA2000
internal sealed class EnvironmentVerificationTests
{
    [Test]
    public async Task Docker_Is_Running()
    {
        using var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "ps",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };

        process.Start();
        await process.WaitForExitAsync();

        process.ExitCode.Should().Be(0,
            "Docker must be running for all database operations"
        );
    }

    [Test]
    public async Task Docker_Compose_File_Is_Valid()
    {
        using var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "compose -f C:\\Users\\Lance\\Dev\\Scripts\\docker-compose.yml config",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };

        process.Start();
        await process.WaitForExitAsync();

        process.ExitCode.Should().Be(0,
            "docker-compose.yml must be valid"
        );
    }

    [Test]
    public void Dot_Env_File_Exists()
    {
        var envPath = @"C:\Users\Lance\Dev\Scripts\.env";
        File.Exists(envPath).Should().BeTrue(
            ".env file must exist with PGCONNSTR"
        );
    }

    [Test]
    public void Dot_Env_Contains_PGCONNSTR()
    {
        var envPath = @"C:\Users\Lance\Dev\Scripts\.env";
        var content = File.ReadAllText(envPath);
        content.Should().Contain("PGCONNSTR",
            ".env must define PGCONNSTR"
        );
    }

    [Test]
    public void Compiled_Model_Directory_Exists()
    {
        var compiledModelDir = @"C:\Users\Lance\Dev\Scripts\csharp\CompiledModels";
        Directory.Exists(compiledModelDir).Should().BeTrue(
            "CompiledModels directory must exist after EF Core compiled model generation"
        );
        Directory.GetFiles(compiledModelDir, "*.cs").Should().NotBeEmpty(
            "CompiledModels directory must contain generated .cs files"
        );
    }

    [Test]
    public void LogDirectory_Points_To_UserProfile_Cache()
    {
        var expectedBase = Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile),
            ".cache", "logs", "scripts"
        );

        var logDir = CSharpScripts.Core.Paths.LogDirectory;

        logDir.Should().Be(expectedBase,
            $"LogDirectory must equal %USERPROFILE%\\.cache\\logs\\scripts. Actual: {logDir}"
        );
    }
}
