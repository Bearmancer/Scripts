namespace CSharpScripts.Services.Music;

#region MusicExporter

public static class MusicExporter
{
    public static string ExportWorksToCSV(string releaseTitle, List<WorkSummary> works)
    {
        CreateDirectory(path: Paths.ExportsDirectory);

        string sanitizedTitle = SanitizeFileName(name: releaseTitle);
        string path = Combine(path1: Paths.ExportsDirectory, $"{sanitizedTitle}_works.csv");

        using StreamWriter writer = new(path: path, append: false);
        using CsvWriter csv = new(
            writer: writer,
            new CsvConfiguration(cultureInfo: CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
            }
        );

        csv.WriteField(field: "Disc");
        csv.WriteField(field: "TrackStart");
        csv.WriteField(field: "TrackEnd");
        csv.WriteField(field: "Work");
        csv.WriteField(field: "Composer");
        csv.WriteField(field: "Conductor");
        csv.WriteField(field: "Orchestra");
        csv.WriteField(field: "Year");
        csv.WriteField(field: "Movements");
        csv.NextRecord();

        foreach (var work in works)
        {
            csv.WriteField(field: work.Disc);
            csv.WriteField(field: work.FirstTrack);
            csv.WriteField(field: work.LastTrack);
            csv.WriteField(field: work.Work);
            csv.WriteField(field: work.Composer);
            csv.WriteField(field: work.Conductor);
            csv.WriteField(field: work.Orchestra);
            csv.WriteField(field: work.YearDisplay);
            csv.WriteField(work.LastTrack - work.FirstTrack + 1);
            csv.NextRecord();
        }

        Console.Info(message: "Exported {0} works to {1}", works.Count, GetFileName(path: path));
        return path;
    }

    public static string ExportToSheets(ReleaseData release)
    {
        GoogleSheetsService sheets = new();

        string spreadsheetId = sheets.CreateSpreadsheet(title: release.Info.Title);
        Console.Info(message: "Created Google Sheet: {0}", release.Info.Title);

        List<object> headers =
        [
            "Disc",
            "Track",
            "Title",
            "Work",
            "Composer",
            "Conductor",
            "Orchestra",
            "Year",
            "Duration",
            "Album",
            "Label",
        ];

        sheets.WriteRecords(
            spreadsheetId: spreadsheetId,
            sheetName: "Sheet1",
            headers: headers,
            records: release.Tracks,
            t =>
                [
                    t.DiscNumber,
                    t.TrackNumber,
                    t.Title,
                    t.WorkName ?? "",
                    t.Composer ?? "",
                    t.Conductor ?? "",
                    t.Orchestra ?? "",
                    release.Info.Year?.ToString() ?? "",
                    t.Duration is { } d && d > TimeSpan.Zero ? d.ToString(@"m\:ss") : "",
                    release.Info.Title,
                    release.Info.Label ?? "",
                ]
        );

        string url = GoogleSheetsService.GetSpreadsheetUrl(spreadsheetId: spreadsheetId);
        Console.Link(url: url, text: "View spreadsheet");

        sheets.Dispose();
        return url;
    }

    private static string SanitizeFileName(string name) =>
        GetInvalidFileNameChars()
            .Aggregate(seed: name, (current, c) => current.Replace(oldChar: c, newChar: '_'))
            .Trim()
            .TrimEnd(trimChar: '.')[..Math.Min(val1: name.Length, val2: 100)];
}

#endregion
