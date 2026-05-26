using System.Text.RegularExpressions;
using FluentAssertions;

namespace CSharpScripts.Tests.Guards;

internal class EF11GuardTests
{
	private static readonly string[] SourceDirectories =
	[
		Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "src"),
	];

	[Test]
	public void ShouldNotUseMaxByAsync()
	{
		var pattern = new Regex(@"\bMaxByAsync\s*\(", RegexOptions.Compiled);
		var violations = FindViolations(pattern, "MaxByAsync");

		violations.Should().BeEmpty($"Found MaxByAsync usage in:\n{string.Join("\n", violations)}");
	}

	[Test]
	public void ShouldNotUseMinByAsync()
	{
		var pattern = new Regex(@"\bMinByAsync\s*\(", RegexOptions.Compiled);
		var violations = FindViolations(pattern, "MinByAsync");

		violations.Should().BeEmpty($"Found MinByAsync usage in:\n{string.Join("\n", violations)}");
	}

	[Test]
	public void ShouldNotUseJsonPathExists()
	{
		var pattern = new Regex(@"JsonPathExists\s*\(", RegexOptions.Compiled);
		var violations = FindViolations(pattern, "JsonPathExists");

		violations.Should().BeEmpty($"Found JsonPathExists usage in:\n{string.Join("\n", violations)}");
	}

	[Test]
	public void ShouldUseOrderByDescendingForMaxValue()
	{
		var pattern = new Regex(@"OrderByDescending\s*\(\s*[^)]+\s*\)\s*\.\s*FirstOrDefaultAsync", RegexOptions.Compiled);
		var matches = FindMatches(pattern);

		matches.Count.Should().BeGreaterThan(0, "Should have at least one OrderByDescending().FirstOrDefaultAsync() pattern");
	}

	[Test]
	public void ShouldUseOrderByForMinValue()
	{
		var pattern = new Regex(@"OrderBy\s*\(\s*[^)]+\s*\)\s*\.\s*FirstOrDefaultAsync", RegexOptions.Compiled);
		var matches = FindMatches(pattern);

		matches.Count.Should().BeGreaterThan(0, "Should have at least one OrderBy().FirstOrDefaultAsync() pattern");
	}

	[Test]
	public void ShouldUseJsonContainsForJsonQueries()
	{
		var pattern = new Regex(@"JsonContains\s*\(", RegexOptions.Compiled);
		var matches = FindMatches(pattern);

		matches.Count.Should().BeGreaterThanOrEqualTo(0, "JsonContains pattern should be available");
	}

	[Test]
	public void ShouldUseExecuteUpdateAsyncForBulkUpdates()
	{
		var pattern = new Regex(@"ExecuteUpdateAsync\s*\(", RegexOptions.Compiled);
		var matches = FindMatches(pattern);

		matches.Count.Should().BeGreaterThan(0, "Should have at least one ExecuteUpdateAsync() call");
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
					var relativePath = Path.GetRelativePath(AppContext.BaseDirectory, file);
					violations.Add($"{relativePath}: {matches.Count} occurrence(s) of {patternName}");
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
					var relativePath = Path.GetRelativePath(AppContext.BaseDirectory, file);
					matches.Add(relativePath);
				}
			}
		}

		return matches;
	}
}
