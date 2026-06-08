using System.Text.RegularExpressions;
using FluentAssertions;
using TUnit;

namespace Scripts.Tests.Guards;

internal sealed class Ef11ForbiddenPatternsTests
{
    
    
    private static readonly string SourceRoot = TestPaths.SrcRoot;

    private static IEnumerable<string> EnumerateSourceFiles()
    {
        return Directory.EnumerateFiles(
            SourceRoot,
            "*.cs",
            SearchOption.AllDirectories
        );
    }

    [Test]
    public Task No_MaxByAsync_In_SourceFiles() =>
        AssertNoMatch(pattern: @"\bMaxByAsync\b", description: "MaxByAsync is EF11-only");

    [Test]
    public Task No_MinByAsync_In_SourceFiles() =>
        AssertNoMatch(pattern: @"\bMinByAsync\b", description: "MinByAsync is EF11-only");

    [Test]
    public Task No_JsonPathExists_In_SourceFiles() =>
        AssertNoMatch(pattern: @"\bJsonPathExists\b", description: "JsonPathExists is EF11-only");

    [Test]
    public Task No_EF11_Namespace_Imports() =>
        AssertNoMatch(
            pattern: @"using\s+Microsoft\.EntityFrameworkCore\.Extensions\.EntityFrameworkQueryableExtensions",
            description: "EF11 namespace import is forbidden"
        );

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

        violations.Should().BeEmpty(
            $"because {description}. Found in: {string.Join(", ", violations)}"
        );
    }
}
