using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using CSharpScripts.Services.Read;
using FluentAssertions;
using AssertionExtensions = FluentAssertions.AssertionExtensions;

namespace CSharpScripts.Tests.Read;

public class ExtractionRegressionTests
{
	private HtmlParser Parser = null!;

	[Before(hookType: Test)]
	public void SetUp() => Parser = new HtmlParser();

	public class CaravanMagazineScenarios : ExtractionRegressionTests
	{
		[Test]
		public void WhenBpcTimesOutThenPaywallMarkerRemainsAndQualityIsIncomplete()
		{
			// Arrange - Simulates caravan article with paywall still present (BPC didn't complete)
			const string html = """
				<html>
				<head><title>The RSS as an unaccountable organisation</title></head>
				<body>
				    <article class="article-content">
				        <h1>The RSS as an unaccountable organisation</h1>
				        <p>The Rashtriya Swayamsevak Sangh is built to evade scrutiny...</p>
				        <div class="paywall-message" style="display:block;">
				            <p>Subscribe to read the full article</p>
				        </div>
				        <div class="partial-content">
				            Only first few paragraphs visible...
				        </div>
				    </article>
				</body>
				</html>
				""";
			IHtmlDocument doc = Parser.ParseDocument(source: html);

			// Act
			WebExtractionQuality quality = WebExtractionQualityAnalyzer.ClassifyQuality(doc: doc);
			var hasPaywall = WebExtractionQualityAnalyzer.HasPaywallMarker(doc: doc);

			// Assert
			EnumAssertionsExtensions.Should(quality).Be(expected: WebExtractionQuality.Incomplete);
			AssertionExtensions
				.Should(hasPaywall)
				.BeTrue(because: "paywall marker should be detected when BPC times out");
		}

		[Test]
		public void WhenBpcSucceedsThenPaywallIsRemovedAndQualityIsReady()
		{
			// Arrange - Simulates caravan article after successful BPC processing
			const string html = """
				<html>
				<head><title>The RSS as an unaccountable organisation</title></head>
				<body class="bpc-paywall-removed">
				    <article class="article-content">
				        <h1>The RSS as an unaccountable organisation</h1>
				        <p>The Rashtriya Swayamsevak Sangh is built to evade scrutiny...</p>
				        <p>Full article content here after BPC removed the paywall...</p>
				        <p>More paragraphs...</p>
				        <p>Complete article text...</p>
				    </article>
				</body>
				</html>
				""";
			IHtmlDocument doc = Parser.ParseDocument(source: html);

			// Act
			WebExtractionQuality quality = WebExtractionQualityAnalyzer.ClassifyQuality(doc: doc);
			var hasBpcMarker = WebExtractionQualityAnalyzer.HasBpcSuccessMarker(doc: doc);

			// Assert
			EnumAssertionsExtensions.Should(quality).Be(expected: WebExtractionQuality.Ready);
			AssertionExtensions
				.Should(hasBpcMarker)
				.BeTrue(because: "BPC success marker should be present");
		}

		[Test]
		public void WhenArticleIsPubliclyAccessibleThenQualityIsUnknown()
		{
			// Arrange - Simulates open-access article on caravan (no paywall, no BPC needed)
			const string html = """
				<html>
				<head><title>Open Access Article</title></head>
				<body>
				    <article class="article-content">
				        <h1>Open Access Article</h1>
				        <p>Full article content freely available...</p>
				        <p>More content...</p>
				    </article>
				</body>
				</html>
				""";
			IHtmlDocument doc = Parser.ParseDocument(source: html);

			// Act
			WebExtractionQuality quality = WebExtractionQualityAnalyzer.ClassifyQuality(doc: doc);

			// Assert
			EnumAssertionsExtensions.Should(quality).Be(expected: WebExtractionQuality.Unknown);
		}
	}

	//todo diagnoze why I get conflicting error of `should be internal` but then when it is made internal, I am asked to make it public so others can access type.
	//todo remove all comments in all .cs files
	//todo retain man help pages
	public class PaywalledSitesScenarios : ExtractionRegressionTests
	{
		[Test]
		public void WhenNewYorkTimesPaywallDetectedThenQualityIsIncomplete()
		{
			// Arrange
			const string html = """
				<html><body>
				    <div id="gateway-content" class="css-subscription-required">
				        <p>Subscribe to continue reading</p>
				    </div>
				    <article>Limited preview...</article>
				</body></html>
				""";
			IHtmlDocument doc = Parser.ParseDocument(source: html);

			// Act
			WebExtractionQuality quality = WebExtractionQualityAnalyzer.ClassifyQuality(doc: doc);

			// Assert
			EnumAssertionsExtensions.Should(quality).Be(expected: WebExtractionQuality.Incomplete);
		}

		[Test]
		public void WhenMediumMemberOnlyArticleDetectedThenQualityIsIncomplete()
		{
			// Arrange
			const string html = """
				<html><body>
				    <div class="meteredContent members-only">
				        <p>This story is for Medium members only</p>
				    </div>
				</body></html>
				""";
			IHtmlDocument doc = Parser.ParseDocument(source: html);

			// Act
			WebExtractionQuality quality = WebExtractionQualityAnalyzer.ClassifyQuality(doc: doc);

			// Assert
			EnumAssertionsExtensions.Should(quality).Be(expected: WebExtractionQuality.Incomplete);
		}

		[Test]
		public void WhenBpcUnlocksContentThenMarkerIsPresent()
		{
			// Arrange
			const string html = """
				<html><body data-bpc="unlocked">
				    <article>Full content after BPC processing</article>
				</body></html>
				""";
			IHtmlDocument doc = Parser.ParseDocument(source: html);

			// Act
			WebExtractionQuality quality = WebExtractionQualityAnalyzer.ClassifyQuality(doc: doc);

			// Assert
			EnumAssertionsExtensions.Should(quality).Be(expected: WebExtractionQuality.Ready);
		}
	}
}
