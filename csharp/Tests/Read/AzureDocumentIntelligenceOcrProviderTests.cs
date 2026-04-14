using Azure.AI.DocumentIntelligence;
using CSharpScripts.Services.Read.Ocr;
using AssertionExtensions = FluentAssertions.AssertionExtensions;

namespace CSharpScripts.Tests.Read;

internal sealed class AzureDocumentIntelligenceOcrProviderTests
{
	[Test]
	public void WhenParagraphRoleIsHeaderFooterThenItIsExcludedFromStructuredOutput()
	{
		AnalyzeResult result = DocumentIntelligenceModelFactory.AnalyzeResult(
			apiVersion: "2024-11-30",
			modelId: "prebuilt-layout",
			contentFormat: null,
			content: "Header\nBody text",
			[
				DocumentIntelligenceModelFactory.DocumentPage(
					pageNumber: 1,
					angle: null,
					width: 100,
					height: 100,
					unit: LengthUnit.Pixel,
					[],
					[],
					[],
					[],
					[],
					[]
				),
			],
			[
				DocumentIntelligenceModelFactory.DocumentParagraph(
					role: ParagraphRole.PageHeader,
					content: "Header",
					[
						DocumentIntelligenceModelFactory.BoundingRegion(
							pageNumber: 1,
							[0f, 2f, 100f, 2f, 100f, 4f, 0f, 4f]
						),
					],
					[]
				),
				DocumentIntelligenceModelFactory.DocumentParagraph(
					role: null,
					content: "Body text",
					[
						DocumentIntelligenceModelFactory.BoundingRegion(
							pageNumber: 1,
							[0f, 20f, 100f, 20f, 100f, 40f, 0f, 40f]
						),
					],
					[]
				),
			],
			[],
			[],
			[],
			[],
			[],
			[],
			[],
			[]
		);

		DocumentPageResult extracted = AzureDocumentIntelligenceOcrProvider.ExtractStructured(
			result: result
		);

		AssertionExtensions.Should(extracted.BodyBlocks).Equal("Body text");
		AssertionExtensions.Should(extracted.SkippedHeadersFooters).Be(expected: 1);
	}

	[Test]
	public void WhenOnlyLinesAreAvailableThenHeaderFooterHeuristicsUsePolygonPosition()
	{
		AnalyzeResult result = DocumentIntelligenceModelFactory.AnalyzeResult(
			apiVersion: "2024-11-30",
			modelId: "prebuilt-layout",
			contentFormat: null,
			content: "Header\nBody line\nFooter",
			[
				DocumentIntelligenceModelFactory.DocumentPage(
					pageNumber: 1,
					angle: null,
					width: 100,
					height: 100,
					unit: LengthUnit.Pixel,
					[],
					[],
					[],
					[
						DocumentIntelligenceModelFactory.DocumentLine(
							content: "Header",
							[0f, 2f, 100f, 2f, 100f, 4f, 0f, 4f],
							[]
						),
						DocumentIntelligenceModelFactory.DocumentLine(
							content: "Body line",
							[0f, 35f, 100f, 35f, 100f, 45f, 0f, 45f],
							[]
						),
						DocumentIntelligenceModelFactory.DocumentLine(
							content: "Footer",
							[0f, 96f, 100f, 96f, 100f, 98f, 0f, 98f],
							[]
						),
					],
					[],
					[]
				),
			],
			[],
			[],
			[],
			[],
			[],
			[],
			[],
			[],
			[]
		);

		DocumentPageResult extracted = AzureDocumentIntelligenceOcrProvider.ExtractStructured(
			result: result
		);

		AssertionExtensions.Should(extracted.BodyBlocks).Equal("Body line");
		AssertionExtensions.Should(extracted.SkippedHeadersFooters).Be(expected: 2);
	}
}
