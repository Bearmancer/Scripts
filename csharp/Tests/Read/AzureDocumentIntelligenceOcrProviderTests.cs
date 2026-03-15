using Azure.AI.DocumentIntelligence;
using CSharpScripts.Services.Read.Ocr;
using FluentAssertions;

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
			pages:
			[
				DocumentIntelligenceModelFactory.DocumentPage(
					pageNumber: 1,
					angle: null,
					width: 100,
					height: 100,
					unit: LengthUnit.Pixel,
					spans: [],
					words: [],
					selectionMarks: [],
					lines: [],
					barcodes: [],
					formulas: []
				),
			],
			paragraphs:
			[
				DocumentIntelligenceModelFactory.DocumentParagraph(
					ParagraphRole.PageHeader,
					"Header",
					[
						DocumentIntelligenceModelFactory.BoundingRegion(
							1,
							[0f, 2f, 100f, 2f, 100f, 4f, 0f, 4f]
						),
					],
					[]
				),
				DocumentIntelligenceModelFactory.DocumentParagraph(
					null,
					"Body text",
					[
						DocumentIntelligenceModelFactory.BoundingRegion(
							1,
							[0f, 20f, 100f, 20f, 100f, 40f, 0f, 40f]
						),
					],
					[]
				),
			],
			tables: [],
			figures: [],
			sections: [],
			keyValuePairs: [],
			styles: [],
			languages: [],
			documents: [],
			warnings: []
		);

		DocumentPageResult extracted = AzureDocumentIntelligenceOcrProvider.ExtractStructured(result);

		extracted.BodyBlocks.Should().Equal("Body text");
		extracted.SkippedHeadersFooters.Should().Be(1);
	}

	[Test]
	public void WhenOnlyLinesAreAvailableThenHeaderFooterHeuristicsUsePolygonPosition()
	{
		AnalyzeResult result = DocumentIntelligenceModelFactory.AnalyzeResult(
			apiVersion: "2024-11-30",
			modelId: "prebuilt-layout",
			contentFormat: null,
			content: "Header\nBody line\nFooter",
			pages:
			[
				DocumentIntelligenceModelFactory.DocumentPage(
					pageNumber: 1,
					angle: null,
					width: 100,
					height: 100,
					unit: LengthUnit.Pixel,
					spans: [],
					words: [],
					selectionMarks: [],
					lines:
					[
						DocumentIntelligenceModelFactory.DocumentLine(
							"Header",
							[0f, 2f, 100f, 2f, 100f, 4f, 0f, 4f],
							[]
						),
						DocumentIntelligenceModelFactory.DocumentLine(
							"Body line",
							[0f, 35f, 100f, 35f, 100f, 45f, 0f, 45f],
							[]
						),
						DocumentIntelligenceModelFactory.DocumentLine(
							"Footer",
							[0f, 96f, 100f, 96f, 100f, 98f, 0f, 98f],
							[]
						),
					],
					barcodes: [],
					formulas: []
				),
			],
			paragraphs: [],
			tables: [],
			figures: [],
			sections: [],
			keyValuePairs: [],
			styles: [],
			languages: [],
			documents: [],
			warnings: []
		);

		DocumentPageResult extracted = AzureDocumentIntelligenceOcrProvider.ExtractStructured(result);

		extracted.BodyBlocks.Should().Equal("Body line");
		extracted.SkippedHeadersFooters.Should().Be(2);
	}
}
