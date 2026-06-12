using System.Text.RegularExpressions;

namespace Scripts.Tests.Guards;

internal sealed class Ef11ForbiddenPatternsTests
{
	private static readonly string SourceRoot = TestPaths.SrcRoot;

	private static IEnumerable<string> EnumerateSourceFiles() => Directory.EnumerateFiles(SourceRoot, "*.cs", SearchOption.AllDirectories);

	[Test]
	public async Task No_MaxByAsync_In_SourceFiles() =>
		await AssertNoMatch(pattern: @"\bMaxByAsync\b", description: "MaxByAsync is EF11-only");

	[Test]
	public async Task No_MinByAsync_In_SourceFiles() =>
		await AssertNoMatch(pattern: @"\bMinByAsync\b", description: "MinByAsync is EF11-only");

	[Test]
	public async Task No_JsonPathExists_In_SourceFiles() =>
		await AssertNoMatch(
			pattern: @"\bJsonPathExists\b",
			description: "JsonPathExists is EF11-only"
		);

	[Test]
	public async Task No_EF11_Namespace_Imports()
	{
		await AssertNoMatch(
			pattern: @"using\s+Microsoft\.EntityFrameworkCore\.Extensions\.EntityFrameworkQueryableExtensions",
			description: "EF11 namespace import is forbidden"
		);
	}

	private static async Task AssertNoMatch(string pattern, string description)
	{
		var regex = new Regex(pattern, RegexOptions.Compiled);
		var violations = new List<string>();

		foreach (var file in EnumerateSourceFiles())
		{
			var content = await File.ReadAllTextAsync(file);
			if (regex.IsMatch(content))
				violations.Add(file);
		}

		await Assert.That(violations).IsEmpty();
	}
}
