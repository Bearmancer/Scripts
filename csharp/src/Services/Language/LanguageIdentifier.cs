using Lingua;

namespace CSharpScripts.Services.Language;

internal static class LanguageIdentifier
{
	private static readonly LanguageDetector Detector = LanguageDetectorBuilder
		.FromAllLanguages()
		.WithPreloadedLanguageModels()
		.Build();

	public static string? Detect(string text)
	{
		if (IsNullOrWhiteSpace(value: text) || text.Length < 15)
			return null;

		try
		{
			var language = Detector.DetectLanguageOf(text);
			return GetLanguageCode(language);
		}
		catch (Exception ex)
		{
			Log.Warning(messageTemplate: "Language detection failed: {Error}", ex.Message);
			return null;
		}
	}

	public static bool IsEnglish(string text) =>
		Detect(text: text)?.EqualsIgnoreCase(other: "eng") == true;

	public static bool RequiresTranslation(string text)
	{
		var lang = Detect(text: text);
		return lang is { } && !lang.EqualsIgnoreCase(other: "eng");
	}

	private static string GetLanguageCode(Lingua.Language language) =>
		language switch
		{
			Lingua.Language.English => "eng",
			Lingua.Language.French => "fra",
			Lingua.Language.German => "deu",
			Lingua.Language.Spanish => "spa",
			Lingua.Language.Portuguese => "por",
			Lingua.Language.Italian => "ita",
			Lingua.Language.Dutch => "nld",
			Lingua.Language.Russian => "rus",
			Lingua.Language.Chinese => "zho",
			Lingua.Language.Japanese => "jpn",
			Lingua.Language.Korean => "kor",
			Lingua.Language.Arabic => "ara",
			Lingua.Language.Hindi => "hin",
			Lingua.Language.Bengali => "ben",
			Lingua.Language.Catalan => "cat",
			Lingua.Language.Czech => "ces",
			Lingua.Language.Danish => "dan",
			Lingua.Language.Finnish => "fin",
			Lingua.Language.Greek => "ell",
			Lingua.Language.Hungarian => "hun",
			Lingua.Language.Polish => "pol",
			Lingua.Language.Romanian => "ron",
			Lingua.Language.Slovak => "slk",
			Lingua.Language.Swedish => "swe",
			Lingua.Language.Turkish => "tur",
			Lingua.Language.Ukrainian => "ukr",
			Lingua.Language.Vietnamese => "vie",
			Lingua.Language.Thai => "tha",
			Lingua.Language.Hebrew => "heb",
			Lingua.Language.Afrikaans => "afr",
			Lingua.Language.Albanian => "sqi",
			Lingua.Language.Basque => "eus",
			Lingua.Language.Belarusian => "bel",
			Lingua.Language.Bosnian => "bos",
			Lingua.Language.Bulgarian => "bul",
			Lingua.Language.Croatian => "hrv",
			Lingua.Language.Estonian => "est",
			Lingua.Language.Georgian => "kat",
			Lingua.Language.Gujarati => "guj",
			Lingua.Language.Icelandic => "isl",
			Lingua.Language.Indonesian => "ind",
			Lingua.Language.Irish => "gle",
			Lingua.Language.Kazakh => "kaz",
			Lingua.Language.Latvian => "lav",
			Lingua.Language.Lithuanian => "lit",
			Lingua.Language.Macedonian => "mkd",
			Lingua.Language.Malay => "msa",
			Lingua.Language.Marathi => "mar",
			Lingua.Language.Persian => "fas",
			_ => language.ToString().ToLowerInvariant()[..3],
		};
}
