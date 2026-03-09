namespace CSharpScripts.Services.Music;

internal static class MusicExporter
{
	public static string ExportWorksToCSV(string releaseTitle, List<WorkSummary> works)
	{
		Directory.CreateDirectory(Paths.ExportsDirectory);

		var sanitizedTitle = SanitizeFileName(releaseTitle);
		var path = Path.Combine(Paths.ExportsDirectory, $"{sanitizedTitle}_works.csv");

		using StreamWriter writer = new(path, append: false);
		using CsvWriter csv = new(
			writer,
			new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true }
		);

		csv.WriteField("Disc");
		csv.WriteField("TrackStart");
		csv.WriteField("TrackEnd");
		csv.WriteField("Work");
		csv.WriteField("Composer");
		csv.WriteField("Orchestra");
		csv.WriteField("Conductor");
		csv.WriteField("Soloists");
		csv.WriteField("Venue");
		csv.WriteField("Year");
		csv.WriteField("Duration");
		csv.WriteField("Movements");
		csv.NextRecord();

		foreach (WorkSummary work in works)
		{
			csv.WriteField(work.Disc);
			csv.WriteField(work.FirstTrack);
			csv.WriteField(work.LastTrack);
			csv.WriteField(work.Work);
			csv.WriteField(work.Composer);
			csv.WriteField(work.Orchestra);
			csv.WriteField(work.Conductor);
			csv.WriteField(Join("; ", work.Soloists));
			csv.WriteField(Join("; ", work.RecordingVenues));
			csv.WriteField(work.YearDisplay);
			csv.WriteField(
				work.TotalDuration > TimeSpan.Zero ? work.TotalDuration.ToString(@"hh\:mm\:ss") : ""
			);
			csv.WriteField(work.LastTrack - work.FirstTrack + 1);
			csv.NextRecord();
		}

		Log.Information("Exported {0} works to {1}", works.Count, Path.GetFileName(path));
		return path;
	}

	public static async Task<string> ExportToSheetsAsync(
		ReleaseData release,
		CancellationToken ct = default
	)
	{
		GoogleSheetsService sheets = await GoogleSheetsService.CreateAsync(ct);

		var spreadsheetId = await sheets.CreateSpreadsheetAsync(release.Info.Title, ct);
		Log.Information("Created Google Sheet: {0}", release.Info.Title);

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

		await sheets.WriteRecordsAsync(
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
				],
			ct
		);

		var url = GoogleSheetsService.GetSpreadsheetUrl(spreadsheetId);
		Log.Information("MusicSheetUrl {Url}", url);

		sheets.Dispose();
		return url;
	}

	private static string SanitizeFileName(string name) =>
		Path.GetInvalidFileNameChars()
			.Aggregate(name, (current, c) => current.Replace(c, '_'))
			.Trim()
			.TrimEnd('.')[..Math.Min(name.Length, 100)];
}
