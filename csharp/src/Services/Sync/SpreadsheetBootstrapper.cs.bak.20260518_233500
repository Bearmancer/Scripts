namespace CSharpScripts.Services.Sync.Google;

internal sealed class SpreadsheetBootstrapper(GoogleSheetsService sheetsService)
{
	public async Task<string> GetOrCreateAsync(
		string? currentSpreadsheetId,
		string? defaultSpreadsheetId,
		string spreadsheetTitle,
		Func<string, Task> onSpreadsheetResolved,
		CancellationToken ct = default
	)
	{
		if (
			!IsNullOrEmpty(value: currentSpreadsheetId)
			&& await sheetsService.SpreadsheetExistsAsync(spreadsheetId: currentSpreadsheetId, ct)
		)
			return currentSpreadsheetId;

		if (!IsNullOrEmpty(value: defaultSpreadsheetId))
		{
			if (await sheetsService.SpreadsheetExistsAsync(spreadsheetId: defaultSpreadsheetId, ct))
			{
				await onSpreadsheetResolved(defaultSpreadsheetId);
				return defaultSpreadsheetId;
			}

			Log.Warning("Default spreadsheet not found: {0}", defaultSpreadsheetId);
		}

		Log.Information("Creating spreadsheet: {0}", spreadsheetTitle);
		var newId = await sheetsService.CreateSpreadsheetAsync(title: spreadsheetTitle, ct);
		await onSpreadsheetResolved(newId);

		Log.Information("Created new spreadsheet: {0}", newId);
		return newId;
	}
}
