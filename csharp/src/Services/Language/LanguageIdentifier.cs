using NTextCat;

namespace CSharpScripts.Services.Language;

internal static class LanguageIdentifier
{
	private static Lazy<RankedLanguageIdentifier?> Detector { get; } =
		new(() =>
		{
			var exeDir = AppContext.BaseDirectory;
			var profilePath = Path.Combine(exeDir, "Core14.profile.xml");

			if (!File.Exists(profilePath))
			{
				Log.Warning("Language profile not found: {Path}", profilePath);
				return null;
			}

			return new RankedLanguageIdentifierFactory().Load(profilePath);
		});

	public static string? Detect(string text)
	{
		if (IsNullOrWhiteSpace(text) || text.Length < 15)
			return null; // Too short for reliable detection

		if (Detector.Value is null)
			return null; // Profile not loaded

		Tuple<LanguageInfo, double>? result = Detector.Value.Identify(text).FirstOrDefault();

		return result?.Item1.Iso639_3;
	}

	public static bool IsEnglish(string text) => Detect(text)?.EqualsIgnoreCase("eng") == true;

	public static bool RequiresTranslation(string text)
	{
		var lang = Detect(text);
		return lang is not null && !lang.EqualsIgnoreCase("eng");
	}
}
