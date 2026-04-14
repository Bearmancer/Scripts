namespace CSharpScripts.Services.Sync.YouTube;

internal static class YouTubeTranslationService
{
	internal static async Task<List<YouTubeVideo>> TranslateVideosAsync(
		List<YouTubeVideo> videos,
		CancellationToken ct
	)
	{
		if (!Enumerable.Any(videos, v => v.NeedsTranslation))
			return videos;

		return await TranslationService.WithContainerAsync(
			async token =>
			{
				var result = new List<YouTubeVideo>(videos.Count);

				foreach (YouTubeVideo video in videos)
				{
					if (token.IsCancellationRequested)
						return videos;

					if (!video.NeedsTranslation)
					{
						result.Add(item: video);
						continue;
					}

					TranslationResult? titleResult = await TranslationService.TranslateAsync(
						text: video.Title,
						sourceLanguage: video.DetectedLanguage,
						ct: token
					);
					TranslationResult? descResult = await TranslationService.TranslateAsync(
						text: video.Description,
						sourceLanguage: video.DetectedLanguage,
						ct: token
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
			},
			ct: ct
		);
	}

	internal static void ShowTranslationPreview(List<YouTubeVideo> videos)
	{
		List<YouTubeVideo> translated = [.. videos.Where(v => v.TranslatedTitle is { })];
		if (translated.Count == 0)
		{
			UI.Info(message: "No translations available");
			return;
		}

		SpectreTable table = TableExtensions.AddColumn(
			TableExtensions.AddColumn(
				TableExtensions.AddColumn(
					HasTableBorderExtensions.Border(
						new SpectreTable(),
						border: TableBorder.Rounded
					),
					column: "Original Title"
				),
				column: "Translated Title"
			),
			column: "Language"
		);

		foreach (YouTubeVideo video in translated)
		{
			TableExtensions.AddRow(
				table,
				Markup.Escape(text: video.Title),
				Markup.Escape(video.TranslatedTitle ?? "-"),
				Markup.Escape(video.DetectedLanguage ?? "-")
			);
		}

		AnsiConsole.Write(renderable: table);
		UI.Info(message: "{0} of {1} videos translated", translated.Count, videos.Count);
	}
}
