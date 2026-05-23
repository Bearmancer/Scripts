#pragma warning disable CA2263 // Prefer generic overload
#pragma warning disable IDE0022 // Use expression body for methods

using TUnit;
using FluentAssertions;
using CSharpScripts.Data.Entities;

namespace Scripts.Tests.Entities;

internal sealed class FailedTaskEntityTests
{
    [Test]
    public void FailedTask_HasRequired_Properties()
    {
        var props = typeof(FailedTask).GetProperties().Select(p => p.Name).ToList();

        props.Should().Contain("Id");
        props.Should().Contain("Operation");
        props.Should().Contain("ErrorMessage");
        props.Should().Contain("CreatedAt");
    }

    [Test]
    public void FailedTask_Id_IsGuid()
    {
        typeof(FailedTask).GetProperty("Id")!.PropertyType.Should().Be(typeof(Guid));
    }

    [Test]
    public void FailedTask_CreatedAt_IsDateTimeOffset()
    {
        typeof(FailedTask).GetProperty("CreatedAt")!.PropertyType
            .Should().Be(typeof(DateTimeOffset));
    }
}
