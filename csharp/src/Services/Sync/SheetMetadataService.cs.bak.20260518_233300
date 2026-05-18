// (removed pragma warning disables; resolving analyzer issues explicitly)

using DriveFile = Google.Apis.Drive.v3.Data.File;
using DriveFileList = Google.Apis.Drive.v3.Data.FileList;

namespace CSharpScripts.Services.Sync;

internal class SheetMetadataService(DriveService DriveService, SpreadsheetsResource Spreadsheets)
{
	private Dictionary<string, string> SpreadsheetIdCache { get; } = [];

	public async Task<string> EnsureSpreadsheetExistsAsync(
		string spreadsheetTitle,
		CancellationToken ct
	)
	{
		if (SpreadsheetIdCache.TryGetValue(key: spreadsheetTitle, out string? cachedId))
			return cachedId;

		DriveFileList files = await DriveService.Files.List().ExecuteAsync(ct);
		DriveFile? existingSpreadsheet = Enumerable.FirstOrDefault(
			files.Files,
			f =>
				f.Name == spreadsheetTitle
				&& f.MimeType == "application/vnd.google-apps.spreadsheet"
		);

		if (existingSpreadsheet != null)
		{
			SpreadsheetIdCache[key: spreadsheetTitle] = existingSpreadsheet.Id;
			return existingSpreadsheet.Id;
		}

		Spreadsheet newSpreadsheet = new()
		{
			Properties = new SpreadsheetProperties { Title = spreadsheetTitle },
			Sheets = [new Sheet { Properties = new SheetProperties { Title = "Sheet1" } }],
		};

		Spreadsheet createdSpreadsheet = await Spreadsheets
			.Create(body: newSpreadsheet)
			.ExecuteAsync(ct);

		SpreadsheetIdCache[key: spreadsheetTitle] = createdSpreadsheet.SpreadsheetId;
		return createdSpreadsheet.SpreadsheetId;
	}

	public async Task<bool> SpreadsheetExistsAsync(string spreadsheetTitle, CancellationToken ct)
	{
		if (SpreadsheetIdCache.ContainsKey(key: spreadsheetTitle))
			return true;

		DriveFileList files = await DriveService.Files.List().ExecuteAsync(ct);
		return Enumerable.Any(
			files.Files,
			f =>
				f.Name == spreadsheetTitle
				&& f.MimeType == "application/vnd.google-apps.spreadsheet"
		);
	}

	public async Task DeleteSpreadsheetAsync(string spreadsheetId, CancellationToken ct)
	{
		await DriveService.Files.Delete(fileId: spreadsheetId).ExecuteAsync(ct);
		InvalidateCache();
	}

	public async Task<bool> EnsureSubsheetExistsAsync(
		string spreadsheetId,
		string subsheetTitle,
		List<string> headers,
		CancellationToken ct
	)
	{
		Spreadsheet? metadata = await GetSpreadsheetMetadataAsync(spreadsheetId: spreadsheetId, ct);
		Sheet? sheet = metadata?.Sheets?.FirstOrDefault(s => s.Properties?.Title == subsheetTitle);

		if (sheet != null)
			return false;

		BatchUpdateSpreadsheetRequest request = new()
		{
			Requests =
			[
				new Request
				{
					AddSheet = new AddSheetRequest
					{
						Properties = new SheetProperties { Title = subsheetTitle },
					},
				},
			],
		};

		await Spreadsheets
			.BatchUpdate(body: request, spreadsheetId: spreadsheetId)
			.ExecuteAsync(ct);

		await EnsureHeadersForSheetAsync(
			spreadsheetId: spreadsheetId,
			sheetTitle: subsheetTitle,
			headers: headers,
			ct
		);

		return true;
	}

	public async Task<bool> SheetExistsAsync(
		string spreadsheetId,
		string sheetTitle,
		CancellationToken ct
	)
	{
		Spreadsheet? metadata = await GetSpreadsheetMetadataAsync(spreadsheetId: spreadsheetId, ct);
		return metadata?.Sheets?.Any(s => s.Properties?.Title == sheetTitle) ?? false;
	}

	public async Task<int> GetAlphabeticalInsertIndexAsync(
		string spreadsheetId,
		string newSheetTitle,
		CancellationToken ct
	)
	{
		Spreadsheet? metadata = await GetSpreadsheetMetadataAsync(spreadsheetId: spreadsheetId, ct);
		if (metadata?.Sheets == null)
			return 0;

		List<Sheet> sorted = [.. Enumerable.OrderBy(metadata.Sheets, s => s.Properties?.Title)];

		for (var i = 0; i < sorted.Count; i++)
		{
			if (
				Compare(
					strA: newSheetTitle,
					strB: sorted[index: i].Properties?.Title,
					comparisonType: Ordinal
				) < 0
			)
				return sorted[index: i].Properties?.Index ?? i;
		}

		return sorted.Count;
	}

	public async Task ReorderSheetsAlphabeticallyAsync(string spreadsheetId, CancellationToken ct)
	{
		Spreadsheet? metadata = await GetSpreadsheetMetadataAsync(spreadsheetId: spreadsheetId, ct);
		if (metadata?.Sheets == null)
			return;

		List<Sheet> sorted = [.. Enumerable.OrderBy(metadata.Sheets, s => s.Properties?.Title)];

		List<Request> requests = [];
		for (var i = 0; i < sorted.Count; i++)
		{
			if (sorted[index: i].Properties?.Index != i)
			{
				requests.Add(
					new Request
					{
						UpdateSheetProperties = new UpdateSheetPropertiesRequest
						{
							Properties = new SheetProperties
							{
								SheetId = sorted[index: i].Properties?.SheetId,
								Index = i,
							},
							Fields = "index",
						},
					}
				);
			}
		}

		if (requests.Count > 0)
		{
			BatchUpdateSpreadsheetRequest batchRequest = new() { Requests = requests };
			await Spreadsheets
				.BatchUpdate(body: batchRequest, spreadsheetId: spreadsheetId)
				.ExecuteAsync(ct);
		}
	}

	public async Task DeleteSubsheetAsync(
		string spreadsheetId,
		string subsheetTitle,
		CancellationToken ct
	)
	{
		var sheetId = await GetSheetIdAsync(
			spreadsheetId: spreadsheetId,
			sheetTitle: subsheetTitle,
			ct
		);
		if (!sheetId.HasValue)
			return;

		BatchUpdateSpreadsheetRequest request = new()
		{
			Requests =
			[
				new Request { DeleteSheet = new DeleteSheetRequest { SheetId = sheetId.Value } },
			],
		};

		await Spreadsheets
			.BatchUpdate(body: request, spreadsheetId: spreadsheetId)
			.ExecuteAsync(ct);
	}

	public async Task<List<string>> GetSubsheetNamesAsync(
		string spreadsheetId,
		CancellationToken ct
	)
	{
		Spreadsheet? metadata = await GetSpreadsheetMetadataAsync(spreadsheetId: spreadsheetId, ct);
		return metadata?.Sheets?.Select(s => s.Properties?.Title ?? Empty).ToList() ?? [];
	}

	public async Task RenameSubsheetAsync(
		string spreadsheetId,
		string oldTitle,
		string newTitle,
		CancellationToken ct
	)
	{
		var sheetId = await GetSheetIdAsync(spreadsheetId: spreadsheetId, sheetTitle: oldTitle, ct);
		if (!sheetId.HasValue)
			throw new InvalidOperationException($"Sheet '{oldTitle}' not found");

		BatchUpdateSpreadsheetRequest request = new()
		{
			Requests =
			[
				new Request
				{
					UpdateSheetProperties = new UpdateSheetPropertiesRequest
					{
						Properties = new SheetProperties
						{
							SheetId = sheetId.Value,
							Title = newTitle,
						},
						Fields = "title",
					},
				},
			],
		};

		await Spreadsheets
			.BatchUpdate(body: request, spreadsheetId: spreadsheetId)
			.ExecuteAsync(ct);
	}

	public async Task CleanupDefaultSheetAsync(string spreadsheetId, CancellationToken ct)
	{
		Spreadsheet? metadata = await GetSpreadsheetMetadataAsync(spreadsheetId: spreadsheetId, ct);
		if (metadata?.Sheets == null || metadata.Sheets.Count < 2)
			return;

		Sheet? defaultSheet = await FindSheetAsync(
			spreadsheetId: spreadsheetId,
			sheetTitle: "Sheet1",
			ct
		);
		if (defaultSheet?.Properties?.SheetId == null)
			return;

		IList<RowData>? rows = defaultSheet.Data?.FirstOrDefault()?.RowData;
		var isEmpty =
			rows == null
			|| rows.Count == 0
			|| Enumerable.All(
				rows,
				r =>
					r.Values == null
					|| Enumerable.All(r.Values, v => IsNullOrEmpty(value: v.FormattedValue))
			);

		if (!isEmpty)
			return;

		BatchUpdateSpreadsheetRequest request = new()
		{
			Requests =
			[
				new Request
				{
					DeleteSheet = new DeleteSheetRequest
					{
						SheetId = defaultSheet.Properties.SheetId.Value,
					},
				},
			],
		};

		await Spreadsheets
			.BatchUpdate(body: request, spreadsheetId: spreadsheetId)
			.ExecuteAsync(ct);
	}

	public async Task<List<string>> FindDuplicateSpreadsheetsAsync(
		string searchTitle,
		CancellationToken ct
	)
	{
		DriveFileList files = await DriveService.Files.List().ExecuteAsync(ct);
		return Enumerable.ToList(
			Enumerable.Select(
				Enumerable.Where(
					files.Files,
					f =>
						f.Name == searchTitle
						&& f.MimeType == "application/vnd.google-apps.spreadsheet"
				),
				f => f.Id
			)
		);
	}

	private async Task<Spreadsheet?> GetSpreadsheetMetadataAsync(
		string spreadsheetId,
		CancellationToken ct
	)
	{
		try
		{
			return await Spreadsheets.Get(spreadsheetId: spreadsheetId).ExecuteAsync(ct);
		}
		catch
		{
			return null;
		}
	}

	private void InvalidateCache() => SpreadsheetIdCache.Clear();

	private async Task<Sheet?> FindSheetAsync(
		string spreadsheetId,
		string sheetTitle,
		CancellationToken ct
	)
	{
		Spreadsheet? metadata = await GetSpreadsheetMetadataAsync(spreadsheetId: spreadsheetId, ct);
		return metadata?.Sheets?.FirstOrDefault(s => s.Properties?.Title == sheetTitle);
	}

	private async Task<int?> GetSheetIdAsync(
		string spreadsheetId,
		string sheetTitle,
		CancellationToken ct
	)
	{
		Sheet? sheet = await FindSheetAsync(
			spreadsheetId: spreadsheetId,
			sheetTitle: sheetTitle,
			ct
		);
		return sheet?.Properties?.SheetId;
	}

	private async Task RenameDefaultSheetAsync(
		string spreadsheetId,
		string newTitle,
		CancellationToken ct
	)
	{
		Spreadsheet? metadata = await GetSpreadsheetMetadataAsync(spreadsheetId: spreadsheetId, ct);
		Sheet? defaultSheet = metadata?.Sheets?.FirstOrDefault(s =>
			s.Properties?.Title == "Sheet1"
		);

		if (defaultSheet?.Properties?.SheetId == null)
			return;

		BatchUpdateSpreadsheetRequest request = new()
		{
			Requests =
			[
				new Request
				{
					UpdateSheetProperties = new UpdateSheetPropertiesRequest
					{
						Properties = new SheetProperties
						{
							SheetId = defaultSheet.Properties.SheetId.Value,
							Title = newTitle,
						},
						Fields = "title",
					},
				},
			],
		};

		await Spreadsheets
			.BatchUpdate(body: request, spreadsheetId: spreadsheetId)
			.ExecuteAsync(ct);
	}

	private async Task EnsureHeadersForSheetAsync(
		string spreadsheetId,
		string sheetTitle,
		List<string> headers,
		CancellationToken ct
	)
	{
		var range = $"{sheetTitle}!A1:{GetColumnLetter(columnNumber: headers.Count)}1";
		ValueRange values = new()
		{
			Range = range,
			Values = [Enumerable.ToList(Enumerable.Cast<object>(headers))],
		};

		await Spreadsheets
			.Values.Update(body: values, spreadsheetId: spreadsheetId, range: range)
			.ExecuteAsync(ct);
	}

	private async Task EnsureHeadersAsync(
		string spreadsheetId,
		string sheetTitle,
		List<string> headers,
		CancellationToken ct
	) =>
		await EnsureHeadersForSheetAsync(
			spreadsheetId: spreadsheetId,
			sheetTitle: sheetTitle,
			headers: headers,
			ct
		);

	private static string GetColumnLetter(int columnNumber)
	{
		var columnLetter = Empty;
		while (columnNumber > 0)
		{
			var modulo = (columnNumber - 1) % 26;
			columnLetter = Convert.ToChar('A' + modulo) + columnLetter;
			columnNumber = (columnNumber - modulo) / 26;
		}
		return columnLetter;
	}
}
