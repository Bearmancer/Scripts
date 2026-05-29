using TUnit;
using FluentAssertions;

namespace Scripts.Tests.StateManager;

internal sealed class StateManagerDeleteTests
{
    [Test]
    public void Infrastructure_StateManager_DoesNotExist()
    {
        var type = Type.GetType("CSharpScripts.Infrastructure.StateManager, tools");
        type.Should().BeNull(because: "Infrastructure StateManager must be deleted");
    }

    [Test]
    public void CorePersistence_StateManager_FileDoesNotExist()
    {
        var filePath = @"C:\Users\Lance\Dev\Scripts\csharp\src\Core\Persistence\StateManager.cs";
        System.IO.File.Exists(filePath).Should().BeFalse(because: "Core/Persistence/StateManager.cs must be deleted");
    }

    [Test]
    public void DataState_StateManager_IsSoleVersion()
    {
        var type = Type.GetType("CSharpScripts.Data.State.StateManager, tools");
        type.Should().NotBeNull(because: "Only Data.State version should remain");
    }
}
