using FluentAssertions;
using System.Text;

namespace Scripts.Tests.Language;

internal sealed class LanguageIdentifierCompilationTests
{
    [Test]
    public void LanguageIdentifier_HasNoScreamingSnakeCaseReferences()
    {
        var path = TestPaths.Combine("csharp", "src", "Services", "Language", "LanguageIdentifier.cs");
        var source = File.ReadAllText(path, Encoding.UTF8);
        var forbidden = new[]
        {
            "Language.ENGLISH", "Language.FRENCH", "Language.GERMAN",
            "Language.SPANISH", "Language.PORTUGUESE", "Language.ITALIAN",
            "Language.DUTCH", "Language.RUSSIAN", "Language.CHINESE",
            "Language.JAPANESE", "Language.KOREAN", "Language.ARABIC",
            "Language.HINDI"
        };
        foreach (var token in forbidden)
        {
            source.Should().NotContain(token, because: $"{token} must be PascalCase");
        }
    }
}
