using Azure;
using Azure.AI.Vision.ImageAnalysis;

namespace Scripts.Services.Language;

internal static class AzureVisionService
{
	private static readonly ImageAnalysisClient? Client = string.IsNullOrWhiteSpace(
		Secrets.AzureVisionEndpoint
	)
		? null
		: new ImageAnalysisClient(
			new Uri(Secrets.AzureVisionEndpoint),
			Core.Auth.AzureAuth.Credential
		);

	internal static bool IsConfigured => !string.IsNullOrWhiteSpace(Secrets.AzureVisionEndpoint);

	internal static async Task<string?> ExtractTextAsync(
		byte[] imageBytes,
		CancellationToken ct = default
	)
	{
		_ = imageBytes ?? throw new ArgumentNullException(nameof(imageBytes));
		using var track = Log.Track(new { imageBytesLength = imageBytes.Length });

		if (imageBytes.Length == 0)
			throw new ArgumentException("Image bytes cannot be empty.", nameof(imageBytes));

		if (Client is null)
			return null;

		try
		{
			Response<ImageAnalysisResult> response = await Client
				.AnalyzeAsync(
					imageData: BinaryData.FromBytes(imageBytes),
					visualFeatures: VisualFeatures.Read,
					options: new ImageAnalysisOptions { GenderNeutralCaption = true },
					cancellationToken: ct
				)
				.ConfigureAwait(continueOnCapturedContext: false);

			return ConcatReadBlocks(response.Value?.Read);
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			Log.Warning("Azure Vision OCR failed: {Error}", ex.Message);
			return null;
		}
	}

	internal static async Task<string?> CaptionAsync(
		byte[] imageBytes,
		CancellationToken ct = default
	)
	{
		_ = imageBytes ?? throw new ArgumentNullException(nameof(imageBytes));
		using var track = Log.Track(new { imageBytesLength = imageBytes.Length });

		if (imageBytes.Length == 0)
			throw new ArgumentException("Image bytes cannot be empty.", nameof(imageBytes));

		if (Client is null)
			return null;

		try
		{
			Response<ImageAnalysisResult> response = await Client
				.AnalyzeAsync(
					imageData: BinaryData.FromBytes(imageBytes),
					visualFeatures: VisualFeatures.Caption,
					options: new ImageAnalysisOptions { GenderNeutralCaption = true },
					cancellationToken: ct
				)
				.ConfigureAwait(continueOnCapturedContext: false);

			return response.Value?.Caption?.Text;
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			Log.Warning("Azure Vision caption failed: {Error}", ex.Message);
			return null;
		}
	}

	internal static async Task<string?> TagAsync(
		byte[] imageBytes,
		CancellationToken ct = default
	)
	{
		_ = imageBytes ?? throw new ArgumentNullException(nameof(imageBytes));
		using var track = Log.Track(new { imageBytesLength = imageBytes.Length });

		if (imageBytes.Length == 0)
			throw new ArgumentException("Image bytes cannot be empty.", nameof(imageBytes));

		if (Client is null)
			return null;

		try
		{
			Response<ImageAnalysisResult> response = await Client
				.AnalyzeAsync(
					imageData: BinaryData.FromBytes(imageBytes),
					visualFeatures: VisualFeatures.Tags,
					options: new ImageAnalysisOptions { GenderNeutralCaption = true },
					cancellationToken: ct
				)
				.ConfigureAwait(continueOnCapturedContext: false);

			return JoinTags(response.Value?.Tags);
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			Log.Warning("Azure Vision tagging failed: {Error}", ex.Message);
			return null;
		}
	}

	private static string? ConcatReadBlocks(ReadResult? read)
	{
		if (read?.Blocks is not { Count: > 0 } blocks)
			return null;
		List<string> lines = new();
		foreach (DetectedTextBlock block in blocks)
		{
			if (block.Lines is null)
				continue;
			foreach (DetectedTextLine line in block.Lines)
			{
				if (line.Text is { Length: > 0 } text)
					lines.Add(text);
			}
		}
		return lines.Count == 0 ? null : string.Join(" ", lines);
	}

	private static string? JoinTags(TagsResult? tags)
	{
		if (tags?.Values is not { Count: > 0 } values)
			return null;
		List<string> names = new(capacity: values.Count);
		foreach (DetectedTag tag in values)
		{
			if (tag.Name is { Length: > 0 } name)
				names.Add(name);
		}
		return names.Count == 0 ? null : string.Join(", ", names);
	}
}
