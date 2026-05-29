namespace CSharpScripts.Services.Language;

internal static partial class TranslationNormalizer
{
	private static readonly FrozenDictionary<string, string> ComposerNameVariants = new Dictionary<
		string,
		string
	>(comparer: StringComparer.OrdinalIgnoreCase)
	{
		[key: "Tschaikowsky"] = "Tchaikovsky",
		[key: "Tschaikowski"] = "Tchaikovsky",
		[key: "Tschaikovsky"] = "Tchaikovsky",
		[key: "Strawinsky"] = "Stravinsky",
		[key: "Strawinski"] = "Stravinsky",
		[key: "Prokofjew"] = "Prokofiev",
		[key: "Prokofiew"] = "Prokofiev",
		[key: "Schostakowitsch"] = "Shostakovich",
		[key: "Schostakovitch"] = "Shostakovich",
		[key: "Mussorgski"] = "Mussorgsky",
		[key: "Mussorgskij"] = "Mussorgsky",
		[key: "Rimski-Korsakow"] = "Rimsky-Korsakov",
		[key: "Rimskij-Korsakow"] = "Rimsky-Korsakov",
		[key: "Rimsky-Korsakow"] = "Rimsky-Korsakov",
		[key: "Rachmaninow"] = "Rachmaninoff",
		[key: "Rachmaninov"] = "Rachmaninoff",
		[key: "Skrjabin"] = "Scriabin",
		[key: "Skriabin"] = "Scriabin",
		[key: "Händel"] = "Handel",
		[key: "Haendel"] = "Handel",
		[key: "Weinberg"] = "Weinberg",

		[key: "Tchaïkovski"] = "Tchaikovsky",
		[key: "Tchaïkovsky"] = "Tchaikovsky",
		[key: "Moussorgski"] = "Mussorgsky",
		[key: "Moussorgsky"] = "Mussorgsky",
		[key: "Rimski-Korsakov"] = "Rimsky-Korsakov",
		[key: "Chostakovitch"] = "Shostakovich",

		[key: "Ciaikovski"] = "Tchaikovsky",
		[key: "Ciaikovskij"] = "Tchaikovsky",

		[key: "Csajkovszkij"] = "Tchaikovsky",
		[key: "Čajkovskij"] = "Tchaikovsky",

		[key: "Dvorak"] = "Dvořák",
		[key: "Smetana"] = "Smetana",
		[key: "Janacek"] = "Janáček",
		[key: "Bartok"] = "Bartók",
		[key: "Kodaly"] = "Kodály",

		[key: "Vineyard"] = "Weinberg",
		[key: "Dull"] = "Dutilleux",
	}.ToFrozenDictionary(comparer: StringComparer.OrdinalIgnoreCase);

	private static readonly FrozenDictionary<string, string> MistranslationCorrections =
		new Dictionary<string, string>(comparer: StringComparer.OrdinalIgnoreCase)
		{
			[key: "Vineyard"] = "Weinberg",
			[key: "Wine mountain"] = "Weinberg",
			[key: "Dull"] = "Dutilleux",
			[key: "The Moldova"] = "The Moldau",
			[key: "Stringserenade"] = "Serenade for Strings",
			[key: "order recording"] = "orchestral version",
			[key: "hr Symphony Orchestra"] = "Frankfurt Radio Symphony",
			[key: "hr symphony orchestra"] = "Frankfurt Radio Symphony",
		}.ToFrozenDictionary(comparer: StringComparer.OrdinalIgnoreCase);

	private static readonly FrozenDictionary<string, string> MusicalTerms = new Dictionary<
		string,
		string
	>(comparer: StringComparer.OrdinalIgnoreCase)
	{
		[key: "Klavierkonzert"] = "Piano Concerto",
		[key: "Violinkonzert"] = "Violin Concerto",
		[key: "Cellokonzert"] = "Cello Concerto",
		[key: "Konzert für Klavier"] = "Piano Concerto",
		[key: "Konzert für Violine"] = "Violin Concerto",
		[key: "Konzert für Violoncello"] = "Cello Concerto",
		[key: "Konzert für Orchester"] = "Concerto for Orchestra",
		[key: "Konzert für Streichorchester"] = "Concerto for String Orchestra",
		[key: "Sinfonie"] = "Symphony",
		[key: "Sinfonieorchester"] = "Symphony Orchestra",
		[key: "hr-Sinfonieorchester"] = "Frankfurt Radio Symphony",
		[key: "Symphonie"] = "Symphony",
		[key: "Ouvertüre"] = "Overture",
		[key: "Ouverture"] = "Overture",
		[key: "Streichquartett"] = "String Quartet",
		[key: "Streicherserenade"] = "Serenade for Strings",
		[key: "Kammermusik"] = "Chamber Music",
		[key: "Kammerorchester"] = "Chamber Orchestra",
		[key: "Kammersymphonie"] = "Chamber Symphony",
		[key: "Kammersinfonie"] = "Chamber Symphony",
		[key: "Sinfonische Dichtung"] = "Symphonic Poem",
		[key: "Sinfonische Metamorphosen"] = "Symphonic Metamorphoses",
		[key: "Filmmusik"] = "Film Music",
		[key: "Schlagzeug"] = "Percussion",
		[key: "Marimbafon"] = "Marimba",
		[key: "Dirigent"] = "Conductor",
		[key: "Dirigentin"] = "Conductor",
		[key: "Klavier"] = "Piano",
		[key: "Violine"] = "Violin",
		[key: "Vorspiel"] = "Prelude",
		[key: "Liebestod"] = "Love Death",
		[key: "Feuervogel"] = "Firebird",
		[key: "Frühlingsweihe"] = "Rite of Spring",
		[key: "Sommernachtstraum"] = "Midsummer Night's Dream",
		[key: "Heldenleben"] = "A Hero's Life",
		[key: "Tod und Verklärung"] = "Death and Transfiguration",
		[key: "Also sprach Zarathustra"] = "Thus Spoke Zarathustra",
		[key: "Ein Heldenleben"] = "A Hero's Life",
		[key: "Till Eulenspiegels"] = "Till Eulenspiegel's",
		[key: "Rosenkavalier"] = "Der Rosenkavalier",
		[key: "Meistersinger"] = "Die Meistersinger",
		[key: "Walküre"] = "Die Walküre",
		[key: "Götterdämmerung"] = "Götterdämmerung",
		[key: "Rheingold"] = "Das Rheingold",
		[key: "Ungarische Fantasie"] = "Hungarian Fantasy",
		[key: "Totentanz"] = "Dance of Death",
		[key: "Manfred-Sinfonie"] = "Manfred Symphony",
		[key: "Totenfeier"] = "Funeral Rite",
		[key: "Bilder einer Ausstellung"] = "Pictures at an Exhibition",
		[key: "Die Moldau"] = "The Moldau",
		[key: "Das goldene Spinnrad"] = "The Golden Spinning Wheel",

		[key: "Concerto pour piano"] = "Piano Concerto",
		[key: "Concerto pour violon"] = "Violin Concerto",

		[key: "Concerto per pianoforte"] = "Piano Concerto",
		[key: "Concerto per violino"] = "Violin Concerto",
		[key: "Sinfonia"] = "Symphony",
	}.ToFrozenDictionary(comparer: StringComparer.OrdinalIgnoreCase);

	private static readonly (
		Regex Regex,
		string Original,
		string Replacement
	)[] MistranslationRegexes = BuildRegexPatterns(source: MistranslationCorrections);

	private static readonly (
		Regex Regex,
		string Original,
		string Replacement
	)[] ComposerNameRegexes = BuildRegexPatterns(source: ComposerNameVariants);

	private static readonly (
		Regex Regex,
		string Original,
		string Replacement
	)[] MusicalTermRegexes = BuildRegexPatterns(source: MusicalTerms);

	[GeneratedRegex(
		pattern: @"\b(\d+)(?:st|nd|rd|th)\s+(Symphony|Concerto|Sonata|Quartet|Quintet|Trio|Suite)",
		options: RegexOptions.IgnoreCase
	)]
	private static partial Regex OrdinalBeforeWorkRegex();

	[GeneratedRegex(
		pattern: @"\b(Symphony|Concerto|Sonata|Quartet|Quintet|Trio|Suite)\s+(?:No\.?\s*)?(\d+)(?:st|nd|rd|th)?",
		options: RegexOptions.IgnoreCase
	)]
	private static partial Regex WorkWithNumberRegex();

	[GeneratedRegex(
		pattern: @"\b(\d+)\.\s*(Klavierkonzert|Violinkonzert|Cellokonzert|Sinfonie|Symphonie|Streichquartett)",
		options: RegexOptions.IgnoreCase
	)]
	private static partial Regex GermanOrdinalRegex();

	[GeneratedRegex(pattern: @"Nr\.\s*(\d+)", options: RegexOptions.IgnoreCase)]
	private static partial Regex GermanNrRegex();

	[GeneratedRegex(pattern: @"n[°º]\s*(\d+)", options: RegexOptions.IgnoreCase)]
	private static partial Regex FrenchNoRegex();

	[GeneratedRegex(pattern: @"\bop\.\s*(\d+)", options: RegexOptions.IgnoreCase)]
	private static partial Regex OpusRegex();

	public static string Normalize(string text)
	{
		if (IsNullOrWhiteSpace(value: text))
			return text;

		var result = text;

		result = FixMistranslations(text: result);
		result = NormalizeComposerNames(text: result);
		result = NormalizeMusicalTerms(text: result);
		result = NormalizeOrdinals(text: result);
		result = NormalizeOpusNumbers(text: result);
		result = CleanupWhitespace(text: result);

		return result;
	}

	public static string PreProcess(string text)
	{
		if (IsNullOrWhiteSpace(value: text))
			return text;

		return text;
	}

	private static string FixMistranslations(string text)
	{
		foreach ((Regex regex, _, var replacement) in MistranslationRegexes)
			text = regex.Replace(input: text, replacement: replacement);
		return text;
	}

	private static string NormalizeComposerNames(string text)
	{
		foreach ((Regex regex, _, var replacement) in ComposerNameRegexes)
			text = regex.Replace(input: text, replacement: replacement);
		return text;
	}

	private static string NormalizeMusicalTerms(string text)
	{
		foreach ((Regex regex, _, var replacement) in MusicalTermRegexes)
			text = regex.Replace(input: text, replacement: replacement);
		return text;
	}

	private static string NormalizeOrdinals(string text)
	{
		text = GermanOrdinalRegex()
			.Replace(
				input: text,
				match =>
				{
					var number = match.Groups[groupnum: 1].Value;
					var term = match.Groups[groupnum: 2].Value;
					var englishTerm = MusicalTerms.GetValueOrDefault(key: term, defaultValue: term);
					return $"{englishTerm} No. {number}";
				}
			);

		text = OrdinalBeforeWorkRegex()
			.Replace(
				input: text,
				match =>
				{
					var number = match.Groups[groupnum: 1].Value;
					var work = match.Groups[groupnum: 2].Value;
					return $"{work} No. {number}";
				}
			);

		text = WorkWithNumberRegex()
			.Replace(
				input: text,
				match =>
				{
					var work = match.Groups[groupnum: 1].Value;
					var number = match.Groups[groupnum: 2].Value;
					return $"{work} No. {number}";
				}
			);

		text = GermanNrRegex().Replace(input: text, replacement: "No. $1");
		text = FrenchNoRegex().Replace(input: text, replacement: "No. $1");

		return text;
	}

	private static string NormalizeOpusNumbers(string text)
	{
		text = OpusRegex().Replace(input: text, replacement: "Op. $1");
		return text;
	}

	private static string CleanupWhitespace(string text)
	{
		text = Regex.Replace(input: text, pattern: @"\s{2,}", replacement: " ");
		text = Regex.Replace(input: text, pattern: @"\s+([,.:;!?])", replacement: "$1");
		return text.Trim();
	}

	public static List<string> AnalyzePatterns(string text)
	{
		List<string> patterns = [];

		foreach (Match match in GermanOrdinalRegex().Matches(input: text))
			patterns.Add($"German ordinal: {match.Value}");

		foreach (Match match in OrdinalBeforeWorkRegex().Matches(input: text))
			patterns.Add($"Ordinal before work: {match.Value}");

		foreach (Match match in WorkWithNumberRegex().Matches(input: text))
			patterns.Add($"Work with number: {match.Value}");

		foreach (Match match in GermanNrRegex().Matches(input: text))
			patterns.Add($"German Nr.: {match.Value}");

		foreach (Match match in OpusRegex().Matches(input: text))
			patterns.Add($"Opus: {match.Value}");

		foreach ((Regex regex, var original, var replacement) in ComposerNameRegexes)
		{
			if (regex.IsMatch(input: text))
				patterns.Add($"Composer variant: {original} → {replacement}");
		}

		return patterns;
	}

	private static (Regex, string, string)[] BuildRegexPatterns(
		FrozenDictionary<string, string> source
	)
	{
		(Regex, string, string)[] result = new (Regex, string, string)[source.Count];
		var i = 0;
		foreach (var (key, value) in source)
		{
			result[i++] = (
				new Regex(
					$@"\b{Regex.Escape(str: key)}\b",
					RegexOptions.Compiled | RegexOptions.IgnoreCase
				),
				key,
				value
			);
		}
		return result;
	}
}
