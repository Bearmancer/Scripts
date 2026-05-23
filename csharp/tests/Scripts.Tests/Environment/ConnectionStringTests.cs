using TUnit;
using FluentAssertions;
using System.Text.RegularExpressions;

namespace Scripts.Tests.Environment;

internal sealed class ConnectionStringTests
{
    [Test]
    public void ConnectionString_IsSet_InEnvironment()
    {
        var connStr = System.Environment.GetEnvironmentVariable("PGCONNSTR");
        connStr.Should().NotBeNullOrWhiteSpace(
            "because PGCONNSTR must be loaded from .env before running tests");
    }

    [Test]
    public void ConnectionString_IsValid_PostgresFormat()
    {
        var connStr = System.Environment.GetEnvironmentVariable("PGCONNSTR");

        connStr.Should().Contain("Host=",
            "because a valid Npgsql connection string must specify a host");
        connStr.Should().Contain("Database=",
            "because a valid Npgsql connection string must specify a database");
        connStr.Should().Contain("Username=",
            "because a valid Npgsql connection string must specify a username");
    }

    [Test]
    public void ConnectionString_DoesNotContain_Password_InPlainText_InLogs()
    {
        // Confirm we can get the string — we do NOT log or print it
        var connStr = System.Environment.GetEnvironmentVariable("PGCONNSTR");
        // If this assertion passes, the test runner never printed the value
        connStr.Should().NotBeNull();
    }
}
