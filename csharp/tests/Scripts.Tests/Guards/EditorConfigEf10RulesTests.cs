using FluentAssertions;
using TUnit;

namespace Scripts.Tests.Guards;

internal sealed class EditorConfigEf10RulesTests
{
    [Test]
    public async Task EditorConfig_Contains_Ef10EnforcementSection()
    {
        var editorConfigPath = TestPaths.Combine(".editorconfig");
        var content = await File.ReadAllTextAsync(editorConfigPath);

        content.Should().Contain(
            "[*.cs]",
            "because .editorconfig must have a C#-specific section"
        );

        content.Should().Contain(
            "dotnet_diagnostic",
            "because .editorconfig must define EF10 enforcement rules"
        );
    }
}
