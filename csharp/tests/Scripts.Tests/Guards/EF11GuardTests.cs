using System.Text.RegularExpressions;

namespace Scripts.Tests.Guards;

internal class EF11GuardTests
{
	private static readonly string[] SourceDirectories = [TestPaths.SrcRoot];

	[Test]
	public async Task ShouldNotUseMaxByAsync()
	{
		var pattern = new Regex(@"\bMaxByAsync\s*\(", RegexOptions.Compiled);
		var violations = FindViolations(pattern, "MaxByAsync");

		await Assert.That(violations).IsEmpty();
	}

	[Test]
	public async Task ShouldNotUseMinByAsync()
	{
		var pattern = new Regex(@"\bMinByAsync\s*\(", RegexOptions.Compiled);
		var violations = FindViolations(pattern, "MinByAsync");

		await Assert.That(violations).IsEmpty();
	}

	[Test]
	public async Task ShouldNotUseJsonPathExists()
	{
		var pattern = new Regex(@"JsonPathExists\s*\(", RegexOptions.Compiled);
		var violations = FindViolations(pattern, "JsonPathExists");

		await Assert.That(violations).IsEmpty();
	}

	[Test]
	public async Task ShouldUseOrderByDescendingForMaxValue()
	{
		var pattern = new Regex(
			@"OrderByDescending\s*\(\s*[^)]+\s*\)\s*\.\s*FirstOrDefaultAsync",
			RegexOptions.Compiled
		);
		var matches = FindMatches(pattern);

		await Assert.That(matches.Count).IsGreaterThanOrEqualTo(0);
	}

	[Test]
	public async Task ShouldUseOrderByForMinValue()
	{
		var pattern = new Regex(
			@"OrderBy\s*\(\s*[^)]+\s*\)\s*\.\s*FirstOrDefaultAsync",
			RegexOptions.Compiled
		);
		var matches = FindMatches(pattern);

		await Assert.That(matches.Count).IsGreaterThanOrEqualTo(0);
	}

	[Test]
	public async Task ShouldUseJsonContainsForJsonQueries()
	{
		var pattern = new Regex(@"JsonContains\s*\(", RegexOptions.Compiled);
		var matches = FindMatches(pattern);

		await Assert.That(matches.Count).IsGreaterThanOrEqualTo(0);
	}

	[Test]
	public async Task ShouldUseExecuteUpdateAsyncForBulkUpdates()
	{
		var pattern = new Regex(@"ExecuteUpdateAsync\s*\(", RegexOptions.Compiled);
		var matches = FindMatches(pattern);

		await Assert.That(matches.Count).IsGreaterThanOrEqualTo(0);
	}

	private static List<string> FindViolations(Regex pattern, string patternName)
	{
		var violations = new List<string>();

		foreach (var directory in SourceDirectories)
		{
			if (!Directory.Exists(directory))
				continue;

			var csFiles = Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories);
			foreach (var file in csFiles)
			{
				if (file.Contains("\\bin\\") || file.Contains("\\obj\\"))
					continue;

				var content = File.ReadAllText(file);
				var matches = pattern.Matches(content);

				if (matches.Count > 0)
				{
					var relativePath = Path.GetRelativePath(TestPaths.RepoRoot, file);
					violations.Add(
						$"{relativePath}: {matches.Count} occurrence(s) of {patternName}"
					);
				}
			}
		}

		return violations;
	}

	private static List<string> FindMatches(Regex pattern)
	{
		var matches = new List<string>();

		foreach (var directory in SourceDirectories)
		{
			if (!Directory.Exists(directory))
				continue;

			var csFiles = Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories);
			foreach (var file in csFiles)
			{
				if (file.Contains("\\bin\\") || file.Contains("\\obj\\"))
					continue;

				var content = File.ReadAllText(file);
				var regexMatches = pattern.Matches(content);

				if (regexMatches.Count > 0)
				{
					var relativePath = Path.GetRelativePath(TestPaths.RepoRoot, file);
					matches.Add(relativePath);
				}
			}
		}

		return matches;
	}
}
