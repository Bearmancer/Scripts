namespace CSharpScripts.Services.Sync.Google;

internal sealed class SpreadsheetBootstrapper(GoogleSheetsService sheetsService)
{
	public async Task<string> GetOrCreateAsync(
		string? currentSpreadsheetId,
		string? defaultSpreadsheetId,
		string spreadsheetTitle,
		Action<string> onSpreadsheetResolved,
		CancellationToken ct = default
	)
	{
		if (
			!IsNullOrEmpty(currentSpreadsheetId)
			&& await sheetsService.SpreadsheetExistsAsync(currentSpreadsheetId, ct)
		)
			return currentSpreadsheetId;

		if (!IsNullOrEmpty(defaultSpreadsheetId))
		{
			if (await sheetsService.SpreadsheetExistsAsync(defaultSpreadsheetId, ct))
			{
				onSpreadsheetResolved(defaultSpreadsheetId);
				return defaultSpreadsheetId;
			}

			Log.Warning("Default spreadsheet not found: {0}", defaultSpreadsheetId);
		}

		Log.Information("Creating spreadsheet: {0}", spreadsheetTitle);
		var newId = await sheetsService.CreateSpreadsheetAsync(spreadsheetTitle, ct);
		onSpreadsheetResolved(newId);

		Log.Information("Created new spreadsheet: {0}", newId);
		return newId;
	}
}
