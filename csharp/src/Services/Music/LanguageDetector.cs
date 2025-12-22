namespace CSharpScripts.Services.Music;

/// <summary>
/// Detects non-English text and non-Latin scripts in metadata fields.
/// Logs issues to state/csv/language-issues.csv for review.
/// </summary>
public static class LanguageDetector
{
    static readonly string CsvPath = Combine(Paths.StateDirectory, "csv", "language-issues.csv");
    static bool headerWritten;

    /// <summary>
    /// Checks Work, Composer, Conductor, Orchestra for non-Latin scripts (Cyrillic, CJK, etc.)
    /// and logs to CSV if found.
    /// </summary>
    public static void LogNonLatinScript(
        int disc,
        int track,
        string? work,
        string? composer,
        string? conductor,
        string? orchestra
    )
    {
        CheckField(disc, track, "Work", work);
        CheckField(disc, track, "Composer", composer);
        CheckField(disc, track, "Conductor", conductor);
        CheckField(disc, track, "Orchestra", orchestra);
    }

    static void CheckField(int disc, int track, string fieldName, string? value)
    {
        if (IsNullOrWhiteSpace(value))
            return;

        string? nonLatinChars = DetectNonLatinChars(value);
        if (nonLatinChars is not null)
        {
            LogIssue(disc, track, fieldName, value, $"Non-Latin: {nonLatinChars}");
        }
    }

    /// <summary>
    /// Returns a sample of non-Latin characters found, or null if all Latin/common.
    /// </summary>
    static string? DetectNonLatinChars(string text)
    {
        List<char> nonLatin = [];
        foreach (char c in text)
        {
            // Allow: Latin letters, digits, punctuation, spaces, diacritics
            if (
                char.IsAsciiLetter(c)
                || char.IsDigit(c)
                || char.IsPunctuation(c)
                || char.IsWhiteSpace(c)
                || IsLatinDiacritic(c)
            )
            {
                continue;
            }

            // Non-Latin character found
            nonLatin.Add(c);
        }

        return nonLatin.Count > 0 ? new string([.. nonLatin.Distinct().Take(10)]) : null;
    }

    /// <summary>
    /// Check if character is Latin with diacritics (e.g., á, ñ, ü, ø, ß)
    /// </summary>
    static bool IsLatinDiacritic(char c)
    {
        // Latin Extended-A: 0x0100-0x017F
        // Latin Extended-B: 0x0180-0x024F
        // Latin Extended Additional: 0x1E00-0x1EFF
        // Latin Supplement: 0x00C0-0x00FF (excludes control chars)
        return c
            is >= '\u00C0'
                and <= '\u00FF'
                or // Latin-1 Supplement (À-ÿ)
                >= '\u0100'
                and <= '\u017F'
                or // Latin Extended-A
                >= '\u0180'
                and <= '\u024F'
                or // Latin Extended-B
                >= '\u1E00'
                and <= '\u1EFF'; // Latin Extended Additional
    }

    static void LogIssue(int disc, int track, string field, string value, string issue)
    {
        string dir = GetDirectoryName(CsvPath)!;
        CreateDirectory(dir);

        if (!headerWritten && !File.Exists(CsvPath))
        {
            AppendAllText(CsvPath, "Disc,Track,Field,Value,Issue\n");
            headerWritten = true;
        }

        // Escape value for CSV
        string escapedValue =
            value.Contains(',') || value.Contains('"') || value.Contains('\n')
                ? $"\"{value.Replace("\"", "\"\"")}\""
                : value;

        string row = $"{disc},{track},{field},{escapedValue},{issue}\n";
        AppendAllText(CsvPath, row);
    }
}
