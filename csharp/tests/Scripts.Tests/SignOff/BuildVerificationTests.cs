using FluentAssertions;
using TUnit;

namespace Scripts.Tests.SignOff;

#pragma warning disable CA2000
internal sealed class BuildVerificationTests
{
    [Test]
    public async Task Dotnet_Build_Slnx_Zero_Errors()
    {
        using var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "build C:\\Users\\Lance\\Dev\\Scripts\\csharp\\Scripts.slnx",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };

        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        process.ExitCode.Should().Be(0,
            $"dotnet build must exit 0.\nStdOut: {output}\nStdErr: {error}"
        );
    }

    [Test]
    public async Task Dotnet_Restore_Succeeds()
    {
        using var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "restore C:\\Users\\Lance\\Dev\\Scripts\\csharp\\Scripts.slnx",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };

        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        process.ExitCode.Should().Be(0,
            $"dotnet restore must succeed.\nStdOut: {output}\nStdErr: {error}"
        );
    }
}
