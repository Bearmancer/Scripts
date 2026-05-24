using TUnit;
using FluentAssertions;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using CSharpScripts.Data;

namespace Scripts.Tests.Environment;

internal sealed class OciDeploymentTests
{
    private static (int ExitCode, string StdOut, string StdErr) RunCommand(
        string fileName, string arguments)
    {
        var psi = new ProcessStartInfo(fileName, arguments)
        {
            WorkingDirectory = @"C:\Users\Lance\Dev\Scripts",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using var proc = Process.Start(psi)!;
        var stdout = proc.StandardOutput.ReadToEnd();
        var stderr = proc.StandardError.ReadToEnd();
        proc.WaitForExit();
        return (proc.ExitCode, stdout, stderr);
    }

    [Test]
    public void OciSshConnection_Succeeds_WithVerifiedFile()
    {
        var (exitCode, stdout, stderr) = RunCommand("ssh", "oci \"test -f /home/ubuntu/.oci_verified\"");
        exitCode.Should().Be(0, $"SSH verified file check failed: {stderr}\n{stdout}");
    }
}
