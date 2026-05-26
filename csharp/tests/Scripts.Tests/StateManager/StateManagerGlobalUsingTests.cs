using TUnit;
using FluentAssertions;

namespace Scripts.Tests.StateManager;

internal sealed class StateManagerGlobalUsingTests
{
    [Test]
    public void StateManager_IsAccessible_WithoutNamespaceQualification()
    {
        var indented = CSharpScripts.Data.State.StateManager.JsonIndented;
        indented.Should().NotBeNull();
        var compact = CSharpScripts.Data.State.StateManager.JsonCompact;
        compact.Should().NotBeNull();
    }

    [Test]
    public void Log_IsAccessible_ViaGlobalUsing()
    {
        var logType = typeof(CSharpScripts.Core.Log);
        logType.Should().NotBeNull();
    }
}
