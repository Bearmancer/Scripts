using System.Text.Json;

namespace Scripts.Tests.StateManager;

internal sealed class StateManagerNamespaceTests
{
	[Test]
	public async Task StateManager_ExistsIn_DataStateNamespace()
	{
		var type = Type.GetType("Scripts.Data.State.StateManager, Scripts");
		await Assert.That(type).IsNotNull();
	}

	[Test]
	public async Task StateManager_HasJsonIndented_Option()
	{
		var type = Type.GetType("Scripts.Data.State.StateManager, Scripts");
		await Assert.That(type).IsNotNull();
		var field = type!.GetField("JsonIndented");
		await Assert.That(field).IsNotNull();
		await Assert.That(field!.FieldType).IsEqualTo(typeof(JsonSerializerOptions));
	}

	[Test]
	public async Task StateManager_HasJsonCompact_Option()
	{
		var type = Type.GetType("Scripts.Data.State.StateManager, Scripts");
		await Assert.That(type).IsNotNull();
		var field = type!.GetField("JsonCompact");
		await Assert.That(field).IsNotNull();
		await Assert.That(field!.FieldType).IsEqualTo(typeof(JsonSerializerOptions));
	}

	[Test]
	public async Task StateManager_HasLoadStateAsync_Method()
	{
		var type = Type.GetType("Scripts.Data.State.StateManager, Scripts");
		await Assert.That(type).IsNotNull();
		var method = type!.GetMethod("LoadStateAsync");
		await Assert.That(method).IsNotNull();
	}

	[Test]
	public async Task StateManager_HasSaveStateAsync_Method()
	{
		var type = Type.GetType("Scripts.Data.State.StateManager, Scripts");
		await Assert.That(type).IsNotNull();
		var method = type!.GetMethod("SaveStateAsync");
		await Assert.That(method).IsNotNull();
	}
}
