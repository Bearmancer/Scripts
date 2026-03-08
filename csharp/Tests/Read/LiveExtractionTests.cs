using CSharpScripts.Models;
using CSharpScripts.Services.Read;
using FluentAssertions;

namespace CSharpScripts.Tests.Read;

/// <summary>
/// Live end-to-end extraction tests against real article URLs.
/// These tests launch Playwright + BPC and make real network requests.
/// Never skipped — fail first (Red) without the navigation/timing fixes,
/// pass (Green) after those fixes are in place.
/// </summary>
[Category("Integration")]
internal class LiveExtractionTests
{
	/// <summary>
	/// Resolves the bpc-ext directory relative to the test binary output path.
	/// Test output: CSharpScripts.Tests/bin/Debug/net11.0/
	/// Workspace root: 4 levels up from that directory.
	/// </summary>
	private static string GetBpcExtPath()
	{
		var workspaceRoot = Path.GetFullPath(
			Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..")
		);
		return Path.Combine(workspaceRoot, "bpc-ext");
	}

	[Test]
	[Timeout(180_000)]
	public async Task WhenEconomicTimesArticleExtractedThenStructureAndImagesAreRetained(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		// Red: before DOMContentLoaded + 8s BPC wait, ET pages would be blocked or incomplete via fixed
		//      NetworkIdle timeout. Green: navigation + timing changes bring back full content.
		const string url =
			"https://economictimes.indiatimes.com/wealth/invest/beyond-venezuela-how-donald-trumps-oil-power-play-could-reshape-inflation-interest-rates-and-emerging-market-returns/articleshow/126447264.cms";

		StandardExtractor extractor = new(new Uri(url), GetBpcExtPath(), cancellationToken);

		// Act
		ArticleContent content = await extractor.ExtractAsync();

		// Assert — structure
		content.Title.Should().NotBeNullOrWhiteSpace("title must be extracted");
		content
			.Title.Should()
			.NotContain("Page Not Found", "extraction must not land on an error page");
		content
			.Title.Should()
			.ContainEquivalentOf("Venezuela", "extracted title should match the article subject");

		// Assert — body has real content
		content
			.BodyHtml.Length.Should()
			.BeGreaterThan(5_000, "long-form ET article body must be substantial");

		// Assert — photos retained (ET articles always carry data visualisation images)
		content.Images.Should().NotBeEmpty("article photo(s) must be retained in EPUB");

		// Assert — captions/credits retained (ET uses figcaption within article body)
		content
			.BodyHtml.Should()
			.MatchRegex(
				"(?i)<figcaption|<cite|credit",
				"photo annotations (figcaption / credit) must be present in extracted body"
			);
	}

	[Test]
	[Timeout(180_000)]
	public async Task WhenNewYorkerProfileExtractedThenStructureAndImagesAreRetained(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		// Red: DOMContentLoaded not used — NetworkIdle timeout 30s fires, leaves 2s for BPC which
		//      is not enough for Condé Nast JS-heavy pages. Missing images + truncated body result.
		// Green: DOMContentLoaded + 10s settle + 8s BPC window fixes both timing issues.
		const string url = "https://www.newyorker.com/magazine/2026/01/19/marco-rubio-profile";

		StandardExtractor extractor = new(new Uri(url), GetBpcExtPath(), cancellationToken);

		// Act
		ArticleContent content = await extractor.ExtractAsync();

		// Assert — structure
		content.Title.Should().NotBeNullOrWhiteSpace("title must be extracted");
		content
			.Title.Should()
			.NotContain("Page Not Found", "extraction must not land on an error page");
		content
			.Title.Should()
			.ContainEquivalentOf("Rubio", "extracted title must name the profile subject");

		// Assert — body has real content (longform New Yorker profiles run 5000+ words)
		content
			.BodyHtml.Length.Should()
			.BeGreaterThan(5_000, "longform New Yorker profile body must be substantial");

		// Assert — photojournalism images retained
		content.Images.Should().NotBeEmpty("magazine photos must be retained in EPUB");

		// Assert — photo captions retained
		content
			.BodyHtml.Should()
			.MatchRegex(
				"(?i)<figcaption|<cite|credit|caption",
				"photo captions must be preserved in extracted body"
			);
	}

	[Test]
	[Timeout(180_000)]
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

		StandardExtractor extractor = new(new Uri(url), GetBpcExtPath(), cancellationToken);

		// Act
		ArticleContent content = await extractor.ExtractAsync();

		// Assert — structure
		content.Title.Should().NotBeNullOrWhiteSpace("title must be extracted");
		content
			.Title.Should()
			.NotContain("Page Not Found", "extraction must not land on an error page");

		// Assert — body is the full long-form investigation piece
		content
			.BodyHtml.Length.Should()
			.BeGreaterThan(
				5_000,
				"Caravan investigation body must be substantial (was empty/partial before fix)"
			);

		// Assert — article-specific terms appear (proves content is real, not a truncated teaser)
		content
			.BodyHtml.Should()
			.MatchRegex(
				"RSS|Rashtriya|Sangh|Keshav",
				"extracted body must contain investigation-specific terminology"
			);

		// Assert — editorial photos and captions retained
		content.Images.Should().NotBeEmpty("editorial photos must be retained in EPUB");
		content
			.BodyHtml.Should()
			.MatchRegex(
				"(?i)<figcaption|<cite|credit|caption|photographer",
				"photo annotations must be preserved in extracted body"
			);
	}
}
