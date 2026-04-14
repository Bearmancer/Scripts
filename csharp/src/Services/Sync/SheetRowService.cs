// (removed pragma warning disables; resolving analyzer issues explicitly)

namespace CSharpScripts.Services.Sync;

internal class SheetRowService(SpreadsheetsResource Spreadsheets)
{
	public async Task ClearSubsheetAsync(
		string spreadsheetId,
		string sheetName,
		CancellationToken ct
	)
	{
		var range = $"{sheetName}!A1:ZZ";
		await Spreadsheets
			.Values.Clear(new ClearValuesRequest(), spreadsheetId: spreadsheetId, range: range)
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

		await ClearSubsheetAsync(spreadsheetId: spreadsheetId, sheetName: sheetName, ct);

		var range = $"{sheetName}!A1";
		var values = new List<IList<object>>(rows.Count);
		foreach (List<object> row in rows)
			values.Add(row);

		ValueRange body = new() { Values = values };

		SpreadsheetsResource.ValuesResource.UpdateRequest request = Spreadsheets.Values.Update(
			body: body,
			spreadsheetId: spreadsheetId,
			range: range
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

		var range = $"{sheetName}!A:A";
		var values = new List<IList<object>>(rows.Count);
		foreach (List<object> row in rows)
			values.Add(row);

		var body = new ValueRange { Values = values };

		SpreadsheetsResource.ValuesResource.AppendRequest? request = Spreadsheets.Values.Append(
			body: body,
			spreadsheetId: spreadsheetId,
			range: range
		);
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
		var range = $"{sheetName}!A:B";
		SpreadsheetsResource.ValuesResource.GetRequest request = Spreadsheets.Values.Get(
			spreadsheetId: spreadsheetId,
			range: range
		);

		ValueRange response = await request.ExecuteAsync(ct);
		IList<IList<object>>? values = response.Values;
		if (values == null || values.Count <= 1)
			return null;

		var valueCount = values.Count;

		for (var i = valueCount - 1; i >= 1; i--)
		{
			IList<object> row = values[index: i];
			var rowCount = row.Count;
			if (rowCount >= 2 && row[index: 1] is string dateStr && !IsNullOrEmpty(value: dateStr))
			{
				if (DateTime.TryParse(s: dateStr, out DateTime timestamp))
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
		var range = $"{sheetName}!A:A";
		SpreadsheetsResource.ValuesResource.GetRequest? request = Spreadsheets.Values.Get(
			spreadsheetId: spreadsheetId,
			range: range
		);

		ValueRange? response = await request.ExecuteAsync(ct);
		return (response.Values?.Count ?? 0) - 1;
	}

	public async Task<int> DeleteScrobblesOnOrAfterAsync(
		string spreadsheetId,
		string sheetName,
		DateTime fromDate,
		CancellationToken ct
	)
	{
		var range = $"{sheetName}!A:B";
		SpreadsheetsResource.ValuesResource.GetRequest request = Spreadsheets.Values.Get(
			spreadsheetId: spreadsheetId,
			range: range
		);

		ValueRange response = await request.ExecuteAsync(ct);
		IList<IList<object>>? values = response.Values;
		if (values == null || values.Count <= 1)
			return 0;

		var valueCount = values.Count;
		var rowsToDelete = new List<int>(valueCount - 1);

		for (var i = 1; i < valueCount; i++)
		{
			IList<object> row = values[index: i];
			var rowCount = row.Count;
			if (rowCount >= 2 && row[index: 1] is string dateStr && !IsNullOrEmpty(value: dateStr))
			{
				if (DateTime.TryParse(s: dateStr, out DateTime timestamp) && timestamp >= fromDate)
					rowsToDelete.Add(item: i);
			}
		}

		if (rowsToDelete.Count == 0)
			return 0;

		await DeleteRowsFromSubsheetAsync(
			spreadsheetId: spreadsheetId,
			sheetName: sheetName,
			zeroBasedRowIndices: rowsToDelete,
			ct
		);
		return rowsToDelete.Count;
	}

	public async Task<List<Scrobble>> GetNewScrobblesAsync(
		string spreadsheetId,
		string sheetName,
		DateTime? afterDate,
		CancellationToken ct
	)
	{
		var range = $"{sheetName}!A:E";
		SpreadsheetsResource.ValuesResource.GetRequest? request = Spreadsheets.Values.Get(
			spreadsheetId: spreadsheetId,
			range: range
		);

		ValueRange? response = await request.ExecuteAsync(ct);
		IList<IList<object>>? values = response.Values;
		if (values == null || values.Count <= 1)
			return [];

		var valueCount = values.Count;
		var scrobbles = new List<Scrobble>(valueCount - 1);

		for (var i = 1; i < valueCount; i++)
		{
			IList<object> row = values[index: i];
			var rowCount = row.Count;
			if (rowCount >= 5)
			{
				DateTime.TryParse(row[index: 1]?.ToString(), out DateTime timestamp);

				if (afterDate == null || timestamp > afterDate)
				{
					scrobbles.Add(
						new Scrobble(
							row[index: 2]?.ToString() ?? Empty,
							row[index: 0]?.ToString() ?? Empty,
							row[index: 3]?.ToString() ?? Empty,
							PlayedAt: timestamp
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
		var rows = new List<List<object>>(scrobbles.Count + 1);
		rows.Add(new List<object> { "Track", "Artist", "Album", "Played At" });
		foreach (Scrobble scrobble in scrobbles)
		{
			rows.Add(
				new List<object>
				{
					scrobble.TrackName,
					scrobble.ArtistName,
					scrobble.AlbumName,
					scrobble.PlayedAt?.ToString(format: "yyyy-MM-dd HH:mm:ss") ?? Empty,
				}
			);
		}

		await WriteRowsAsync(spreadsheetId: spreadsheetId, sheetName: sheetName, rows: rows, ct);
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

		// Batch contiguous delete ranges into a single BatchUpdate to avoid one
		// round-trip per deleted row (which previously fetched metadata each time).
		List<int> sortedDesc = [.. zeroBasedRowIndices];
		sortedDesc.Sort((a, b) => a.CompareTo(b));

		// Group into contiguous ranges
		var ranges = new List<(int Start, int End)>();
		int rangeStart = sortedDesc[0];
		int rangeEnd = sortedDesc[0];
		for (var i = 1; i < sortedDesc.Count; i++)
		{
			int idx = sortedDesc[i];
			if (idx == rangeEnd + 1)
			{
				rangeEnd = idx;
			}
			else
			{
				ranges.Add((rangeStart, rangeEnd));
				rangeStart = idx;
				rangeEnd = idx;
			}
		}
		ranges.Add((rangeStart, rangeEnd));

		// Get sheet id once
		SpreadsheetsResource.GetRequest metadataRequest = Spreadsheets.Get(
			spreadsheetId: spreadsheetId
		);
		Spreadsheet metadata = await metadataRequest.ExecuteAsync(ct);

		Sheet? sheet = metadata.Sheets?.FirstOrDefault(s => s.Properties?.Title == sheetName);
		if (sheet?.Properties?.SheetId == null)
			return;

		var sheetId = sheet.Properties.SheetId.Value;

		List<Request> requests = new();
		foreach (var (Start, End) in ranges)
		{
			requests.Add(
				new Request
				{
					DeleteDimension = new DeleteDimensionRequest
					{
						Range = new DimensionRange
						{
							SheetId = sheetId,
							Dimension = "ROWS",
							StartIndex = Start,
							EndIndex = End + 1,
						},
					},
				}
			);
		}

		BatchUpdateSpreadsheetRequest batchRequest = new() { Requests = requests };

		await Spreadsheets
			.BatchUpdate(body: batchRequest, spreadsheetId: spreadsheetId)
			.ExecuteAsync(ct);
	}

	public async Task<int> ExportEachSheetAsCSVAsync(
		string spreadsheetId,
		string outputDirectory,
		CancellationToken ct
	)
	{
		SpreadsheetsResource.GetRequest metadataRequest = Spreadsheets.Get(
			spreadsheetId: spreadsheetId
		);
		Spreadsheet metadata = await metadataRequest.ExecuteAsync(ct);

		if (metadata.Sheets == null)
			return 0;

		List<Sheet> sheets = [.. Enumerable.OrderBy(metadata.Sheets, s => s.Properties?.Title)];

		foreach (Sheet sheet in sheets)
		{
			var sheetTitle = sheet.Properties?.Title;
			var sheetId = sheet.Properties?.SheetId;

			if (sheetTitle == null || sheetId == null)
				continue;

			var range = $"{sheetTitle}!A:ZZ";
			SpreadsheetsResource.ValuesResource.GetRequest? dataRequest = Spreadsheets.Values.Get(
				spreadsheetId: spreadsheetId,
				range: range
			);
			ValueRange? values = await dataRequest.ExecuteAsync(ct);

			if (values.Values == null || values.Values.Count == 0)
				continue;

			var csvPath = Path.Combine(path1: outputDirectory, $"{sheetTitle}.csv");

			await using StreamWriter writer = new(path: csvPath);
			foreach (IList<object> row in values.Values)
			{
				var csvLine = Join(separator: ",", Enumerable.Select(row, cell => $"\"{cell}\""));
				await writer.WriteLineAsync(value: csvLine);
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
		SpreadsheetsResource.GetRequest metadataRequest = Spreadsheets.Get(
			spreadsheetId: spreadsheetId
		);
		Spreadsheet metadata = await metadataRequest.ExecuteAsync(ct);

		Sheet? sheet = metadata.Sheets?.FirstOrDefault(s => s.Properties?.Title == sheetName);
		if (sheet?.Properties?.SheetId == null)
			return;

		var sheetId = sheet.Properties.SheetId.Value;

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

		await Spreadsheets
			.BatchUpdate(body: batchRequest, spreadsheetId: spreadsheetId)
			.ExecuteAsync(ct);
	}
}
