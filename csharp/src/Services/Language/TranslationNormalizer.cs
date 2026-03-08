namespace CSharpScripts.Services.Language;

using System.Text.RegularExpressions;

internal static partial class TranslationNormalizer
{
	private static readonly Dictionary<string, string> ComposerNameVariants = new(
		StringComparer.OrdinalIgnoreCase
	)
	{
		["Tschaikowsky"] = "Tchaikovsky",
		["Tschaikowski"] = "Tchaikovsky",
		["Tschaikovsky"] = "Tchaikovsky",
		["Strawinsky"] = "Stravinsky",
		["Strawinski"] = "Stravinsky",
		["Prokofjew"] = "Prokofiev",
		["Prokofiew"] = "Prokofiev",
		["Schostakowitsch"] = "Shostakovich",
		["Schostakovitch"] = "Shostakovich",
		["Mussorgski"] = "Mussorgsky",
		["Mussorgskij"] = "Mussorgsky",
		["Rimski-Korsakow"] = "Rimsky-Korsakov",
		["Rimskij-Korsakow"] = "Rimsky-Korsakov",
		["Rimsky-Korsakow"] = "Rimsky-Korsakov",
		["Rachmaninow"] = "Rachmaninoff",
		["Rachmaninov"] = "Rachmaninoff",
		["Skrjabin"] = "Scriabin",
		["Skriabin"] = "Scriabin",
		["Händel"] = "Handel",
		["Haendel"] = "Handel",
		["Weinberg"] = "Weinberg",

		["Tchaïkovski"] = "Tchaikovsky",
		["Tchaïkovsky"] = "Tchaikovsky",
		["Moussorgski"] = "Mussorgsky",
		["Moussorgsky"] = "Mussorgsky",
		["Rimski-Korsakov"] = "Rimsky-Korsakov",
		["Chostakovitch"] = "Shostakovich",

		["Ciaikovski"] = "Tchaikovsky",
		["Ciaikovskij"] = "Tchaikovsky",

		["Csajkovszkij"] = "Tchaikovsky",
		["Čajkovskij"] = "Tchaikovsky",

		["Dvorak"] = "Dvořák",
		["Smetana"] = "Smetana",
		["Janacek"] = "Janáček",
		["Bartok"] = "Bartók",
		["Kodaly"] = "Kodály",

		["Vineyard"] = "Weinberg",
		["Dull"] = "Dutilleux",
	};

	private static readonly Dictionary<string, string> MistranslationCorrections = new(
		StringComparer.OrdinalIgnoreCase
	)
	{
		["Vineyard"] = "Weinberg",
		["Wine mountain"] = "Weinberg",
		["Dull"] = "Dutilleux",
		["The Moldova"] = "The Moldau",
		["Stringserenade"] = "Serenade for Strings",
		["order recording"] = "orchestral version",
		["hr Symphony Orchestra"] = "Frankfurt Radio Symphony", // Standardize orchestra name
		["hr symphony orchestra"] = "Frankfurt Radio Symphony",
	};

	private static readonly Dictionary<string, string> MusicalTerms = new(
		StringComparer.OrdinalIgnoreCase
	)
	{
		["Klavierkonzert"] = "Piano Concerto",
		["Violinkonzert"] = "Violin Concerto",
		["Cellokonzert"] = "Cello Concerto",
		["Konzert für Klavier"] = "Piano Concerto",
		["Konzert für Violine"] = "Violin Concerto",
		["Konzert für Violoncello"] = "Cello Concerto",
		["Konzert für Orchester"] = "Concerto for Orchestra",
		["Konzert für Streichorchester"] = "Concerto for String Orchestra",
		["Sinfonie"] = "Symphony",
		["Sinfonieorchester"] = "Symphony Orchestra",
		["hr-Sinfonieorchester"] = "Frankfurt Radio Symphony",
		["Symphonie"] = "Symphony",
		["Ouvertüre"] = "Overture",
		["Ouverture"] = "Overture",
		["Streichquartett"] = "String Quartet",
		["Streicherserenade"] = "Serenade for Strings",
		["Kammermusik"] = "Chamber Music",
		["Kammerorchester"] = "Chamber Orchestra",
		["Kammersymphonie"] = "Chamber Symphony",
		["Kammersinfonie"] = "Chamber Symphony",
		["Sinfonische Dichtung"] = "Symphonic Poem",
		["Sinfonische Metamorphosen"] = "Symphonic Metamorphoses",
		["Filmmusik"] = "Film Music",
		["Schlagzeug"] = "Percussion",
		["Marimbafon"] = "Marimba",
		["Dirigent"] = "Conductor",
		["Dirigentin"] = "Conductor",
		["Klavier"] = "Piano",
		["Violine"] = "Violin",
		["Vorspiel"] = "Prelude",
		["Liebestod"] = "Love Death",
		["Feuervogel"] = "Firebird",
		["Frühlingsweihe"] = "Rite of Spring",
		["Sommernachtstraum"] = "Midsummer Night's Dream",
		["Heldenleben"] = "A Hero's Life",
		["Tod und Verklärung"] = "Death and Transfiguration",
		["Also sprach Zarathustra"] = "Thus Spoke Zarathustra",
		["Ein Heldenleben"] = "A Hero's Life",
		["Till Eulenspiegels"] = "Till Eulenspiegel's",
		["Rosenkavalier"] = "Der Rosenkavalier",
		["Meistersinger"] = "Die Meistersinger",
		["Walküre"] = "Die Walküre",
		["Götterdämmerung"] = "Götterdämmerung",
		["Rheingold"] = "Das Rheingold",
		["Ungarische Fantasie"] = "Hungarian Fantasy",
		["Totentanz"] = "Dance of Death",
		["Manfred-Sinfonie"] = "Manfred Symphony",
		["Totenfeier"] = "Funeral Rite",
		["Bilder einer Ausstellung"] = "Pictures at an Exhibition",
		["Die Moldau"] = "The Moldau",
		["Das goldene Spinnrad"] = "The Golden Spinning Wheel",

		["Concerto pour piano"] = "Piano Concerto",
		["Concerto pour violon"] = "Violin Concerto",

		["Concerto per pianoforte"] = "Piano Concerto",
		["Concerto per violino"] = "Violin Concerto",
		["Sinfonia"] = "Symphony",
	};

	[GeneratedRegex(
		@"\b(\d+)(?:st|nd|rd|th)\s+(Symphony|Concerto|Sonata|Quartet|Quintet|Trio|Suite)",
		RegexOptions.IgnoreCase
	)]
	private static partial Regex OrdinalBeforeWorkRegex();

	[GeneratedRegex(
		@"\b(Symphony|Concerto|Sonata|Quartet|Quintet|Trio|Suite)\s+(?:No\.?\s*)?(\d+)(?:st|nd|rd|th)?",
		RegexOptions.IgnoreCase
	)]
	private static partial Regex WorkWithNumberRegex();

	[GeneratedRegex(
		@"\b(\d+)\.\s*(Klavierkonzert|Violinkonzert|Cellokonzert|Sinfonie|Symphonie|Streichquartett)",
		RegexOptions.IgnoreCase
	)]
	private static partial Regex GermanOrdinalRegex();

	[GeneratedRegex(@"Nr\.\s*(\d+)", RegexOptions.IgnoreCase)]
	private static partial Regex GermanNrRegex();

	[GeneratedRegex(@"n[°º]\s*(\d+)", RegexOptions.IgnoreCase)]
	private static partial Regex FrenchNoRegex();

	[GeneratedRegex(@"\bop\.\s*(\d+)", RegexOptions.IgnoreCase)]
	private static partial Regex OpusRegex();

	public static string Normalize(string text)
	{
		if (IsNullOrWhiteSpace(text))
			return text;

		var result = text;

		result = FixMistranslations(result);
		result = NormalizeComposerNames(result);
		result = NormalizeMusicalTerms(result);
		result = NormalizeOrdinals(result);
		result = NormalizeOpusNumbers(result);
		result = CleanupWhitespace(result);

		return result;
	}

	public static string PreProcess(string text)
	{
		if (IsNullOrWhiteSpace(text))
			return text;

		return text;
	}

	private static string FixMistranslations(string text)
	{
		foreach ((var wrong, var correct) in MistranslationCorrections)
		{
			text = Regex.Replace(
				text,
				$@"\b{Regex.Escape(wrong)}\b",
				correct,
				RegexOptions.IgnoreCase
			);
		}
		return text;
	}

	private static string NormalizeComposerNames(string text)
	{
		foreach ((var variant, var standard) in ComposerNameVariants)
		{
			text = Regex.Replace(
				text,
				$@"\b{Regex.Escape(variant)}\b",
				standard,
				RegexOptions.IgnoreCase
			);
		}
		return text;
	}

	private static string NormalizeMusicalTerms(string text)
	{
		foreach ((var foreign, var english) in MusicalTerms)
		{
			text = Regex.Replace(
				text,
				$@"\b{Regex.Escape(foreign)}\b",
				english,
				RegexOptions.IgnoreCase
			);
		}
		return text;
	}

	private static string NormalizeOrdinals(string text)
	{
		text = GermanOrdinalRegex()
			.Replace(
				text,
				match =>
				{
					var number = match.Groups[1].Value;
					var term = match.Groups[2].Value;
					var englishTerm = MusicalTerms.TryGetValue(term, out var t) ? t : term;
					return $"{englishTerm} No. {number}";
				}
			);

		text = OrdinalBeforeWorkRegex()
			.Replace(
				text,
				match =>
				{
					var number = match.Groups[1].Value;
					var work = match.Groups[2].Value;
					return $"{work} No. {number}";
				}
			);

		text = WorkWithNumberRegex()
			.Replace(
				text,
				match =>
				{
					var work = match.Groups[1].Value;
					var number = match.Groups[2].Value;
					return $"{work} No. {number}";
				}
			);

		text = GermanNrRegex().Replace(text, "No. $1");
		text = FrenchNoRegex().Replace(text, "No. $1");

		return text;
	}

	private static string NormalizeOpusNumbers(string text)
	{
		text = OpusRegex().Replace(text, "Op. $1");
		return text;
	}

	private static string CleanupWhitespace(string text)
	{
		text = Regex.Replace(text, @"\s{2,}", " ");
		text = Regex.Replace(text, @"\s+([,.:;!?])", "$1");
		return text.Trim();
	}

	public static List<string> AnalyzePatterns(string text)
	{
		List<string> patterns = [];

		foreach (Match match in GermanOrdinalRegex().Matches(text))
			patterns.Add($"German ordinal: {match.Value}");

		foreach (Match match in OrdinalBeforeWorkRegex().Matches(text))
			patterns.Add($"Ordinal before work: {match.Value}");

		foreach (Match match in WorkWithNumberRegex().Matches(text))
			patterns.Add($"Work with number: {match.Value}");

		foreach (Match match in GermanNrRegex().Matches(text))
			patterns.Add($"German Nr.: {match.Value}");

		foreach (Match match in OpusRegex().Matches(text))
			patterns.Add($"Opus: {match.Value}");

		foreach ((var variant, var standard) in ComposerNameVariants)
		{
			if (Regex.IsMatch(text, $@"\b{Regex.Escape(variant)}\b", RegexOptions.IgnoreCase))
				patterns.Add($"Composer variant: {variant} → {standard}");
		}

		return patterns;
	}
}
