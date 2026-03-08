namespace CSharpScripts.Services.Sync.YouTube;

internal static class YouTubeTranslationService
{
	internal static async Task<List<YouTubeVideo>> TranslateVideosAsync(
		List<YouTubeVideo> videos,
		CancellationToken ct
	)
	{
		var needsTranslation = videos.Where(v => v.NeedsTranslation).ToList();
		if (needsTranslation.Count == 0)
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
						result.Add(video);
						continue;
					}

					TranslationResult? titleResult = await TranslationService.TranslateAsync(
						video.Title,
						video.DetectedLanguage,
						token
					);
					TranslationResult? descResult = await TranslationService.TranslateAsync(
						video.Description,
						video.DetectedLanguage,
						token
					);

					if (titleResult is not null)
					{
						result.Add(
							video.WithTranslation(
								titleResult.Translation,
								descResult?.Translation ?? video.Description,
								titleResult.DetectedLanguage
							)
						);
					}
					else
					{
						result.Add(video);
					}
				}

				return result;
			},
			ct: ct
		);
	}

	internal static void ShowTranslationPreview(List<YouTubeVideo> videos)
	{
		var translated = videos.Where(v => v.TranslatedTitle is not null).ToList();
		if (translated.Count == 0)
		{
			UI.Info("No translations available");
			return;
		}

		SpectreTable table = new SpectreTable()
			.Border(TableBorder.Rounded)
			.AddColumn("Original Title")
			.AddColumn("Translated Title")
			.AddColumn("Language");

		foreach (YouTubeVideo video in translated)
		{
			table.AddRow(
				Markup.Escape(video.Title),
				Markup.Escape(video.TranslatedTitle ?? "-"),
				Markup.Escape(video.DetectedLanguage ?? "-")
			);
		}

		AnsiConsole.Write(table);
		UI.Info("{0} of {1} videos translated", translated.Count, videos.Count);
	}
}
