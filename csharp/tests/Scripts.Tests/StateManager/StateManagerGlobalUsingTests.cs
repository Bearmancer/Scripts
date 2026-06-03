using TUnit;
using FluentAssertions;

namespace Scripts.Tests.StateManager;

internal sealed class StateManagerGlobalUsingTests
{
    [Test]
    public void StateManager_IsAccessible_WithoutNamespaceQualification()
    {
        var indented = Scripts.Data.State.StateManager.JsonIndented;
        indented.Should().NotBeNull();
        var compact = Scripts.Data.State.StateManager.JsonCompact;
        compact.Should().NotBeNull();
    }

    [Test]
    public void Log_IsAccessible_ViaGlobalUsing()
    {
        var logType = typeof(Scripts.Core.Log);
        logType.Should().NotBeNull();
    }
}
