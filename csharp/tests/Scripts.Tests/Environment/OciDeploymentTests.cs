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

    [Test]
    public void OciPostgresContainer_IsRunning()
    {
        var (exitCode, stdout, stderr) = RunCommand("ssh", "oci \"docker ps --filter name=postgres --format '{{.Status}}'\"");
        exitCode.Should().Be(0, $"Docker check failed: {stderr}");
        stdout.Trim().Should().StartWith("Up", "because PostgreSQL container must be running on OCI");
    }
    [Test]
    public async Task OciDatabase_CanConnect_ViaConnectionString()
    {
        var connStr = System.Environment.GetEnvironmentVariable("PGCONNSTR");
        connStr.Should().NotBeNullOrWhiteSpace("PGCONNSTR must be loaded");
        connStr.Should().Contain("Host=oci", "because the application must connect to the remote OCI instance");

        var options = new DbContextOptionsBuilder<ScriptsDbContext>()
            .UseNpgsql(connStr)
            .Options;

        await using var context = new ScriptsDbContext(options);
        var canConnect = await context.Database.CanConnectAsync();
        canConnect.Should().BeTrue("because connection to OCI database must succeed");
    }
}
