namespace Scripts.Tests.StateManager;

internal sealed class StateManagerDeleteTests
{
	[Test]
	public async Task Infrastructure_StateManager_DoesNotExist()
	{
		var type = Type.GetType("Scripts.Infrastructure.StateManager, Scripts");
		await Assert.That(type).IsNull();
	}

	[Test]
	public async Task CorePersistence_StateManager_FileDoesNotExist()
	{
		var filePath = @"C:\Users\Lance\Dev\Scripts\csharp\src\Core\Persistence\StateManager.cs";
		await Assert.That(System.IO.File.Exists(filePath)).IsFalse();
	}

	[Test]
	public async Task DataState_StateManager_IsSoleVersion()
	{
		var type = Type.GetType("Scripts.Data.State.StateManager, Scripts");
		await Assert.That(type).IsNotNull();
	}
}
