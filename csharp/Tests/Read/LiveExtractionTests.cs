using CSharpScripts.Models;
using CSharpScripts.Services.Read;
using AssertionExtensions = FluentAssertions.AssertionExtensions;

namespace CSharpScripts.Tests.Read;

/// <summary>
///     Live end-to-end extraction tests against real article URLs.
///     These tests launch Playwright + BPC and make real network requests.
///     These tests require the workspace `csharp\bpc-ext` fixture.
///     When that fixture is absent in a normal checkout, they are skipped cleanly.
/// </summary>
// todo there is NEVER skipping of tests EVER -- throw error to enforce no skipping of tests ever
[Category(category: "Integration")]
public class LiveExtractionTests
{
	/// <summary>
	///     Resolves the bpc-ext directory relative to the test binary output path.
	///     Test output: CSharpScripts.Tests/bin/Debug/net11.0/
	///     Workspace root: 4 levels up from that directory.
	///     Skips the live integration test when the local BPC fixture is unavailable.
	/// </summary>
	private static string GetBpcExtPath()
	{
		string workspaceRoot = Path.GetFullPath(
			Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..")
		);
		string bpcExtPath = Path.Combine(path1: workspaceRoot, path2: "bpc-ext");

		Skip.Unless(
			Directory.Exists(bpcExtPath),
			$"Live extraction tests require the BPC fixture at '{bpcExtPath}'."
		);

		return bpcExtPath;
	}

	[Test]
	[Timeout(timeoutInMilliseconds: 180_000)]
	public async Task WhenEconomicTimesArticleExtractedThenStructureAndImagesAreRetained(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		// Red: before DOMContentLoaded + 8s BPC wait, ET pages would be blocked or incomplete via fixed
		//      NetworkIdle timeout. Green: navigation + timing changes bring back full content.
		const string url =
			"https://economictimes.indiatimes.com/wealth/invest/beyond-venezuela-how-donald-trumps-oil-power-play-could-reshape-inflation-interest-rates-and-emerging-market-returns/articleshow/126447264.cms";

		StandardExtractor extractor = new(
			new Uri(uriString: url),
			GetBpcExtPath(),
			ct: cancellationToken
		);

		// Act
		ArticleContent content = await extractor.ExtractAsync();

		// Assert — structure
		AssertionExtensions
			.Should(content.Title)
			.NotBeNullOrWhiteSpace(because: "title must be extracted");
		AssertionExtensions
			.Should(content.Title)
			.NotContain(
				unexpected: "Page Not Found",
				because: "extraction must not land on an error page"
			);
		AssertionExtensions
			.Should(content.Title)
			.ContainEquivalentOf(
				expected: "Venezuela",
				because: "extracted title should match the article subject"
			);

		// Assert — body has real content
		AssertionExtensions
			.Should(content.BodyHtml.Length)
			.BeGreaterThan(
				expected: 5_000,
				because: "long-form ET article body must be substantial"
			);

		// Assert — photos retained (ET articles always carry data visualisation images)
		AssertionExtensions
			.Should(content.Images)
			.NotBeEmpty(because: "article photo(s) must be retained in EPUB");

		// Assert — captions/credits retained (ET uses figcaption within article body)
		AssertionExtensions
			.Should(content.BodyHtml)
			.MatchRegex(
				regularExpression: "(?i)<figcaption|<cite|credit",
				because: "photo annotations (figcaption / credit) must be present in extracted body"
			);
	}

	[Test]
	[Timeout(timeoutInMilliseconds: 180_000)]
	public async Task WhenNewYorkerProfileExtractedThenStructureAndImagesAreRetained(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		// Red: DOMContentLoaded not used — NetworkIdle timeout 30s fires, leaves 2s for BPC which
		//      is not enough for Condé Nast JS-heavy pages. Missing images + truncated body result.
		// Green: DOMContentLoaded + 10s settle + 8s BPC window fixes both timing issues.
		const string url = "https://www.newyorker.com/magazine/2026/01/19/marco-rubio-profile";

		StandardExtractor extractor = new(
			new Uri(uriString: url),
			GetBpcExtPath(),
			ct: cancellationToken
		);

		// Act
		ArticleContent content = await extractor.ExtractAsync();

		// Assert — structure
		AssertionExtensions
			.Should(content.Title)
			.NotBeNullOrWhiteSpace(because: "title must be extracted");
		AssertionExtensions
			.Should(content.Title)
			.NotContain(
				unexpected: "Page Not Found",
				because: "extraction must not land on an error page"
			);
		AssertionExtensions
			.Should(content.Title)
			.ContainEquivalentOf(
				expected: "Rubio",
				because: "extracted title must name the profile subject"
			);

		// Assert — body has real content (longform New Yorker profiles run 5000+ words)
		AssertionExtensions
			.Should(content.BodyHtml.Length)
			.BeGreaterThan(
				expected: 5_000,
				because: "longform New Yorker profile body must be substantial"
			);

		// Assert — photojournalism images retained
		AssertionExtensions
			.Should(content.Images)
			.NotBeEmpty(because: "magazine photos must be retained in EPUB");

		// Assert — photo captions retained
		AssertionExtensions
			.Should(content.BodyHtml)
			.MatchRegex(
				regularExpression: "(?i)<figcaption|<cite|credit|caption",
				because: "photo captions must be preserved in extracted body"
			);
	}

	[Test]
	[Timeout(timeoutInMilliseconds: 180_000)]
	public async Task WhenCaravanInvestigationExtractedThenStructureAndImagesAreRetained(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		// Red: archive.is async DOM replacement races against fixed 2s post-scroll delay,
		//      leaving partial or placeholder content in the captured HTML.
		// Green: 8s post-scroll wait gives archive.is fetch + DOM replacement time to complete.
		const string url =
			"https://caravanmagazine.in/politics/rss-unaccountable-organisation-keshav-kunj";

		StandardExtractor extractor = new(
			new Uri(uriString: url),
			GetBpcExtPath(),
			ct: cancellationToken
		);

		// Act
		ArticleContent content = await extractor.ExtractAsync();

		// Assert — structure
		AssertionExtensions
			.Should(content.Title)
			.NotBeNullOrWhiteSpace(because: "title must be extracted");
		AssertionExtensions
			.Should(content.Title)
			.NotContain(
				unexpected: "Page Not Found",
				because: "extraction must not land on an error page"
			);

		// Assert — body is the full long-form investigation piece
		AssertionExtensions
			.Should(content.BodyHtml.Length)
			.BeGreaterThan(
				expected: 5_000,
				because: "Caravan investigation body must be substantial (was empty/partial before fix)"
			);

		// Assert — article-specific terms appear (proves content is real, not a truncated teaser)
		AssertionExtensions
			.Should(content.BodyHtml)
			.MatchRegex(
				regularExpression: "RSS|Rashtriya|Sangh|Keshav",
				because: "extracted body must contain investigation-specific terminology"
			);

		// Assert — editorial photos and captions retained
		AssertionExtensions
			.Should(content.Images)
			.NotBeEmpty(because: "editorial photos must be retained in EPUB");
		AssertionExtensions
			.Should(content.BodyHtml)
			.MatchRegex(
				regularExpression: "(?i)<figcaption|<cite|credit|caption|photographer",
				because: "photo annotations must be preserved in extracted body"
			);
	}
}
