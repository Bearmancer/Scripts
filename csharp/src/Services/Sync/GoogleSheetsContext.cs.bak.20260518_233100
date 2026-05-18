namespace CSharpScripts.Services.Sync.Google;

internal sealed record GoogleSheetsContext : IDisposable
{
	private readonly Dictionary<string, Spreadsheet> SpreadsheetCache = [];

	private GoogleSheetsContext(DriveService driveService, SheetsService sheetsService)
	{
		DriveService = driveService;
		SheetsService = sheetsService;
	}

	internal DriveService DriveService { get; init; }
	internal SheetsService SheetsService { get; init; }

	public void Dispose()
	{
		SheetsService?.Dispose();
		DriveService?.Dispose();
		GC.SuppressFinalize(this);
	}

	public static async Task<GoogleSheetsContext> CreateAsync(CancellationToken ct = default)
	{
		BaseClientService.Initializer initializer = await GoogleAuth.GetInitializerAsync(ct);
		return new GoogleSheetsContext(
			new DriveService(initializer: initializer),
			new SheetsService(initializer: initializer)
		);
	}

	internal async Task<Spreadsheet> GetSpreadsheetMetadataAsync(
		string spreadsheetId,
		bool forceRefresh = false,
		CancellationToken ct = default
	)
	{
		if (
			!forceRefresh
			&& SpreadsheetCache.TryGetValue(key: spreadsheetId, out Spreadsheet? cached)
		)
			return cached;

		Spreadsheet spreadsheet = await Resilience.ExecuteAsync(
			operation: "Sheets.Get",
			async () =>
			{
				SpreadsheetsResource.GetRequest request = SheetsService.Spreadsheets.Get(
					spreadsheetId: spreadsheetId
				);
				request.Fields =
					"spreadsheetId,properties/title,sheets(properties(sheetId,title,index,gridProperties))";
				return await request.ExecuteAsync(ct);
			},
			ct
		);

		SpreadsheetCache[key: spreadsheetId] = spreadsheet;
		return spreadsheet;
	}

	internal void InvalidateCache(string spreadsheetId) =>
		SpreadsheetCache.Remove(key: spreadsheetId);

	internal async Task<Sheet?> FindSheetAsync(
		string spreadsheetId,
		string sheetName,
		bool forceRefresh = false,
		CancellationToken ct = default
	)
	{
		Spreadsheet spreadsheet = await GetSpreadsheetMetadataAsync(
			spreadsheetId: spreadsheetId,
			forceRefresh: forceRefresh,
			ct
		);
		return spreadsheet.Sheets?.FirstOrDefault(s =>
			s.Properties?.Title.EqualsIgnoreCase(sheetName, Ordinal) ?? false
		);
	}

	public static string GetSpreadsheetUrl(string spreadsheetId) =>
		$"https://docs.google.com/spreadsheets/d/{spreadsheetId}";
}
