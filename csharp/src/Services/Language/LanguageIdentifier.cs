namespace CSharpScripts.Services.Language;

internal static class LanguageIdentifier
{
	private static Lazy<Lingua.LanguageDetector?> Detector { get; } =
		new(() =>
		{
			try
			{
				return Lingua.LanguageDetectorBuilder.FromAllLanguages().Build();
			}
			catch (Exception ex)
			{
				Log.Warning(
					messageTemplate: "Failed to initialize Lingua language detector: {Error}",
					ex.Message
				);
				return null;
			}
		});

	public static string? Detect(string text)
	{
		if (IsNullOrWhiteSpace(value: text) || text.Length < 15)
			return null;

		try
		{
			var detector = Detector.Value;
			if (detector is null)
				return null;

			var language = detector.DetectLanguageOf(text: text);
			return language?.IsoCode639_1;
		}
		catch (Exception ex)
		{
			Log.Warning(messageTemplate: "Language detection failed: {Error}", ex.Message);
			return null;
		}
	}

	public static bool IsEnglish(string text) =>
		Detect(text: text)?.EqualsIgnoreCase(other: "en") == true;

	public static bool RequiresTranslation(string text)
	{
		var lang = Detect(text: text);
		return lang is { } && !lang.EqualsIgnoreCase(other: "en");
	}
}
