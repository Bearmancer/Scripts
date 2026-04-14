namespace CSharpScripts.Services.Music;

internal static class MusicExporter
{
	public static string ExportWorksToCSV(string releaseTitle, List<WorkSummary> works)
	{
		Directory.CreateDirectory(path: Paths.ExportsDirectory);

		var sanitizedTitle = releaseTitle.SanitizeFileName(maxLength: 100);
		var path = Path.Combine(path1: Paths.ExportsDirectory, $"{sanitizedTitle}_works.csv");

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
		csv.WriteField(field: "Orchestra");
		csv.WriteField(field: "Conductor");
		csv.WriteField(field: "Soloists");
		csv.WriteField(field: "Venue");
		csv.WriteField(field: "Year");
		csv.WriteField(field: "Duration");
		csv.WriteField(field: "Movements");
		csv.NextRecord();

		foreach (WorkSummary work in works)
		{
			csv.WriteField(field: work.Disc);
			csv.WriteField(field: work.FirstTrack);
			csv.WriteField(field: work.LastTrack);
			csv.WriteField(field: work.Work);
			csv.WriteField(field: work.Composer);
			csv.WriteField(field: work.Orchestra);
			csv.WriteField(field: work.Conductor);
			csv.WriteField(Join(separator: "; ", values: work.Soloists));
			csv.WriteField(Join(separator: "; ", values: work.RecordingVenues));
			csv.WriteField(field: work.YearDisplay);
			csv.WriteField(
				work.TotalDuration > TimeSpan.Zero
					? work.TotalDuration.ToString(format: @"hh\:mm\:ss")
					: ""
			);
			csv.WriteField(work.LastTrack - work.FirstTrack + 1);
			csv.NextRecord();
		}

		Log.Information("Exported {0} works to {1}", works.Count, Path.GetFileName(path: path));
		return path;
	}

	public static async Task<string> ExportToSheetsAsync(
		ReleaseData release,
		CancellationToken ct = default
	)
	{
		GoogleSheetsService sheets = await GoogleSheetsService.CreateAsync(ct);

		var spreadsheetId = await sheets.CreateSpreadsheetAsync(title: release.Info.Title, ct);
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
					t.Duration is { } d && d > TimeSpan.Zero ? d.ToString(format: @"m\:ss") : "",
					release.Info.Title,
					release.Info.Label ?? "",
				],
			ct
		);

		var url = GoogleSheetsService.GetSpreadsheetUrl(spreadsheetId: spreadsheetId);
		Log.Information("MusicSheetUrl {Url}", url);

		sheets.Dispose();
		return url;
	}
}
