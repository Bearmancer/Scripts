namespace CSharpScripts.Services.Language;

internal static class LanguageIdentifier
{
	private static Lazy<RankedLanguageIdentifier?> Detector { get; } =
		new(() =>
		{
			var exeDir = AppContext.BaseDirectory;
			var profilePath = Path.Combine(path1: exeDir, path2: "Core14.profile.xml");

			if (!File.Exists(path: profilePath))
			{
				Log.Warning(messageTemplate: "Language profile not found: {Path}", profilePath);
				return null;
			}

			return new RankedLanguageIdentifierFactory().Load(inputFilePath: profilePath);
		});

	public static string? Detect(string text)
	{
		if (IsNullOrWhiteSpace(value: text) || text.Length < 15)
			return null;

		Tuple<LanguageInfo, double>? result = Detector.Value?.Identify(text: text).FirstOrDefault();

		return result?.Item1.Iso639_3;
	}

	public static bool IsEnglish(string text) =>
		Detect(text: text)?.EqualsIgnoreCase(other: "eng") == true;

	public static bool RequiresTranslation(string text)
	{
		var lang = Detect(text: text);
		return lang is { } && !lang.EqualsIgnoreCase(other: "eng");
	}
}
