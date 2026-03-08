#pragma warning disable IDE0007, IDE0051, IDE0005, IDE0008, IDE0305, IDE0059, CA1806

using CSharpScripts.Models;
using Google.Apis.Sheets.v4.Data;

namespace CSharpScripts.Services.Sync;

internal class SheetRowService(SpreadsheetsResource Spreadsheets)
{
	public async Task ClearSubsheetAsync(
		string spreadsheetId,
		string sheetName,
		CancellationToken ct
	)
	{
		string range = $"{sheetName}!A1:ZZ";
		await Spreadsheets
			.Values.Clear(new ClearValuesRequest(), spreadsheetId, range)
			.ExecuteAsync(ct);
	}

	public async Task WriteRowsAsync(
		string spreadsheetId,
		string sheetName,
		List<List<object>> rows,
		CancellationToken ct
	)
	{
		if (rows.Count == 0)
			return;

		await ClearSubsheetAsync(spreadsheetId, sheetName, ct);

		string range = $"{sheetName}!A1";
		ValueRange body = new() { Values = rows.Cast<IList<object>>().ToList() };

		SpreadsheetsResource.ValuesResource.UpdateRequest request = Spreadsheets.Values.Update(
			body,
			spreadsheetId,
			range
		);
		request.ValueInputOption = SpreadsheetsResource
			.ValuesResource
			.UpdateRequest
			.ValueInputOptionEnum
			.USERENTERED;

		await request.ExecuteAsync(ct);
	}

	public async Task<int> AppendRowsAsync(
		string spreadsheetId,
		string sheetName,
		List<List<object>> rows,
		CancellationToken ct
	)
	{
		if (rows.Count == 0)
			return 0;

		string range = $"{sheetName}!A:A";
		var body = new ValueRange { Values = rows.Cast<IList<object>>().ToList() };

		var request = Spreadsheets.Values.Append(body, spreadsheetId, range);
		request.ValueInputOption = SpreadsheetsResource
			.ValuesResource
			.AppendRequest
			.ValueInputOptionEnum
			.USERENTERED;

		AppendValuesResponse response = await request.ExecuteAsync(ct);
		return rows.Count;
	}

	public async Task<DateTime?> GetLatestScrobbleTimeAsync(
		string spreadsheetId,
		string sheetName,
		CancellationToken ct
	)
	{
		string range = $"{sheetName}!A:B";
		SpreadsheetsResource.ValuesResource.GetRequest request = Spreadsheets.Values.Get(
			spreadsheetId,
			range
		);

		ValueRange response = await request.ExecuteAsync(ct);
		if (response.Values == null || response.Values.Count <= 1)
			return null;

		for (int i = response.Values.Count - 1; i >= 1; i--)
		{
			IList<object> row = response.Values[i];
			if (row.Count >= 2 && row[1] is string dateStr && !string.IsNullOrEmpty(dateStr))
			{
				if (DateTime.TryParse(dateStr, out DateTime timestamp))
					return timestamp;
			}
		}

		return null;
	}

	public async Task<int> GetScrobbleCountAsync(
		string spreadsheetId,
		string sheetName,
		CancellationToken ct
	)
	{
		string range = $"{sheetName}!A:A";
		var request = Spreadsheets.Values.Get(spreadsheetId, range);

		var response = await request.ExecuteAsync(ct);
		return (response.Values?.Count ?? 0) - 1;
	}

	public async Task<int> DeleteScrobblesOnOrAfterAsync(
		string spreadsheetId,
		string sheetName,
		DateTime fromDate,
		CancellationToken ct
	)
	{
		string range = $"{sheetName}!A:B";
		SpreadsheetsResource.ValuesResource.GetRequest request = Spreadsheets.Values.Get(
			spreadsheetId,
			range
		);

		ValueRange response = await request.ExecuteAsync(ct);
		if (response.Values == null || response.Values.Count <= 1)
			return 0;

		List<int> rowsToDelete = [];

		for (int i = 1; i < response.Values.Count; i++)
		{
			IList<object> row = response.Values[i];
			if (row.Count >= 2 && row[1] is string dateStr && !string.IsNullOrEmpty(dateStr))
			{
				if (DateTime.TryParse(dateStr, out DateTime timestamp) && timestamp >= fromDate)
				{
					rowsToDelete.Add(i);
				}
			}
		}

		if (rowsToDelete.Count == 0)
			return 0;

		await DeleteRowsFromSubsheetAsync(spreadsheetId, sheetName, rowsToDelete, ct);
		return rowsToDelete.Count;
	}

	public async Task<List<Scrobble>> GetNewScrobblesAsync(
		string spreadsheetId,
		string sheetName,
		DateTime? afterDate,
		CancellationToken ct
	)
	{
		string range = $"{sheetName}!A:E";
		var request = Spreadsheets.Values.Get(spreadsheetId, range);

		var response = await request.ExecuteAsync(ct);
		if (response.Values == null || response.Values.Count <= 1)
			return [];

		List<Scrobble> scrobbles = [];

		for (int i = 1; i < response.Values.Count; i++)
		{
			IList<object> row = response.Values[i];
			if (row.Count >= 5)
			{
				DateTime.TryParse(row[1]?.ToString(), out DateTime timestamp);

				if (afterDate == null || timestamp > afterDate)
				{
					scrobbles.Add(
						new Scrobble(
							row[2]?.ToString() ?? string.Empty,
							row[0]?.ToString() ?? string.Empty,
							row[3]?.ToString() ?? string.Empty,
							timestamp
						)
					);
				}
			}
		}

		return scrobbles;
	}

	public async Task WriteScrobblesAsync(
		string spreadsheetId,
		string sheetName,
		List<Scrobble> scrobbles,
		CancellationToken ct
	)
	{
		List<List<object>> rows =
		[
			["Track", "Artist", "Album", "Played At"],
			.. scrobbles.Select(s => new List<object>
			{
				s.TrackName,
				s.ArtistName,
				s.AlbumName,
				s.PlayedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? string.Empty,
			}),
		];

		await WriteRowsAsync(spreadsheetId, sheetName, rows, ct);
	}

	public async Task DeleteRowsFromSubsheetAsync(
		string spreadsheetId,
		string sheetName,
		List<int> zeroBasedRowIndices,
		CancellationToken ct
	)
	{
		if (zeroBasedRowIndices.Count == 0)
			return;

		List<int> sortedIndices = [.. zeroBasedRowIndices.OrderByDescending(x => x)];

		foreach (int rowIndex in sortedIndices)
		{
			await InsertRowsAsync(spreadsheetId, sheetName, rowIndex, 1, ct, deleteMode: true);
		}
	}

	public async Task<int> ExportEachSheetAsCSVAsync(
		string spreadsheetId,
		string outputDirectory,
		CancellationToken ct
	)
	{
		SpreadsheetsResource.GetRequest metadataRequest = Spreadsheets.Get(spreadsheetId);
		Spreadsheet metadata = await metadataRequest.ExecuteAsync(ct);

		if (metadata.Sheets == null)
			return 0;

		List<Sheet> sheets = [.. metadata.Sheets.OrderBy(s => s.Properties?.Title)];

		foreach (Sheet sheet in sheets)
		{
			string? sheetTitle = sheet.Properties?.Title;
			int? sheetId = sheet.Properties?.SheetId;

			if (sheetTitle == null || sheetId == null)
				continue;

			string range = $"{sheetTitle}!A:ZZ";
			var dataRequest = Spreadsheets.Values.Get(spreadsheetId, range);
			var values = await dataRequest.ExecuteAsync(ct);

			if (values.Values == null || values.Values.Count == 0)
				continue;

			string csvPath = Path.Combine(outputDirectory, $"{sheetTitle}.csv");

			await using StreamWriter writer = new(csvPath);
			foreach (IList<object> row in values.Values)
			{
				string csvLine = string.Join(",", row.Select(cell => $"\"{cell}\""));
				await writer.WriteLineAsync(csvLine);
			}
		}

		return sheets.Count;
	}

	private async Task InsertRowsAsync(
		string spreadsheetId,
		string sheetName,
		int startRowIndex,
		int numRows,
		CancellationToken ct,
		bool deleteMode = false
	)
	{
		SpreadsheetsResource.GetRequest metadataRequest = Spreadsheets.Get(spreadsheetId);
		Spreadsheet metadata = await metadataRequest.ExecuteAsync(ct);

		Sheet? sheet = metadata.Sheets?.FirstOrDefault(s => s.Properties?.Title == sheetName);
		if (sheet?.Properties?.SheetId == null)
			return;

		int sheetId = sheet.Properties.SheetId.Value;

		Request request = deleteMode
			? new Request
			{
				DeleteDimension = new DeleteDimensionRequest
				{
					Range = new DimensionRange
					{
						SheetId = sheetId,
						Dimension = "ROWS",
						StartIndex = startRowIndex,
						EndIndex = startRowIndex + numRows,
					},
				},
			}
			: new Request
			{
				InsertDimension = new InsertDimensionRequest
				{
					Range = new DimensionRange
					{
						SheetId = sheetId,
						Dimension = "ROWS",
						StartIndex = startRowIndex,
						EndIndex = startRowIndex + numRows,
					},
					InheritFromBefore = false,
				},
			};

		BatchUpdateSpreadsheetRequest batchRequest = new() { Requests = [request] };

		await Spreadsheets.BatchUpdate(batchRequest, spreadsheetId).ExecuteAsync(ct);
	}
}
