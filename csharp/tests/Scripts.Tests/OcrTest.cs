using Scripts.Services.Read.Ocr;
using FluentAssertions;

namespace Scripts.Tests;

internal class OcrTest
{
	[Test]
	public async Task TestDocumentIntelligenceAuth()
	{
		var provider = AzureDocumentIntelligenceOcrProvider.CreateConfigured();
		provider.Should().NotBeNull();

		byte[] tinyPng = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAACklEQVR4nGMAAQAABQABDQottAAAAABJRU5ErkJggg==");

		try
		{
			var result = await provider.OcrImageAsync(tinyPng, "image/png");
			result.Should().NotBeNull();
			Console.WriteLine("Authentication and OCR succeeded.");
		}
		catch (Azure.RequestFailedException ex) when (ex.Status == 400 && (ex.ErrorCode == "InvalidRequest" || ex.Message.Contains("InvalidContentDimensions")))
		{
			Console.WriteLine("Authentication succeeded (received expected image dimension validation error from service).");
		}
	}
}
