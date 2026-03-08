using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using CSharpScripts.Services.Read;
using FluentAssertions;

namespace CSharpScripts.Tests.Read;

internal class StandardExtractorTests
{
	private HtmlParser Parser = null!;

	[Before(Test)]
	public void SetUp() => Parser = new HtmlParser();

	internal class WhenRemovingUnwantedElements : StandardExtractorTests
	{
		[Test]
		public void ThenScriptElementsAreRemoved()
		{
			// Arrange
			const string html = """
				<html><body>
				    <div>Keep this</div>
				    <script>Remove this</script>
				    <p>Keep this too</p>
				</body></html>
				""";
			IHtmlDocument doc = Parser.ParseDocument(html);

			// Act
			HtmlCleanupHelper.RemoveUnwantedElements(doc);

			// Assert
			doc.QuerySelectorAll("script").Should().BeEmpty();
			doc.Body!.InnerHtml.Should().Contain("Keep this");
		}

		[Test]
		public void ThenStyleElementsAreRemoved()
		{
			// Arrange
			const string html = """
				<html><body>
				    <style>.unwanted { color: red; }</style>
				    <p>Content</p>
				</body></html>
				""";
			IHtmlDocument doc = Parser.ParseDocument(html);

			// Act
			HtmlCleanupHelper.RemoveUnwantedElements(doc);

			// Assert
			doc.QuerySelectorAll("style").Should().BeEmpty();
		}

		[Test]
		public void ThenIframeElementsAreRemoved()
		{
			// Arrange
			const string html = """
				<html><body>
				    <iframe src="ads.html"></iframe>
				    <article>Content</article>
				</body></html>
				""";
			IHtmlDocument doc = Parser.ParseDocument(html);

			// Act
			HtmlCleanupHelper.RemoveUnwantedElements(doc);

			// Assert
			doc.QuerySelectorAll("iframe").Should().BeEmpty();
		}

		[Test]
		public void ThenFormAndButtonElementsAreRemoved()
		{
			// Arrange
			const string html = """
				<html><body>
				    <form><input /></form>
				    <button>Click</button>
				    <p>Text</p>
				</body></html>
				""";
			IHtmlDocument doc = Parser.ParseDocument(html);

			// Act
			HtmlCleanupHelper.RemoveUnwantedElements(doc);

			// Assert
			doc.QuerySelectorAll("form, button").Should().BeEmpty();
		}

		[Test]
		public void ThenAdvertisementAndPromotionalDivsAreRemovedAcrossMultipleSites()
		{
			// Arrange - Real-world ad patterns from Economist, NYT, Business Standard, Caravan, New Yorker
			const string html = """
				<html><body>
				    <article>
				        <p>Article content starts here.</p>
				        <div class="ad-container">Advertisement</div>
				        <p>More article content.</p>
				        <div class="advertisement">Sponsored content</div>
				        <p>Continuing article.</p>
				        <aside class="promo-box">Subscribe now!</aside>
				        <p>Final paragraph.</p>
				        <div id="ad-slot-1">Ad placeholder</div>
				    </article>
				</body></html>
				""";
			IHtmlDocument doc = Parser.ParseDocument(html);

			// Act
			HtmlCleanupHelper.RemoveUnwantedElements(doc);

			// Assert
			doc.QuerySelector(".ad-container")
				.Should()
				.BeNull("ad-container divs should be removed");
			doc.QuerySelector(".advertisement")
				.Should()
				.BeNull("advertisement divs should be removed");
			doc.QuerySelector(".promo-box").Should().BeNull("promo-box elements should be removed");
			doc.QuerySelector("#ad-slot-1").Should().BeNull("ad-slot elements should be removed");
			doc.QuerySelectorAll("p")
				.Should()
				.HaveCount(4, "article paragraphs should be preserved");
		}
	}

	internal class WhenUnwrappingImageAnchors : StandardExtractorTests
	{
		[Test]
		public void ThenImageIsMovedOutOfAnchor()
		{
			// Arrange
			const string html = """
				<html><body>
				    <a href="link.html"><img src="photo.jpg" /></a>
				</body></html>
				""";
			IHtmlDocument doc = Parser.ParseDocument(html);

			// Act
			HtmlCleanupHelper.UnwrapImageAnchors(doc);

			// Assert
			doc.QuerySelector("a > img").Should().BeNull();
			doc.QuerySelector("img").Should().NotBeNull();
		}

		[Test]
		public void ThenAnchorIsRemovedAfterUnwrap()
		{
			// Arrange
			const string html = """
				<html><body>
				    <div><a href="link.html"><img src="photo.jpg" /></a></div>
				</body></html>
				""";
			IHtmlDocument doc = Parser.ParseDocument(html);

			// Act
			HtmlCleanupHelper.UnwrapImageAnchors(doc);

			// Assert
			doc.QuerySelector("a").Should().BeNull();
			doc.QuerySelector("div > img").Should().NotBeNull();
		}

		[Test]
		public void ThenMultipleImagesInSameAnchorAreUnwrapped()
		{
			// Arrange
			const string html = """
				<html><body>
				    <a href="link.html">
				        <img src="photo1.jpg" />
				        <img src="photo2.jpg" />
				    </a>
				</body></html>
				""";
			IHtmlDocument doc = Parser.ParseDocument(html);

			// Act
			HtmlCleanupHelper.UnwrapImageAnchors(doc);

			// Assert
			doc.QuerySelectorAll("img").Should().HaveCount(2);
			doc.QuerySelector("a").Should().BeNull();
		}
	}

	internal class WhenDetectingPaywallMarkers : StandardExtractorTests
	{
		[Test]
		public void ThenCommonPaywallClassIsDetected()
		{
			// Arrange
			const string html = """
				<html><body>
				    <div class="paywall-message">Subscribe to read more</div>
				    <article>Partial content...</article>
				</body></html>
				""";
			IHtmlDocument doc = Parser.ParseDocument(html);

			// Act
			var hasPaywall = WebExtractionQualityAnalyzer.HasPaywallMarker(doc);

			// Assert
			hasPaywall.Should().BeTrue();
		}

		[Test]
		public void ThenSubscriptionPromptIsDetected()
		{
			// Arrange
			const string html = """
				<html><body>
				    <div id="subscription-required">Sign up for full access</div>
				</body></html>
				""";
			IHtmlDocument doc = Parser.ParseDocument(html);

			// Act
			var hasPaywall = WebExtractionQualityAnalyzer.HasPaywallMarker(doc);

			// Assert
			hasPaywall.Should().BeTrue();
		}

		[Test]
		public void ThenCleanContentWithoutPaywallIsNotDetected()
		{
			// Arrange
			const string html = """
				<html><body>
				    <article>Full article content here</article>
				</body></html>
				""";
			IHtmlDocument doc = Parser.ParseDocument(html);

			// Act
			var hasPaywall = WebExtractionQualityAnalyzer.HasPaywallMarker(doc);

			// Assert
			hasPaywall.Should().BeFalse();
		}
	}

	internal class WhenDetectingBpcSuccessMarkers : StandardExtractorTests
	{
		[Test]
		public void ThenBpcMarkerClassIsDetected()
		{
			// Arrange
			const string html = """
				<html><body>
				    <div class="bpc-paywall-removed">BPC processed</div>
				    <article>Full content</article>
				</body></html>
				""";
			IHtmlDocument doc = Parser.ParseDocument(html);

			// Act
			var hasBpcMarker = WebExtractionQualityAnalyzer.HasBpcSuccessMarker(doc);

			// Assert
			hasBpcMarker.Should().BeTrue();
		}

		[Test]
		public void ThenBpcDataAttributeIsDetected()
		{
			// Arrange
			const string html = """
				<html><body data-bpc="unlocked">
				    <article>Full content</article>
				</body></html>
				""";
			IHtmlDocument doc = Parser.ParseDocument(html);

			// Act
			var hasBpcMarker = WebExtractionQualityAnalyzer.HasBpcSuccessMarker(doc);

			// Assert
			hasBpcMarker.Should().BeTrue();
		}

		[Test]
		public void ThenCleanContentWithoutBpcMarkerIsNotDetected()
		{
			// Arrange
			const string html = """
				<html><body>
				    <article>Content</article>
				</body></html>
				""";
			IHtmlDocument doc = Parser.ParseDocument(html);

			// Act
			var hasBpcMarker = WebExtractionQualityAnalyzer.HasBpcSuccessMarker(doc);

			// Assert
			hasBpcMarker.Should().BeFalse();
		}
	}

	internal class WhenClassifyingExtractionQuality : StandardExtractorTests
	{
		[Test]
		public void ThenIncompleteWhenPaywallDetected()
		{
			// Arrange
			const string html = """
				<html><body>
				    <div class="paywall">Subscribe</div>
				    <article>Partial...</article>
				</body></html>
				""";
			IHtmlDocument doc = Parser.ParseDocument(html);

			// Act
			WebExtractionQuality quality = WebExtractionQualityAnalyzer.ClassifyQuality(doc);

			// Assert
			quality.Should().Be(WebExtractionQuality.Incomplete);
		}

		[Test]
		public void ThenReadyWhenBpcSuccessMarkerPresent()
		{
			// Arrange
			const string html = """
				<html><body class="bpc-paywall-removed">
				    <article>Full content here</article>
				</body></html>
				""";
			IHtmlDocument doc = Parser.ParseDocument(html);

			// Act
			WebExtractionQuality quality = WebExtractionQualityAnalyzer.ClassifyQuality(doc);

			// Assert
			quality.Should().Be(WebExtractionQuality.Ready);
		}

		[Test]
		public void ThenUnknownWhenNoMarkersPresent()
		{
			// Arrange
			const string html = """
				<html><body>
				    <article>Some content</article>
				</body></html>
				""";
			IHtmlDocument doc = Parser.ParseDocument(html);

			// Act
			WebExtractionQuality quality = WebExtractionQualityAnalyzer.ClassifyQuality(doc);

			// Assert
			quality.Should().Be(WebExtractionQuality.Unknown);
		}
	}

	internal class WhenPreservingImageStructure : StandardExtractorTests
	{
		[Test]
		public void ThenTheEconomistImageStructureIsRetained()
		{
			// Arrange
			const string html = """
				<html><body>
				    <article>
				        <p>Article text before image.</p>
				        <figure class="article__image">
				            <img src="economist-photo.jpg" alt="Economic data visualization" />
				            <figcaption>
				                GDP growth across regions.
				                <span class="image-credit">Photograph: Reuters</span>
				            </figcaption>
				        </figure>
				        <p>Article text after image.</p>
				    </article>
				</body></html>
				""";
			IHtmlDocument doc = Parser.ParseDocument(html);

			// Act
			HtmlCleanupHelper.RemoveUnwantedElements(doc);

			// Assert
			doc.QuerySelector("figure").Should().NotBeNull("figure element should be retained");
			doc.QuerySelector("figure img")
				.Should()
				.NotBeNull("img within figure should be retained");
			doc.QuerySelector("figcaption").Should().NotBeNull("figcaption should be retained");
			doc.QuerySelector("figcaption")!
				.TextContent.Should()
				.Contain("GDP growth across regions");
			doc.QuerySelector(".image-credit").Should().NotBeNull("credit span should be retained");
			doc.QuerySelector(".image-credit")!.TextContent.Should().Contain("Reuters");
		}

		[Test]
		public void ThenBusinessStandardImageStructureIsRetained()
		{
			// Arrange
			const string html = """
				<html><body>
				    <div class="story-content">
				        <p>Business news content.</p>
				        <figure class="story-image">
				            <img src="business-photo.jpg" alt="Stock market" />
				            <figcaption class="photo-caption">
				                Traders at the stock exchange.
				                <cite class="photographer">PTI Photo</cite>
				            </figcaption>
				        </figure>
				        <p>More business news.</p>
				    </div>
				</body></html>
				""";
			IHtmlDocument doc = Parser.ParseDocument(html);

			// Act
			HtmlCleanupHelper.RemoveUnwantedElements(doc);

			// Assert
			doc.QuerySelector("figure").Should().NotBeNull("figure element should be retained");
			doc.QuerySelector("figure img")
				.Should()
				.NotBeNull("img within figure should be retained");
			doc.QuerySelector("figcaption").Should().NotBeNull("figcaption should be retained");
			doc.QuerySelector("figcaption")!
				.TextContent.Should()
				.Contain("Traders at the stock exchange");
			doc.QuerySelector("cite.photographer")
				.Should()
				.NotBeNull("photographer cite should be retained");
			doc.QuerySelector("cite.photographer")!.TextContent.Should().Contain("PTI Photo");
		}

		[Test]
		public void ThenCaravanMagazineImageStructureIsRetained()
		{
			// Arrange
			const string html = """
				<html><body>
				    <article class="article-content">
				        <p>Long-form journalism begins here.</p>
				        <figure>
				            <img src="caravan-photo.jpg" alt="Documentary photograph" />
				            <figcaption>
				                A scene from the investigation.
				                <span class="photo-credit">Photograph by John Doe</span>
				            </figcaption>
				        </figure>
				        <p>Investigation continues.</p>
				    </article>
				</body></html>
				""";
			IHtmlDocument doc = Parser.ParseDocument(html);

			// Act
			HtmlCleanupHelper.RemoveUnwantedElements(doc);

			// Assert
			doc.QuerySelector("figure").Should().NotBeNull("figure element should be retained");
			doc.QuerySelector("figure img")
				.Should()
				.NotBeNull("img within figure should be retained");
			doc.QuerySelector("figcaption").Should().NotBeNull("figcaption should be retained");
			doc.QuerySelector("figcaption")!
				.TextContent.Should()
				.Contain("A scene from the investigation");
			doc.QuerySelector(".photo-credit")
				.Should()
				.NotBeNull("photo-credit span should be retained");
			doc.QuerySelector(".photo-credit")!.TextContent.Should().Contain("John Doe");
		}

		[Test]
		public void ThenNewYorkerImageStructureIsRetained()
		{
			// Arrange
			const string html = """
				<html><body>
				    <div class="ArticlePageChunks">
				        <p>New Yorker narrative.</p>
				        <figure class="ArticleInlineMediaFigure">
				            <picture>
				                <source srcset="newyorker-lg.jpg" media="(min-width: 768px)" />
				                <img src="newyorker-sm.jpg" alt="Cultural commentary" />
				            </picture>
				            <figcaption>
				                <span class="caption">An artistic interpretation.</span>
				                <span class="credit">Illustration by Jane Smith</span>
				            </figcaption>
				        </figure>
				        <p>Analysis continues.</p>
				    </div>
				</body></html>
				""";
			IHtmlDocument doc = Parser.ParseDocument(html);

			// Act
			HtmlCleanupHelper.RemoveUnwantedElements(doc);

			// Assert
			doc.QuerySelector("figure").Should().NotBeNull("figure element should be retained");
			doc.QuerySelector("picture").Should().NotBeNull("picture element should be retained");
			doc.QuerySelector("img").Should().NotBeNull("img should be retained");
			doc.QuerySelector("figcaption").Should().NotBeNull("figcaption should be retained");
			doc.QuerySelector("figcaption .caption")
				.Should()
				.NotBeNull("caption span should be retained");
			doc.QuerySelector("figcaption .caption")!
				.TextContent.Should()
				.Contain("artistic interpretation");
			doc.QuerySelector("figcaption .credit")
				.Should()
				.NotBeNull("credit span should be retained");
			doc.QuerySelector("figcaption .credit")!.TextContent.Should().Contain("Jane Smith");
		}

		[Test]
		public void ThenNewYorkTimesImageStructureIsRetained()
		{
			// Arrange
			const string html = """
				<html><body>
				    <article>
				        <p>New York Times reporting.</p>
				        <figure class="sizeFull">
				            <picture>
				                <source srcset="nyt-photo-1200.jpg 1200w, nyt-photo-800.jpg 800w" />
				                <img src="nyt-photo-800.jpg" alt="News photograph" />
				            </picture>
				            <figcaption>
				                <span class="caption-text">
				                    Protesters gathered in the capital.
				                </span>
				                <cite class="credit">Chang W. Lee/The New York Times</cite>
				            </figcaption>
				        </figure>
				        <p>Story developments follow.</p>
				    </article>
				</body></html>
				""";
			IHtmlDocument doc = Parser.ParseDocument(html);

			// Act
			HtmlCleanupHelper.RemoveUnwantedElements(doc);

			// Assert
			doc.QuerySelector("figure").Should().NotBeNull("figure element should be retained");
			doc.QuerySelector("picture").Should().NotBeNull("picture element should be retained");
			doc.QuerySelector("img").Should().NotBeNull("img should be retained");
			doc.QuerySelector("figcaption").Should().NotBeNull("figcaption should be retained");
			doc.QuerySelector(".caption-text")
				.Should()
				.NotBeNull("caption-text span should be retained");
			doc.QuerySelector(".caption-text")!.TextContent.Should().Contain("Protesters gathered");
			doc.QuerySelector("cite.credit").Should().NotBeNull("credit cite should be retained");
			doc.QuerySelector("cite.credit")!.TextContent.Should().Contain("New York Times");
		}

		[Test]
		public void ThenMultipleImagesWithCaptionsAreAllRetained()
		{
			// Arrange
			const string html = """
				<html><body>
				    <article>
				        <p>Article with multiple images.</p>
				        <figure>
				            <img src="photo1.jpg" alt="First" />
				            <figcaption>First image caption</figcaption>
				        </figure>
				        <p>Middle content.</p>
				        <figure>
				            <img src="photo2.jpg" alt="Second" />
				            <figcaption>Second image caption</figcaption>
				        </figure>
				        <p>Concluding text.</p>
				    </article>
				</body></html>
				""";
			IHtmlDocument doc = Parser.ParseDocument(html);

			// Act
			HtmlCleanupHelper.RemoveUnwantedElements(doc);

			// Assert
			doc.QuerySelectorAll("figure")
				.Should()
				.HaveCount(2, "both figures should be retained");
			doc.QuerySelectorAll("figcaption")
				.Should()
				.HaveCount(2, "both figcaptions should be retained");
			doc.QuerySelectorAll("img").Should().HaveCount(2, "both images should be retained");
		}

		[Test]
		public void ThenImageOrderingRelativeToTextIsPreserved()
		{
			// Arrange
			const string html = """
				<html><body>
				    <article>
				        <p>Paragraph 1</p>
				        <figure><img src="photo.jpg" /><figcaption>Caption</figcaption></figure>
				        <p>Paragraph 2</p>
				    </article>
				</body></html>
				""";
			IHtmlDocument doc = Parser.ParseDocument(html);

			// Act
			HtmlCleanupHelper.RemoveUnwantedElements(doc);

			// Assert
			var article = (IHtmlElement)doc.QuerySelector("article")!;
			IHtmlCollection<IElement> children = article.Children;
			children.Length.Should().Be(3);
			children[0].TagName.Should().Be("P");
			children[0].TextContent.Should().Contain("Paragraph 1");
			children[1].TagName.Should().Be("FIGURE");
			children[2].TagName.Should().Be("P");
			children[2].TextContent.Should().Contain("Paragraph 2");
		}
	}

	internal class WhenClassifyingArticleQuality : StandardExtractorTests
	{
		[Test]
		public void ThenReadyWhenSmartReaderMarksArticleReadableAndComplete()
		{
			// Arrange — SmartReader completed and found readable content with char length > 0.
			// No hardcoded word/char count: IsReadable is SmartReader's own content-scoring signal.

			// Act
			WebExtractionQuality quality = WebExtractionQualityAnalyzer.ClassifyArticleQuality(
				completed: true,
				isReadable: true,
				charLength: 8000
			);

			// Assert
			quality.Should().Be(WebExtractionQuality.Ready);
		}

		[Test]
		public void ThenIncompleteWhenSmartReaderMarksArticleNotReadable()
		{
			// Arrange — SmartReader could not find enough article structure.
			// Happens when BPC hasn't replaced gated content yet, or page is a 404/error page.

			// Act
			WebExtractionQuality quality = WebExtractionQualityAnalyzer.ClassifyArticleQuality(
				completed: true,
				isReadable: false,
				charLength: 200
			);

			// Assert
			quality.Should().Be(WebExtractionQuality.Incomplete);
		}

		[Test]
		public void ThenIncompleteWhenSmartReaderDidNotComplete()
		{
			// Arrange — extraction was interrupted or timed out.

			// Act
			WebExtractionQuality quality = WebExtractionQualityAnalyzer.ClassifyArticleQuality(
				completed: false,
				isReadable: false,
				charLength: 0
			);

			// Assert
			quality.Should().Be(WebExtractionQuality.Incomplete);
		}

		[Test]
		public void ThenIncompleteWhenCharLengthIsZeroEvenIfReadableFlagIsSet()
		{
			// Arrange — edge case: IsReadable true but empty content.

			// Act
			WebExtractionQuality quality = WebExtractionQualityAnalyzer.ClassifyArticleQuality(
				completed: true,
				isReadable: true,
				charLength: 0
			);

			// Assert
			quality.Should().Be(WebExtractionQuality.Incomplete);
		}

		[Test]
		public void ThenDiagnosticMessageIncludesCharCount()
		{
			// Act
			var message = WebExtractionQualityAnalyzer.GetDiagnosticMessage(
				WebExtractionQuality.Ready,
				42_500
			);

			// Assert — formatted number should appear
			message.Should().Contain("42", "char count should appear in the diagnostic message");
			message.Should().Contain("COMPLETE");
			message.Should().Contain("chars");
		}

		[Test]
		public void ThenDiagnosticMessageForIncompleteIncludesHint()
		{
			// Act
			var message = WebExtractionQualityAnalyzer.GetDiagnosticMessage(
				WebExtractionQuality.Incomplete,
				42
			);

			// Assert
			message.Should().Contain("INCOMPLETE");
			message.Should().Contain("42");
		}

		[Test]
		public void ThenCountWordsStripsHtmlTags()
		{
			// Arrange
			const string html = "<p>Hello <strong>world</strong> test</p>";

			// Act
			var count = WebExtractionQualityAnalyzer.CountWords(html);

			// Assert
			count.Should().Be(3, "HTML tags must not count as words");
		}
	}
}
