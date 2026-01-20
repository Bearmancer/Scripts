namespace CSharpScripts.Services.Music;

public static class MusicExporter
{
	public static string ExportWorksToCSV(string releaseTitle, List<WorkSummary> works)
	{
		CreateDirectory(Paths.ExportsDirectory);

		var sanitizedTitle = SanitizeFileName(releaseTitle);
		var path = Combine(Paths.ExportsDirectory, $"{sanitizedTitle}_works.csv");

		using StreamWriter writer = new(path, append: false);
		using CsvWriter csv = new(
			writer,
			new CsvConfiguration(CultureInfo.InvariantCulture)
			{
				HasHeaderRecord = true,
			}
		);

		csv.WriteField("Disc");
		csv.WriteField("TrackStart");
		csv.WriteField("TrackEnd");
		csv.WriteField("Work");
		csv.WriteField("Composer");
		csv.WriteField("Conductor");
		csv.WriteField("Orchestra");
		csv.WriteField("Year");
		csv.WriteField("Movements");
		csv.NextRecord();

		foreach (WorkSummary work in works)
		{
			csv.WriteField(work.Disc);
			csv.WriteField(work.FirstTrack);
			csv.WriteField(work.LastTrack);
			csv.WriteField(work.Work);
			csv.WriteField(work.Composer);
			csv.WriteField(work.Conductor);
			csv.WriteField(work.Orchestra);
			csv.WriteField(work.YearDisplay);
			csv.WriteField(work.LastTrack - work.FirstTrack + 1);
			csv.NextRecord();
		}

		Console.Info("Exported {0} works to {1}", works.Count, GetFileName(path));
		return path;
	}

	public static string ExportToSheets(ReleaseData release)
	{
		GoogleSheetsService sheets = new();

		var spreadsheetId = sheets.CreateSpreadsheet(release.Info.Title);
		Console.Info("Created Google Sheet: {0}", release.Info.Title);

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
			spreadsheetId,
			"Sheet1",
			headers,
			release.Tracks,
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

		var url = GoogleSheetsService.GetSpreadsheetUrl(spreadsheetId);
		Console.Link(url, "View spreadsheet");

		sheets.Dispose();
		return url;
	}

	private static string SanitizeFileName(string name) =>
		GetInvalidFileNameChars()
			.Aggregate(name, (current, c) => current.Replace(c, '_'))
			.Trim()
			.TrimEnd('.')[..Math.Min(name.Length, 100)];
}
