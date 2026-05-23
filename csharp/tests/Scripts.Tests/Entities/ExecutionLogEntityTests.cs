using TUnit;
using FluentAssertions;
using CSharpScripts.Data.Entities;
using System.Text.Json;

namespace Scripts.Tests.Entities;

internal sealed class ExecutionLogEntityTests
{
    [Test]
    public void ExecutionLog_HasRequired_Properties()
    {
        var props = typeof(ExecutionLog).GetProperties().Select(p => p.Name).ToList();

        props.Should().Contain("Id");
        props.Should().Contain("Timestamp");
        props.Should().Contain("SessionId");
        props.Should().Contain("Payload");
        props.Should().Contain("ExitCode");
    }

    [Test]
    public void ExecutionLog_Payload_IsJsonDocument()
    {
#pragma warning disable CA2263
        typeof(ExecutionLog).GetProperty("Payload")!.PropertyType
            .Should().Be(typeof(JsonDocument));
#pragma warning restore CA2263
    }

    [Test]
    public void ExecutionLog_Timestamp_IsDateTimeOffset()
    {
#pragma warning disable CA2263
        typeof(ExecutionLog).GetProperty("Timestamp")!.PropertyType
            .Should().Be(typeof(DateTimeOffset));
#pragma warning restore CA2263
    }
}
