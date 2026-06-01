using TUnit;
using FluentAssertions;
using System.Text.Json;

namespace Scripts.Tests.StateManager;

internal sealed class StateManagerNamespaceTests
{
    [Test]
    public void StateManager_ExistsIn_DataStateNamespace()
    {
        var type = Type.GetType("Scripts.Data.State.StateManager, Scripts");
        type.Should().NotBeNull(because: "StateManager must live in Scripts.Data.State namespace");
    }

    [Test]
    public void StateManager_HasJsonIndented_Option()
    {
        var type = Type.GetType("Scripts.Data.State.StateManager, Scripts");
        type.Should().NotBeNull();
        var field = type!.GetField("JsonIndented");
        field.Should().NotBeNull();
        field!.FieldType.Should().Be<JsonSerializerOptions>();
    }

    [Test]
    public void StateManager_HasJsonCompact_Option()
    {
        var type = Type.GetType("Scripts.Data.State.StateManager, Scripts");
        type.Should().NotBeNull();
        var field = type!.GetField("JsonCompact");
        field.Should().NotBeNull();
        field!.FieldType.Should().Be<JsonSerializerOptions>();
    }

    [Test]
    public void StateManager_HasLoadStateAsync_Method()
    {
        var type = Type.GetType("Scripts.Data.State.StateManager, Scripts");
        type.Should().NotBeNull();
        var method = type!.GetMethod("LoadStateAsync");
        method.Should().NotBeNull();
    }

    [Test]
    public void StateManager_HasSaveStateAsync_Method()
    {
        var type = Type.GetType("Scripts.Data.State.StateManager, Scripts");
        type.Should().NotBeNull();
        var method = type!.GetMethod("SaveStateAsync");
        method.Should().NotBeNull();
    }
}
