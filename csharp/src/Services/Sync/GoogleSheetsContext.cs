namespace CSharpScripts.Services.Sync.Google;

internal sealed record GoogleSheetsContext : IDisposable
{
	internal DriveService DriveService { get; init; }
	internal SheetsService SheetsService { get; init; }
	private readonly Dictionary<string, Spreadsheet> SpreadsheetCache = [];

	private GoogleSheetsContext(DriveService driveService, SheetsService sheetsService)
	{
		DriveService = driveService;
		SheetsService = sheetsService;
	}

	public static async Task<GoogleSheetsContext> CreateAsync(CancellationToken ct = default)
	{
		BaseClientService.Initializer initializer = await GoogleAuth.GetInitializerAsync(ct);
		return new GoogleSheetsContext(
			new DriveService(initializer),
			new SheetsService(initializer)
		);
	}

	public void Dispose()
	{
		SheetsService?.Dispose();
		DriveService?.Dispose();
		GC.SuppressFinalize(this);
	}

	internal async Task<Spreadsheet> GetSpreadsheetMetadataAsync(
		string spreadsheetId,
		bool forceRefresh = false,
		CancellationToken ct = default
	)
	{
		if (!forceRefresh && SpreadsheetCache.TryGetValue(spreadsheetId, out Spreadsheet? cached))
			return cached;

		Spreadsheet spreadsheet = await Resilience.ExecuteAsync(
			"Sheets.Get",
			async () =>
			{
				SpreadsheetsResource.GetRequest request = SheetsService.Spreadsheets.Get(
					spreadsheetId
				);
				request.Fields =
					"spreadsheetId,properties/title,sheets(properties(sheetId,title,index,gridProperties))";
				return await request.ExecuteAsync(ct);
			},
			ct
		);

		SpreadsheetCache[spreadsheetId] = spreadsheet;
		return spreadsheet;
	}

	internal void InvalidateCache(string spreadsheetId) => SpreadsheetCache.Remove(spreadsheetId);

	internal async Task<Sheet?> FindSheetAsync(
		string spreadsheetId,
		string sheetName,
		bool forceRefresh = false,
		CancellationToken ct = default
	)
	{
		Spreadsheet spreadsheet = await GetSpreadsheetMetadataAsync(
			spreadsheetId,
			forceRefresh,
			ct
		);
		return spreadsheet.Sheets?.FirstOrDefault(s =>
			s.Properties?.Title.EqualsExact(sheetName) == true
		);
	}

	public static string GetSpreadsheetUrl(string spreadsheetId) =>
		$"https://docs.google.com/spreadsheets/d/{spreadsheetId}";
}
