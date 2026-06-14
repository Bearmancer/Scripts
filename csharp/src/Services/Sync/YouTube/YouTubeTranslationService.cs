namespace Scripts.Services.Sync.YouTube;

internal static class YouTubeTranslationService
{
	internal static async Task<List<YouTubeVideo>> TranslateVideosAsync(
		List<YouTubeVideo> videos,
		CancellationToken ct
	)
	{
		if (!videos.Any(static v => v.NeedsTranslation))
			return videos;

		List<YouTubeVideo> result = new(capacity: videos.Count);

		foreach (YouTubeVideo video in videos)
		{
			if (ct.IsCancellationRequested)
				return videos;

			if (!video.NeedsTranslation)
			{
				result.Add(item: video);
				continue;
			}

			TranslationResult? titleResult = await AzureTranslationService.TranslateAsync(
				text: video.Title,
				sourceLanguage: video.DetectedLanguage,
				ct: ct
			);
			TranslationResult? descResult = await AzureTranslationService.TranslateAsync(
				text: video.Description,
				sourceLanguage: video.DetectedLanguage,
				ct: ct
			);

			if (titleResult is { })
			{
				result.Add(
					video.WithTranslation(
						translatedTitle: titleResult.Translation,
						descResult?.Translation ?? video.Description,
						detectedLanguage: titleResult.DetectedLanguage
					)
				);
			}
			else
				result.Add(item: video);
		}

		return result;
	}

	internal static void ShowTranslationPreview(List<YouTubeVideo> videos)
	{
		List<YouTubeVideo> translated = [.. videos.Where(static v => v.TranslatedTitle is { })];
		if (translated.Count == 0)
		{
			Ui.Info(message: "No translations available");
			return;
		}

		SpectreTable table = new SpectreTable()
			.Border(border: TableBorder.Rounded)
			.AddColumn(column: "Original Title")
			.AddColumn(column: "Translated Title")
			.AddColumn(column: "Language");

		foreach (YouTubeVideo video in translated)
		{
			table.AddRow(
				Markup.Escape(text: video.Title),
				Markup.Escape(video.TranslatedTitle ?? "-"),
				Markup.Escape(video.DetectedLanguage ?? "-")
			);
		}

		AnsiConsole.Write(renderable: table);
		Ui.Info(message: "{0} of {1} videos translated", translated.Count, videos.Count);
	}
}
