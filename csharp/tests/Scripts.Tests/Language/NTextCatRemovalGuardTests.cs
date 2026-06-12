namespace Scripts.Tests.Language;

internal sealed class NTextCatRemovalGuardTests
{
	private static readonly string SourceRoot = TestPaths.Combine("csharp", "src");

	private static readonly string[] NTextCatTypes =
	{
		"RankedLanguageIdentifier",
		"RankedLanguageIdentifierFactory",
		"LanguageInfo",
	};

	[Test]
	public async Task No_NTextCat_Types_In_Source()
	{
		var allFiles = Directory.GetFiles(SourceRoot, "*.cs", SearchOption.AllDirectories);

		var violations = new List<string>();
		foreach (var file in allFiles)
		{
			var content = await File.ReadAllTextAsync(file);
			foreach (var typeName in NTextCatTypes)
			{
				if (content.Contains(typeName))
					violations.Add($"{file}: contains {typeName}");
			}
		}

		await Assert.That(violations).IsEmpty();
	}

	[Test]
	public async Task No_Core14_Profile_Xml_Reference_In_Source()
	{
		var allFiles = Directory.GetFiles(SourceRoot, "*.cs", SearchOption.AllDirectories);

		var violations = new List<string>();
		foreach (var file in allFiles)
		{
			var content = await File.ReadAllTextAsync(file);
			if (content.Contains("Core14.profile.xml"))
				violations.Add(file);
		}

		await Assert.That(violations).IsEmpty();
	}
}
