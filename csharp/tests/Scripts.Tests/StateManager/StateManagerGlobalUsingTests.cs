namespace Scripts.Tests.StateManager;

internal sealed class StateManagerGlobalUsingTests
{
	[Test]
	public async Task StateManager_IsAccessible_WithoutNamespaceQualification()
	{
		var indented = Scripts.Data.State.StateManager.JsonIndented;
		await Assert.That(indented).IsNotNull();
		var compact = Scripts.Data.State.StateManager.JsonCompact;
		await Assert.That(compact).IsNotNull();
	}

	[Test]
	public async Task Log_IsAccessible_ViaGlobalUsing()
	{
		var logType = typeof(Core.Log);
		await Assert.That(logType).IsNotNull();
	}
}
