using Scripts.Services.Language;

namespace Scripts.Tests.Language;

internal sealed class LanguageIdentifierTests
{
	[Test]
	public async Task Detect_English_Returns_en()
	{
		var result = LanguageIdentifier.Detect(
			"This is a test sentence in English language with enough characters"
		);

		await Assert.That(result).IsEqualTo("en");
	}

	[Test]
	public async Task Detect_Japanese_Returns_ja()
	{
		var result = LanguageIdentifier.Detect(
			"これは日本語のテスト文章です十分な文字数があります"
		);

		await Assert.That(result).IsEqualTo("ja");
	}

	[Test]
	public async Task Detect_Short_Text_Returns_Null()
	{
		var result = LanguageIdentifier.Detect("hi");

		await Assert.That(result).IsNull();
	}

	[Test]
	public async Task Detect_Empty_Text_Returns_Null()
	{
		var result = LanguageIdentifier.Detect("");

		await Assert.That(result).IsNull();
	}

	[Test]
	public async Task Detect_Null_Text_Returns_Null()
	{
		var result = LanguageIdentifier.Detect(null!);

		await Assert.That(result).IsNull();
	}

	[Test]
	public async Task Detect_Whitespace_Only_Returns_Null()
	{
		var result = LanguageIdentifier.Detect("               ");

		await Assert.That(result).IsNull();
	}

	[Test]
	public async Task IsEnglish_Returns_True_For_English_Text()
	{
		var result = LanguageIdentifier.IsEnglish(
			"The quick brown fox jumps over the lazy dog in the meadow"
		);

		await Assert.That(result).IsTrue();
	}

	[Test]
	public async Task RequiresTranslation_Returns_True_For_Non_English_Text()
	{
		var result = LanguageIdentifier.RequiresTranslation(
			"Bonjour le monde ceci est une phrase francaise"
		);

		await Assert.That(result).IsTrue();
	}

	[Test]
	public async Task RequiresTranslation_Returns_False_For_English_Text()
	{
		var result = LanguageIdentifier.RequiresTranslation(
			"This is a very long English sentence that should not require any translation"
		);

		await Assert.That(result).IsFalse();
	}

	[Test]
	public async Task Detect_Does_Not_Throw_For_Missing_Profile()
	{
		var action = () =>
			LanguageIdentifier.Detect(
				"Some random text that is long enough for detection purposes"
			);

		await Assert.That(() => action()).ThrowsNothing();
	}
}
