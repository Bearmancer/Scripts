using CSharpScripts.Services.Language;
using FluentAssertions;
using TUnit;

namespace Scripts.Tests.Language;

internal sealed class LanguageIdentifierTests
{
	[Test]
	public void Detect_English_Returns_eng()
	{
		var result = LanguageIdentifier.Detect(
			"This is a test sentence in English language with enough characters"
		);

		result
			.Should()
			.Be("eng", $"because Lingua must detect English text correctly. Actual: {result}");
	}

	[Test]
	public void Detect_Japanese_Returns_jpn()
	{
		var result = LanguageIdentifier.Detect(
			"これは日本語のテスト文章です十分な文字数があります"
		);

		result.Should().Be("jpn", $"because Lingua must detect Japanese text. Actual: {result}");
	}

	[Test]
	public void Detect_Short_Text_Returns_Null()
	{
		var result = LanguageIdentifier.Detect("hi");

		result.Should().BeNull("because text shorter than 15 characters returns null");
	}

	[Test]
	public void Detect_Empty_Text_Returns_Null()
	{
		var result = LanguageIdentifier.Detect("");

		result.Should().BeNull("because empty string returns null");
	}

	[Test]
	public void Detect_Null_Text_Returns_Null()
	{
		var result = LanguageIdentifier.Detect(null!);

		result.Should().BeNull("because null text returns null");
	}

	[Test]
	public void Detect_Whitespace_Only_Returns_Null()
	{
		var result = LanguageIdentifier.Detect("               ");

		result.Should().BeNull("because whitespace-only string returns null");
	}

	[Test]
	public void IsEnglish_Returns_True_For_English_Text()
	{
		var result = LanguageIdentifier.IsEnglish(
			"The quick brown fox jumps over the lazy dog in the meadow"
		);

		result.Should().BeTrue("because English text must be identified as English");
	}

	[Test]
	public void RequiresTranslation_Returns_True_For_Non_English_Text()
	{
		var result = LanguageIdentifier.RequiresTranslation(
			"Bonjour le monde ceci est une phrase francaise"
		);

		result.Should().BeTrue("because French text requires translation");
	}

	[Test]
	public void RequiresTranslation_Returns_False_For_English_Text()
	{
		var result = LanguageIdentifier.RequiresTranslation(
			"This is a very long English sentence that should not require any translation"
		);

		result.Should().BeFalse("because English text does not require translation");
	}

	[Test]
	public void Detect_Does_Not_Throw_For_Missing_Profile()
	{
		var action = () =>
			LanguageIdentifier.Detect(
				"Some random text that is long enough for detection purposes"
			);

		action.Should().NotThrow("because Lingua embeds language models — no profile file needed");
	}
}
