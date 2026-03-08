#pragma warning disable IDE0007, IDE0051, IDE0005, IDE0305, CA1031, IDE0022

using Google.Apis.Drive.v3;
using Google.Apis.Sheets.v4.Data;
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
		if (SpreadsheetIdCache.TryGetValue(spreadsheetTitle, out string? cachedId))
			return cachedId;

		DriveFileList files = await DriveService.Files.List().ExecuteAsync(ct);
		DriveFile? existingSpreadsheet = files.Files.FirstOrDefault(f =>
			f.Name == spreadsheetTitle && f.MimeType == "application/vnd.google-apps.spreadsheet"
		);

		if (existingSpreadsheet != null)
		{
			SpreadsheetIdCache[spreadsheetTitle] = existingSpreadsheet.Id;
			return existingSpreadsheet.Id;
		}

		Spreadsheet newSpreadsheet = new()
		{
			Properties = new SpreadsheetProperties { Title = spreadsheetTitle },
			Sheets = [new Sheet { Properties = new SheetProperties { Title = "Sheet1" } }],
		};

		Spreadsheet createdSpreadsheet = await Spreadsheets.Create(newSpreadsheet).ExecuteAsync(ct);

		SpreadsheetIdCache[spreadsheetTitle] = createdSpreadsheet.SpreadsheetId;
		return createdSpreadsheet.SpreadsheetId;
	}

	public async Task<bool> SpreadsheetExistsAsync(string spreadsheetTitle, CancellationToken ct)
	{
		if (SpreadsheetIdCache.ContainsKey(spreadsheetTitle))
			return true;

		DriveFileList files = await DriveService.Files.List().ExecuteAsync(ct);
		return files.Files.Any(f =>
			f.Name == spreadsheetTitle && f.MimeType == "application/vnd.google-apps.spreadsheet"
		);
	}

	public async Task DeleteSpreadsheetAsync(string spreadsheetId, CancellationToken ct)
	{
		await DriveService.Files.Delete(spreadsheetId).ExecuteAsync(ct);
		InvalidateCache();
	}

	public async Task<bool> EnsureSubsheetExistsAsync(
		string spreadsheetId,
		string subsheetTitle,
		List<string> headers,
		CancellationToken ct
	)
	{
		Spreadsheet? metadata = await GetSpreadsheetMetadataAsync(spreadsheetId, ct);
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

		await Spreadsheets.BatchUpdate(request, spreadsheetId).ExecuteAsync(ct);

		await EnsureHeadersForSheetAsync(spreadsheetId, subsheetTitle, headers, ct);

		return true;
	}

	public async Task<bool> SheetExistsAsync(
		string spreadsheetId,
		string sheetTitle,
		CancellationToken ct
	)
	{
		Spreadsheet? metadata = await GetSpreadsheetMetadataAsync(spreadsheetId, ct);
		return metadata?.Sheets?.Any(s => s.Properties?.Title == sheetTitle) ?? false;
	}

	public async Task<int> GetAlphabeticalInsertIndexAsync(
		string spreadsheetId,
		string newSheetTitle,
		CancellationToken ct
	)
	{
		Spreadsheet? metadata = await GetSpreadsheetMetadataAsync(spreadsheetId, ct);
		if (metadata?.Sheets == null)
			return 0;

		List<Sheet> sorted = [.. metadata.Sheets.OrderBy(s => s.Properties?.Title)];

		for (int i = 0; i < sorted.Count; i++)
		{
			if (
				string.Compare(newSheetTitle, sorted[i].Properties?.Title, StringComparison.Ordinal)
				< 0
			)
			{
				return sorted[i].Properties?.Index ?? i;
			}
		}

		return sorted.Count;
	}

	public async Task ReorderSheetsAlphabeticallyAsync(string spreadsheetId, CancellationToken ct)
	{
		Spreadsheet? metadata = await GetSpreadsheetMetadataAsync(spreadsheetId, ct);
		if (metadata?.Sheets == null)
			return;

		List<Sheet> sorted = [.. metadata.Sheets.OrderBy(s => s.Properties?.Title)];

		List<Request> requests = [];
		for (int i = 0; i < sorted.Count; i++)
		{
			if (sorted[i].Properties?.Index != i)
			{
				requests.Add(
					new Request
					{
						UpdateSheetProperties = new UpdateSheetPropertiesRequest
						{
							Properties = new SheetProperties
							{
								SheetId = sorted[i].Properties?.SheetId,
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
			await Spreadsheets.BatchUpdate(batchRequest, spreadsheetId).ExecuteAsync(ct);
		}
	}

	public async Task DeleteSubsheetAsync(
		string spreadsheetId,
		string subsheetTitle,
		CancellationToken ct
	)
	{
		int? sheetId = await GetSheetIdAsync(spreadsheetId, subsheetTitle, ct);
		if (!sheetId.HasValue)
			return;

		BatchUpdateSpreadsheetRequest request = new()
		{
			Requests =
			[
				new Request { DeleteSheet = new DeleteSheetRequest { SheetId = sheetId.Value } },
			],
		};

		await Spreadsheets.BatchUpdate(request, spreadsheetId).ExecuteAsync(ct);
	}

	public async Task<List<string>> GetSubsheetNamesAsync(
		string spreadsheetId,
		CancellationToken ct
	)
	{
		Spreadsheet? metadata = await GetSpreadsheetMetadataAsync(spreadsheetId, ct);
		return metadata?.Sheets?.Select(s => s.Properties?.Title ?? string.Empty).ToList() ?? [];
	}

	public async Task RenameSubsheetAsync(
		string spreadsheetId,
		string oldTitle,
		string newTitle,
		CancellationToken ct
	)
	{
		int? sheetId = await GetSheetIdAsync(spreadsheetId, oldTitle, ct);
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

		await Spreadsheets.BatchUpdate(request, spreadsheetId).ExecuteAsync(ct);
	}

	public async Task CleanupDefaultSheetAsync(string spreadsheetId, CancellationToken ct)
	{
		Spreadsheet? metadata = await GetSpreadsheetMetadataAsync(spreadsheetId, ct);
		if (metadata?.Sheets == null || metadata.Sheets.Count < 2)
			return;

		Sheet? defaultSheet = await FindSheetAsync(spreadsheetId, "Sheet1", ct);
		if (defaultSheet?.Properties?.SheetId == null)
			return;

		IList<RowData>? rows = defaultSheet.Data?.FirstOrDefault()?.RowData;
		bool isEmpty =
			rows == null
			|| rows.Count == 0
			|| rows.All(r =>
				r.Values == null || r.Values.All(v => string.IsNullOrEmpty(v.FormattedValue))
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

		await Spreadsheets.BatchUpdate(request, spreadsheetId).ExecuteAsync(ct);
	}

	public async Task<List<string>> FindDuplicateSpreadsheetsAsync(
		string searchTitle,
		CancellationToken ct
	)
	{
		DriveFileList files = await DriveService.Files.List().ExecuteAsync(ct);
		return files
			.Files.Where(f =>
				f.Name == searchTitle && f.MimeType == "application/vnd.google-apps.spreadsheet"
			)
			.Select(f => f.Id)
			.ToList();
	}

	private async Task<Spreadsheet?> GetSpreadsheetMetadataAsync(
		string spreadsheetId,
		CancellationToken ct
	)
	{
		try
		{
			return await Spreadsheets.Get(spreadsheetId).ExecuteAsync(ct);
		}
		catch
		{
			return null;
		}
	}

	private void InvalidateCache()
	{
		SpreadsheetIdCache.Clear();
	}

	private async Task<Sheet?> FindSheetAsync(
		string spreadsheetId,
		string sheetTitle,
		CancellationToken ct
	)
	{
		Spreadsheet? metadata = await GetSpreadsheetMetadataAsync(spreadsheetId, ct);
		return metadata?.Sheets?.FirstOrDefault(s => s.Properties?.Title == sheetTitle);
	}

	private async Task<int?> GetSheetIdAsync(
		string spreadsheetId,
		string sheetTitle,
		CancellationToken ct
	)
	{
		Sheet? sheet = await FindSheetAsync(spreadsheetId, sheetTitle, ct);
		return sheet?.Properties?.SheetId;
	}

	private async Task RenameDefaultSheetAsync(
		string spreadsheetId,
		string newTitle,
		CancellationToken ct
	)
	{
		Spreadsheet? metadata = await GetSpreadsheetMetadataAsync(spreadsheetId, ct);
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

		await Spreadsheets.BatchUpdate(request, spreadsheetId).ExecuteAsync(ct);
	}

	private async Task EnsureHeadersForSheetAsync(
		string spreadsheetId,
		string sheetTitle,
		List<string> headers,
		CancellationToken ct
	)
	{
		string range = $"{sheetTitle}!A1:{GetColumnLetter(headers.Count)}1";
		ValueRange values = new() { Range = range, Values = [headers.Cast<object>().ToList()] };

		await Spreadsheets.Values.Update(values, spreadsheetId, range).ExecuteAsync(ct);
	}

	private async Task EnsureHeadersAsync(
		string spreadsheetId,
		string sheetTitle,
		List<string> headers,
		CancellationToken ct
	)
	{
		await EnsureHeadersForSheetAsync(spreadsheetId, sheetTitle, headers, ct);
	}

	private static string GetColumnLetter(int columnNumber)
	{
		string columnLetter = string.Empty;
		while (columnNumber > 0)
		{
			int modulo = (columnNumber - 1) % 26;
			columnLetter = Convert.ToChar('A' + modulo) + columnLetter;
			columnNumber = (columnNumber - modulo) / 26;
		}
		return columnLetter;
	}
}
