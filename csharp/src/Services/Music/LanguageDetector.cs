namespace Scripts.Services.Music;

public static class LanguageDetector
{
	private static readonly string CsvPath = Combine(
		Paths.StateDirectory,
		"csv",
		"language-issues.csv"
	);

	private static bool headerWritten;

	public static void LogNonLatinScript(
		int disc,
		int track,
		string? work,
		string? composer,
		string? conductor,
		string? orchestra
	)
	{
		CheckField(disc, track, fieldName: "Work", value: work);
		CheckField(disc, track, fieldName: "Composer", value: composer);
		CheckField(disc, track, fieldName: "Conductor", value: conductor);
		CheckField(disc, track, fieldName: "Orchestra", value: orchestra);
	}

	private static void CheckField(int disc, int track, string fieldName, string? value)
	{
		if (IsNullOrWhiteSpace(value))
			return;

		var nonLatinChars = DetectNonLatinChars(value);
		if (nonLatinChars is { })
			LogIssue(disc, track, field: fieldName, value, $"Non-Latin: {nonLatinChars}");
	}

	private static string? DetectNonLatinChars(string text)
	{
		List<char> nonLatin = [];
		foreach (var c in text)
		{
			if (
				char.IsAsciiLetter(c)
				|| char.IsDigit(c)
				|| char.IsPunctuation(c)
				|| char.IsWhiteSpace(c)
				|| IsLatinDiacritic(c)
			)
				continue;

			nonLatin.Add(c);
		}

		return nonLatin.Count > 0 ? new string([.. nonLatin.Distinct().Take(10)]) : null;
	}

	private static bool IsLatinDiacritic(char c)
	{
		return c
			is (>= '\u00C0' and <= '\u00FF')
				or (>= '\u0100' and <= '\u017F')
				or (>= '\u0180' and <= '\u024F')
				or (>= '\u1E00' and <= '\u1EFF');
	}

	private static void LogIssue(int disc, int track, string field, string value, string issue)
	{
		var dir = GetDirectoryName(CsvPath)!;
		CreateDirectory(dir);

		if (!headerWritten && !File.Exists(CsvPath))
		{
			AppendAllText(CsvPath, "Disc,Track,Field,Value,Issue\n");
			headerWritten = true;
		}

		var escapedValue =
			value.Contains(',') || value.Contains('"') || value.Contains('\n')
				? $"\"{value.Replace("\"", "\"\"")}\""
				: value;

		var row = $"{disc},{track},{field},{escapedValue},{issue}\n";
		AppendAllText(CsvPath, row);
	}
}
