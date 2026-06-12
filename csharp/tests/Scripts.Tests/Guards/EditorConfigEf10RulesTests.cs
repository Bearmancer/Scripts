namespace Scripts.Tests.Guards;

internal sealed class EditorConfigEf10RulesTests
{
	[Test]
	public async Task EditorConfig_Contains_Ef10EnforcementSection()
	{
		var editorConfigPath = TestPaths.Combine(".editorconfig");
		var content = await File.ReadAllTextAsync(editorConfigPath);

		await Assert.That(content).Contains("[*.cs]");
		await Assert.That(content).Contains("dotnet_diagnostic");
	}
}
