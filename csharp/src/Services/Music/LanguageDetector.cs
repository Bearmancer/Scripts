namespace CSharpScripts.Services.Music;

#region LanguageDetector

public static class LanguageDetector
{
    private static readonly string CsvPath = Combine(
        path1: Paths.StateDirectory,
        path2: "csv",
        path3: "language-issues.csv"
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
        CheckField(disc: disc, track: track, fieldName: "Work", value: work);
        CheckField(disc: disc, track: track, fieldName: "Composer", value: composer);
        CheckField(disc: disc, track: track, fieldName: "Conductor", value: conductor);
        CheckField(disc: disc, track: track, fieldName: "Orchestra", value: orchestra);
    }

    private static void CheckField(int disc, int track, string fieldName, string? value)
    {
        if (IsNullOrWhiteSpace(value: value))
            return;

        string? nonLatinChars = DetectNonLatinChars(text: value);
        if (nonLatinChars is { })
            LogIssue(
                disc: disc,
                track: track,
                field: fieldName,
                value: value,
                $"Non-Latin: {nonLatinChars}"
            );
    }

    private static string? DetectNonLatinChars(string text)
    {
        List<char> nonLatin = [];
        foreach (char c in text)
        {
            if (
                char.IsAsciiLetter(c: c)
                || char.IsDigit(c: c)
                || char.IsPunctuation(c: c)
                || char.IsWhiteSpace(c: c)
                || IsLatinDiacritic(c: c)
            )
                continue;

            nonLatin.Add(item: c);
        }

        return nonLatin.Count > 0 ? new string([.. nonLatin.Distinct().Take(count: 10)]) : null;
    }

    private static bool IsLatinDiacritic(char c)
    {
        return c
            is >= '\u00C0'
                and <= '\u00FF'
                or >= '\u0100'
                and <= '\u017F'
                or >= '\u0180'
                and <= '\u024F'
                or >= '\u1E00'
                and <= '\u1EFF';
    }

    private static void LogIssue(int disc, int track, string field, string value, string issue)
    {
        string dir = GetDirectoryName(path: CsvPath)!;
        CreateDirectory(path: dir);

        if (!headerWritten && !File.Exists(path: CsvPath))
        {
            AppendAllText(path: CsvPath, contents: "Disc,Track,Field,Value,Issue\n");
            headerWritten = true;
        }

        string escapedValue =
            value.Contains(value: ',') || value.Contains(value: '"') || value.Contains(value: '\n')
                ? $"\"{value.Replace(oldValue: "\"", newValue: "\"\"")}\""
                : value;

        var row = $"{disc},{track},{field},{escapedValue},{issue}\n";
        AppendAllText(path: CsvPath, contents: row);
    }
}

#endregion
