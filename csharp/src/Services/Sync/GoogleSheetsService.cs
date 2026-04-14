using Google;

// (removed pragma warning disable IDE0052; will address unused private fields explicitly)

namespace CSharpScripts.Services.Sync.Google;

internal sealed class GoogleSheetsService : IDisposable
{
	private const string SheetName = "Scrobbles";
	private const string SpreadsheetTitle = "last.fm scrobbles";

	private static readonly FrozenSet<object> Headers = FrozenSet.ToFrozenSet<object>([
		"Date",
		"Track Title",
		"Artist",
		"Album",
	]);

	private static readonly string[] DateFormats =
	[
		"yyyy/MM/dd HH:mm:ss",
		"yyyy/MM/dd HH:mm",
		"yyyy/MM/dd H:mm:ss",
		"yyyy/MM/dd H:mm",
	];

	private readonly DriveService DriveService;

	// removed unused helper service fields (FormattingService, MetadataService, RowService)
	private readonly SheetsService Service;
	private readonly Dictionary<string, Spreadsheet> SpreadsheetCache = [];

	private GoogleSheetsService(DriveService driveService, SheetsService service)
	{
		DriveService = driveService;
		Service = service;
	}

	public void Dispose()
	{
		Service?.Dispose();
		DriveService?.Dispose();
		GC.SuppressFinalize(this);
	}

	public static async Task<GoogleSheetsService> CreateAsync(CancellationToken ct = default)
	{
		BaseClientService.Initializer initializer = await GoogleAuth.GetInitializerAsync(ct: ct);
		DriveService driveService = new(initializer: initializer);
		SheetsService sheetsService = new(initializer: initializer);

		return new GoogleSheetsService(driveService: driveService, service: sheetsService);
	}

	public static string GetSpreadsheetUrl(string spreadsheetId) =>
		$"https://docs.google.com/spreadsheets/d/{spreadsheetId}";

	private async Task<Spreadsheet> GetSpreadsheetMetadataAsync(
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
				SpreadsheetsResource.GetRequest request = Service.Spreadsheets.Get(
					spreadsheetId: spreadsheetId
				);
				request.Fields =
					"spreadsheetId,properties/title,sheets(properties(sheetId,title,index,gridProperties))";
				return await request.ExecuteAsync(cancellationToken: ct);
			},
			ct: ct
		);

		SpreadsheetCache[key: spreadsheetId] = spreadsheet;
		return spreadsheet;
	}

	private void InvalidateCache(string spreadsheetId) =>
		SpreadsheetCache.Remove(key: spreadsheetId);

	private async Task<Sheet?> FindSheetAsync(
		string spreadsheetId,
		string sheetName,
		bool forceRefresh = false,
		CancellationToken ct = default
	)
	{
		Spreadsheet spreadsheet = await GetSpreadsheetMetadataAsync(
			spreadsheetId: spreadsheetId,
			forceRefresh: forceRefresh,
			ct: ct
		);
		return spreadsheet.Sheets?.FirstOrDefault(s =>
			s.Properties?.Title.EqualsIgnoreCase(sheetName, Ordinal) ?? false
		);
	}

	internal async Task<string> CreateSpreadsheetAsync(
		string title = SpreadsheetTitle,
		CancellationToken ct = default
	)
	{
		Log.Debug(messageTemplate: "CreateSpreadsheetAsync entry {Title}", title);
		Spreadsheet response = await Resilience.ExecuteAsync(
			operation: "Sheets.Create",
			async () =>
			{
				Spreadsheet spreadsheet = new()
				{
					Properties = new SpreadsheetProperties { Title = title },
				};
				return await Service
					.Spreadsheets.Create(body: spreadsheet)
					.ExecuteAsync(cancellationToken: ct);
			},
			ct: ct
		);
		var spreadsheetId =
			response?.SpreadsheetId
			?? throw new InvalidOperationException(message: "Failed to create spreadsheet");
		Log.Debug(messageTemplate: "CreateSpreadsheetAsync exit {SpreadsheetId}", spreadsheetId);
		return spreadsheetId;
	}

	internal async Task DeleteSpreadsheetAsync(string spreadsheetId, CancellationToken ct = default)
	{
		Log.Debug(messageTemplate: "DeleteSpreadsheetAsync entry {SpreadsheetId}", spreadsheetId);
		Log.Information(messageTemplate: "Deleting spreadsheet: {0}", spreadsheetId);
		await Resilience.ExecuteAsync(
			operation: "Drive.Delete",
			async () =>
				await DriveService
					.Files.Delete(fileId: spreadsheetId)
					.ExecuteAsync(cancellationToken: ct),
			ct: ct
		);
		Log.Information(messageTemplate: "Spreadsheet deleted");
		Log.Debug(messageTemplate: "DeleteSpreadsheetAsync exit");
	}

	internal async Task<bool> SpreadsheetExistsAsync(
		string spreadsheetId,
		CancellationToken ct = default
	)
	{
		try
		{
			await Resilience.ExecuteAsync(
				operation: "Sheets.Get",
				async () =>
					await Service
						.Spreadsheets.Get(spreadsheetId: spreadsheetId)
						.ExecuteAsync(cancellationToken: ct),
				ct: ct
			);
			return true;
		}
		catch (GoogleApiException)
		{
			return false;
		}
		catch (HttpRequestException)
		{
			return false;
		}
	}

	internal async Task EnsureSubsheetExistsAsync(
		string spreadsheetId,
		string sheetName,
		IEnumerable<object> headers,
		CancellationToken ct = default
	)
	{
		Log.Debug(
			messageTemplate: "EnsureSubsheetExistsAsync entry {SpreadsheetId} {SheetName}",
			spreadsheetId,
			sheetName
		);
		Sheet? existingSheet = await FindSheetAsync(
			spreadsheetId: spreadsheetId,
			sheetName: sheetName,
			ct: ct
		);

		if (existingSheet is null)
		{
			var targetIndex = await GetAlphabeticalInsertIndexAsync(
				spreadsheetId: spreadsheetId,
				newSheetName: sheetName,
				ct: ct
			);

			BatchUpdateSpreadsheetRequest request = new()
			{
				Requests =
				[
					new Request
					{
						AddSheet = new AddSheetRequest
						{
							Properties = new SheetProperties
							{
								Title = sheetName,
								Index = targetIndex,
							},
						},
					},
				],
			};
			await Resilience.ExecuteAsync(
				operation: "Sheets.BatchUpdate.AddSheet",
				async () =>
					await Service
						.Spreadsheets.BatchUpdate(body: request, spreadsheetId: spreadsheetId)
						.ExecuteAsync(cancellationToken: ct),
				ct: ct
			);
			InvalidateCache(spreadsheetId: spreadsheetId);
		}

		await EnsureHeadersForSheetAsync(
			spreadsheetId: spreadsheetId,
			sheetName: sheetName,
			headers: headers,
			ct: ct
		);
		Log.Debug(messageTemplate: "EnsureSubsheetExistsAsync exit");
	}

	private async Task<int> GetAlphabeticalInsertIndexAsync(
		string spreadsheetId,
		string newSheetName,
		CancellationToken ct = default
	)
	{
		List<string> existingNames = await GetSubsheetNamesAsync(
			spreadsheetId: spreadsheetId,
			ct: ct
		);
		existingNames.Add(item: newSheetName);
		existingNames.Sort(comparer: StringComparer.OrdinalIgnoreCase);
		return existingNames.IndexOf(item: newSheetName);
	}

	internal async Task ReorderSheetsAlphabeticallyAsync(
		string spreadsheetId,
		CancellationToken ct = default
	)
	{
		Spreadsheet spreadsheet = await GetSpreadsheetMetadataAsync(
			spreadsheetId: spreadsheetId,
			forceRefresh: true,
			ct: ct
		);
		List<Sheet> sheets =
			spreadsheet
				.Sheets?.Where(s => s.Properties?.Title is { } && s.Properties?.SheetId is { })
				.ToList()
			?? [];

		if (sheets.Count <= 1)
			return;

		var sortedSheets = Enumerable.ToList(
			Enumerable.OrderBy(
				sheets,
				s => s.Properties?.Title,
				comparer: StringComparer.OrdinalIgnoreCase
			)
		);

		var needsReorder = false;
		for (var i = 0; i < sheets.Count; i++)
		{
			if (sheets[index: i].Properties?.SheetId != sortedSheets[index: i].Properties?.SheetId)
			{
				needsReorder = true;
				break;
			}
		}

		if (!needsReorder)
			return;

		Log.Information(messageTemplate: "Reordering {0} sheets alphabetically...", sheets.Count);

		List<Request> requests = [];
		for (var i = 0; i < sortedSheets.Count; i++)
		{
			requests.Add(
				new Request
				{
					UpdateSheetProperties = new UpdateSheetPropertiesRequest
					{
						Properties = new SheetProperties
						{
							SheetId = sortedSheets[index: i].Properties?.SheetId,
							Index = i,
						},
						Fields = "index",
					},
				}
			);
		}

		BatchUpdateSpreadsheetRequest batchRequest = new() { Requests = requests };
		await Resilience.ExecuteAsync(
			operation: "Sheets.BatchUpdate.ReorderSheets",
			async () =>
				await Service
					.Spreadsheets.BatchUpdate(body: batchRequest, spreadsheetId: spreadsheetId)
					.ExecuteAsync(cancellationToken: ct),
			ct: ct
		);
		InvalidateCache(spreadsheetId: spreadsheetId);
		Log.Information(messageTemplate: "Sheets reordered alphabetically");
	}

	internal async Task DeleteSubsheetAsync(
		string spreadsheetId,
		string sheetName,
		CancellationToken ct = default
	)
	{
		Sheet? sheet = await FindSheetAsync(
			spreadsheetId: spreadsheetId,
			sheetName: sheetName,
			ct: ct
		);
		if (sheet?.Properties?.SheetId is null)
			return;

		BatchUpdateSpreadsheetRequest request = new()
		{
			Requests =
			[
				new Request
				{
					DeleteSheet = new DeleteSheetRequest { SheetId = sheet.Properties.SheetId },
				},
			],
		};
		await Resilience.ExecuteAsync(
			operation: "Sheets.BatchUpdate.DeleteSheet",
			async () =>
				await Service
					.Spreadsheets.BatchUpdate(body: request, spreadsheetId: spreadsheetId)
					.ExecuteAsync(cancellationToken: ct),
			ct: ct
		);
		InvalidateCache(spreadsheetId: spreadsheetId);
	}

	internal async Task<List<string>> GetSubsheetNamesAsync(
		string spreadsheetId,
		CancellationToken ct = default
	)
	{
		Spreadsheet spreadsheet = await GetSpreadsheetMetadataAsync(
			spreadsheetId: spreadsheetId,
			ct: ct
		);
		return spreadsheet
				.Sheets?.Select(s => s.Properties?.Title ?? "")
				.Where(t => !IsNullOrEmpty(value: t))
				.ToList()
			?? [];
	}

	internal async Task ClearSubsheetAsync(
		string spreadsheetId,
		string sheetName,
		CancellationToken ct = default
	)
	{
		var escapedName = SheetNameHelper.EscapeForFormula(name: sheetName);
		var range = $"{escapedName}!A2:Z";
		await Resilience.ExecuteAsync(
			operation: "Sheets.Values.Clear",
			async () =>
				await Service
					.Spreadsheets.Values.Clear(
						new ClearValuesRequest(),
						spreadsheetId: spreadsheetId,
						range: range
					)
					.ExecuteAsync(cancellationToken: ct),
			ct: ct
		);
	}

	internal async Task WriteRowsAsync(
		string spreadsheetId,
		string sheetName,
		IList<IList<object>> rows,
		CancellationToken ct = default
	)
	{
		Log.Debug(
			messageTemplate: "WriteRowsAsync entry {SpreadsheetId} {SheetName} {RowCount}",
			spreadsheetId,
			sheetName,
			rows.Count
		);
		if (rows.Count == 0)
		{
			Log.Debug(messageTemplate: "WriteRowsAsync exit (no rows)");
			return;
		}

		var escapedName = SheetNameHelper.EscapeForFormula(name: sheetName);
		ValueRange body = new() { Values = rows };
		var range = $"{escapedName}!A2";

		await Resilience.ExecuteAsync(
			operation: "Sheets.Values.Update",
			async () =>
			{
				SpreadsheetsResource.ValuesResource.UpdateRequest updateRequest =
					Service.Spreadsheets.Values.Update(
						body: body,
						spreadsheetId: spreadsheetId,
						range: range
					);
				updateRequest.ValueInputOption = SpreadsheetsResource
					.ValuesResource
					.UpdateRequest
					.ValueInputOptionEnum
					.USERENTERED;
				return await updateRequest.ExecuteAsync(cancellationToken: ct);
			},
			ct: ct
		);
	}

	internal async Task WriteRecordsAsync<T>(
		string spreadsheetId,
		string sheetName,
		IReadOnlyList<object> headers,
		IEnumerable<T> records,
		Func<T, IList<object>> rowMapper,
		CancellationToken ct = default
	)
	{
		await ClearSubsheetAsync(spreadsheetId: spreadsheetId, sheetName: sheetName, ct: ct);

		List<IList<object>> allRows =
		[
			[.. headers],
			.. Enumerable.Select(records, selector: rowMapper),
		];

		if (allRows.Count > 0)
			await WriteRowsAsync(
				spreadsheetId: spreadsheetId,
				sheetName: sheetName,
				rows: allRows,
				ct: ct
			);
	}

	internal async Task AppendRecordsAsync<T>(
		string spreadsheetId,
		string sheetName,
		IEnumerable<T> records,
		Func<T, IList<object>> rowMapper,
		CancellationToken ct = default
	)
	{
		List<IList<object>> rows = [.. Enumerable.Select(records, selector: rowMapper)];
		if (rows.Count > 0)
			await AppendRowsAsync(
				spreadsheetId: spreadsheetId,
				sheetName: sheetName,
				rows: rows,
				ct: ct
			);
	}

	internal async Task RenameSubsheetAsync(
		string spreadsheetId,
		string oldName,
		string newName,
		CancellationToken ct = default
	)
	{
		Sheet? sheet = await FindSheetAsync(
			spreadsheetId: spreadsheetId,
			sheetName: oldName,
			ct: ct
		);
		if (sheet?.Properties?.SheetId is null)
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
							SheetId = sheet.Properties.SheetId,
							Title = newName,
						},
						Fields = "title",
					},
				},
			],
		};
		await Resilience.ExecuteAsync(
			operation: "Sheets.BatchUpdate.Rename",
			async () =>
				await Service
					.Spreadsheets.BatchUpdate(body: request, spreadsheetId: spreadsheetId)
					.ExecuteAsync(cancellationToken: ct),
			ct: ct
		);
		InvalidateCache(spreadsheetId: spreadsheetId);
		Log.Debug(messageTemplate: "Renamed sheet '{0}' to '{1}'", oldName, newName);
	}

	internal async Task CleanupDefaultSheetAsync(
		string spreadsheetId,
		CancellationToken ct = default
	)
	{
		Spreadsheet spreadsheet = await GetSpreadsheetMetadataAsync(
			spreadsheetId: spreadsheetId,
			ct: ct
		);

		if (spreadsheet.Sheets?.Count <= 1)
			return;

		Sheet? defaultSheet = spreadsheet.Sheets?.FirstOrDefault(s =>
			s.Properties?.Title.EqualsIgnoreCase("Sheet1", Ordinal) ?? false
		);
		if (defaultSheet?.Properties?.SheetId is null)
			return;

		BatchUpdateSpreadsheetRequest request = new()
		{
			Requests =
			[
				new Request
				{
					DeleteSheet = new DeleteSheetRequest
					{
						SheetId = defaultSheet.Properties.SheetId,
					},
				},
			],
		};
		await Resilience.ExecuteAsync(
			operation: "Sheets.BatchUpdate.DeleteSheet1",
			async () =>
				await Service
					.Spreadsheets.BatchUpdate(body: request, spreadsheetId: spreadsheetId)
					.ExecuteAsync(cancellationToken: ct),
			ct: ct
		);
		InvalidateCache(spreadsheetId: spreadsheetId);
	}

	private async Task EnsureHeadersForSheetAsync(
		string spreadsheetId,
		string sheetName,
		IEnumerable<object> headers,
		CancellationToken ct = default
	)
	{
		var escapedName = SheetNameHelper.EscapeForFormula(name: sheetName);
		var range = $"{escapedName}!1:1";
		ValueRange body = new()
		{
			Values =
			[
				[.. headers],
			],
		};
		await Resilience.ExecuteAsync(
			operation: "Sheets.Values.Update.Headers",
			async () =>
			{
				SpreadsheetsResource.ValuesResource.UpdateRequest updateRequest =
					Service.Spreadsheets.Values.Update(
						body: body,
						spreadsheetId: spreadsheetId,
						range: range
					);
				updateRequest.ValueInputOption = SpreadsheetsResource
					.ValuesResource
					.UpdateRequest
					.ValueInputOptionEnum
					.USERENTERED;
				return await updateRequest.ExecuteAsync(cancellationToken: ct);
			},
			ct: ct
		);
	}

	internal async Task EnsureSheetExistsAsync(string spreadsheetId, CancellationToken ct = default)
	{
		Sheet? sheet = await FindSheetAsync(
			spreadsheetId: spreadsheetId,
			sheetName: SheetName,
			ct: ct
		);

		if (sheet is null)
		{
			Spreadsheet spreadsheet = await GetSpreadsheetMetadataAsync(
				spreadsheetId: spreadsheetId,
				ct: ct
			);
			await RenameDefaultSheetAsync(
				spreadsheetId: spreadsheetId,
				spreadsheet: spreadsheet,
				ct: ct
			);
		}

		await EnsureHeadersAsync(spreadsheetId: spreadsheetId, ct: ct);
	}

	private async Task RenameDefaultSheetAsync(
		string spreadsheetId,
		Spreadsheet spreadsheet,
		CancellationToken ct = default
	)
	{
		Sheet? defaultSheet = spreadsheet.Sheets?.FirstOrDefault();
		if (
			defaultSheet?.Properties?.Title == SheetName
			|| defaultSheet?.Properties?.SheetId is null
		)
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
							SheetId = defaultSheet.Properties.SheetId,
							Title = SheetName,
						},
						Fields = "title",
					},
				},
			],
		};
		await Resilience.ExecuteAsync(
			operation: "Sheets.BatchUpdate.RenameDefault",
			async () =>
				await Service
					.Spreadsheets.BatchUpdate(body: request, spreadsheetId: spreadsheetId)
					.ExecuteAsync(cancellationToken: ct),
			ct: ct
		);
		InvalidateCache(spreadsheetId: spreadsheetId);
	}

	private async Task EnsureHeadersAsync(string spreadsheetId, CancellationToken ct = default)
	{
		var range = $"{SheetName}!1:1";
		IList<IList<object>> existing = (
			await Resilience.ExecuteAsync(
				operation: "Sheets.Values.Get.Headers",
				async () =>
					await Service
						.Spreadsheets.Values.Get(spreadsheetId: spreadsheetId, range: range)
						.ExecuteAsync(cancellationToken: ct),
				ct: ct
			)
		).Values;

		var needsUpdate =
			existing is null
			|| existing.Count == 0
			|| existing[index: 0].Count != Headers.Count
			|| !Enumerable.SequenceEqual(existing[index: 0], second: Headers);

		if (!needsUpdate)
			return;

		ValueRange body = new()
		{
			Values =
			[
				[.. Headers],
			],
		};
		await Resilience.ExecuteAsync(
			operation: "Sheets.Values.Update.Headers",
			async () =>
			{
				SpreadsheetsResource.ValuesResource.UpdateRequest updateRequest =
					Service.Spreadsheets.Values.Update(
						body: body,
						spreadsheetId: spreadsheetId,
						range: range
					);
				updateRequest.ValueInputOption = SpreadsheetsResource
					.ValuesResource
					.UpdateRequest
					.ValueInputOptionEnum
					.USERENTERED;
				return await updateRequest.ExecuteAsync(cancellationToken: ct);
			},
			ct: ct
		);
	}

	internal async Task<DateTime?> GetLatestScrobbleTimeAsync(
		string spreadsheetId,
		CancellationToken ct = default
	)
	{
		try
		{
			var range = $"{SheetName}!A2";
			ValueRange response = await Resilience.ExecuteAsync(
				operation: "Sheets.Values.Get.LatestTime",
				async () =>
				{
					SpreadsheetsResource.ValuesResource.GetRequest request =
						Service.Spreadsheets.Values.Get(spreadsheetId: spreadsheetId, range: range);
					request.ValueRenderOption = SpreadsheetsResource
						.ValuesResource
						.GetRequest
						.ValueRenderOptionEnum
						.UNFORMATTEDVALUE;
					return await request.ExecuteAsync(cancellationToken: ct);
				},
				ct: ct
			);

			if (response.Values is null || response.Values.Count == 0)
				return null;

			IList<object> firstRow = response.Values[index: 0];
			if (firstRow is null || firstRow.Count == 0)
				return null;

			var rawValue = firstRow[index: 0];
			Log.Debug(
				messageTemplate: "Sheet raw value: '{0}' (type: {1})",
				rawValue,
				rawValue?.GetType().Name ?? "null"
			);

			if (rawValue is double or int or long or float or decimal)
			{
				var serialDate = Convert.ToDouble(value: rawValue);
				var parsed = DateTime.FromOADate(d: serialDate);
				Log.Debug(messageTemplate: "Parsed from OADate: {0:yyyy/MM/dd HH:mm:ss}", parsed);
				return parsed;
			}

			var latestTimeStr = rawValue?.ToString()?.TrimStart(trimChar: '\'') ?? "";
			if (
				DateTime.TryParseExact(
					s: latestTimeStr,
					formats: DateFormats,
					provider: null,
					style: DateTimeStyles.None,
					out DateTime parsedStr
				)
			)
			{
				Log.Debug(
					messageTemplate: "Parsed from string: {0:yyyy/MM/dd HH:mm:ss}",
					parsedStr
				);
				return parsedStr;
			}

			return null;
		}
		catch (FormatException)
		{
			return null;
		}
		catch (InvalidOperationException)
		{
			return null;
		}
	}

	internal async Task<bool> SheetExistsAsync(string spreadsheetId, CancellationToken ct = default)
	{
		Spreadsheet spreadsheet = await GetSpreadsheetMetadataAsync(
			spreadsheetId: spreadsheetId,
			ct: ct
		);
		return spreadsheet.Sheets?.Any(s =>
				s.Properties?.Title.EqualsIgnoreCase(SheetName, Ordinal) ?? false
			)
			?? false;
	}

	internal async Task<int> GetScrobbleCountAsync(
		string spreadsheetId,
		CancellationToken ct = default
	)
	{
		var range = $"{SheetName}!A:A";
		ValueRange response = await Resilience.ExecuteAsync(
			operation: "Sheets.Values.Get.RowCount",
			async () =>
			{
				SpreadsheetsResource.ValuesResource.GetRequest request =
					Service.Spreadsheets.Values.Get(spreadsheetId: spreadsheetId, range: range);
				return await request.ExecuteAsync(cancellationToken: ct);
			},
			ct: ct
		);

		if (response.Values is null || response.Values.Count <= 1)
			return 0;

		return response.Values.Count - 1;
	}

	internal async Task<int> DeleteScrobblesOnOrAfterAsync(
		string spreadsheetId,
		DateTime fromDate,
		CancellationToken ct = default
	)
	{
		Log.Debug(
			messageTemplate: "DeleteScrobblesOnOrAfterAsync entry {SpreadsheetId} {FromDate:yyyy/MM/dd}",
			spreadsheetId,
			fromDate
		);
		if (!await SheetExistsAsync(spreadsheetId: spreadsheetId, ct: ct))
		{
			Log.Debug(
				messageTemplate: "DeleteScrobblesOnOrAfterAsync exit 0 (sheet does not exist)"
			);
			return 0;
		}

		var range = $"{SheetName}!A2:A";
		ValueRange response = await Resilience.ExecuteAsync(
			operation: "Sheets.Values.Get.AllDates",
			async () =>
			{
				SpreadsheetsResource.ValuesResource.GetRequest request =
					Service.Spreadsheets.Values.Get(spreadsheetId: spreadsheetId, range: range);
				request.ValueRenderOption = SpreadsheetsResource
					.ValuesResource
					.GetRequest
					.ValueRenderOptionEnum
					.UNFORMATTEDVALUE;
				return await request.ExecuteAsync(cancellationToken: ct);
			},
			ct: ct
		);

		if (response.Values is null || response.Values.Count == 0)
			return 0;

		var rowsToDelete = 0;
		foreach (IList<object>? row in response.Values)
		{
			if (row is null || row.Count == 0)
				break;

			DateTime? rowDate = null;
			var rawValue = row[index: 0];

			if (rawValue is double or int or long or float or decimal)
				rowDate = DateTime.FromOADate(Convert.ToDouble(value: rawValue));
			else
			{
				var dateStr = rawValue?.ToString()?.TrimStart(trimChar: '\'') ?? "";
				if (
					DateTime.TryParseExact(
						s: dateStr,
						format: "yyyy/MM/dd HH:mm:ss",
						provider: null,
						style: DateTimeStyles.None,
						out DateTime parsed
					)
				)
					rowDate = parsed;
			}

			if (rowDate is null || rowDate < fromDate)
				break;

			rowsToDelete++;
		}

		if (rowsToDelete == 0)
		{
			Log.Debug(messageTemplate: "DeleteScrobblesOnOrAfterAsync exit 0 (no rows to delete)");
			return 0;
		}

		var sheetId = await GetSheetIdAsync(spreadsheetId: spreadsheetId, ct: ct);
		BatchUpdateSpreadsheetRequest deleteRequest = new()
		{
			Requests =
			[
				new Request
				{
					DeleteDimension = new DeleteDimensionRequest
					{
						Range = new DimensionRange
						{
							SheetId = sheetId,
							Dimension = "ROWS",
							StartIndex = 1,
							EndIndex = 1 + rowsToDelete,
						},
					},
				},
			],
		};

		await Resilience.ExecuteAsync(
			operation: "Sheets.BatchUpdate.DeleteRows",
			async () =>
				await Service
					.Spreadsheets.BatchUpdate(body: deleteRequest, spreadsheetId: spreadsheetId)
					.ExecuteAsync(cancellationToken: ct),
			ct: ct
		);

		Log.Information(messageTemplate: "Deleted {0} scrobbles", rowsToDelete);
		Log.Debug(messageTemplate: "DeleteScrobblesOnOrAfterAsync exit {Count}", rowsToDelete);
		return rowsToDelete;
	}

	internal async Task<List<Scrobble>> GetNewScrobblesAsync(
		string spreadsheetId,
		List<Scrobble> allScrobbles,
		CancellationToken ct = default
	)
	{
		if (!await SheetExistsAsync(spreadsheetId: spreadsheetId, ct: ct))
		{
			Log.Debug(messageTemplate: "Sheet does not exist, returning all scrobbles");
			await EnsureSheetExistsAsync(spreadsheetId: spreadsheetId, ct: ct);
			return allScrobbles;
		}

		DateTime? latestInSheet = await GetLatestScrobbleTimeAsync(
			spreadsheetId: spreadsheetId,
			ct: ct
		);
		if (latestInSheet is null)
			return allScrobbles;

		return [.. Enumerable.Where(allScrobbles, s => s.PlayedAt > latestInSheet)];
	}

	internal async Task WriteScrobblesAsync(
		string spreadsheetId,
		List<Scrobble> scrobbles,
		CancellationToken ct = default
	)
	{
		Log.Debug(
			messageTemplate: "WriteScrobblesAsync entry {SpreadsheetId} {Count}",
			spreadsheetId,
			scrobbles.Count
		);
		var records = Enumerable.ToList(
			Enumerable.Select(
				scrobbles,
				s => (IList<object>)[s.FormattedDate, s.TrackName, s.ArtistName, s.AlbumName]
			)
		);

		await InsertRowsAsync(spreadsheetId: spreadsheetId, records: records, ct: ct);
		Log.Debug(messageTemplate: "WriteScrobblesAsync exit");
	}

	private async Task InsertRowsAsync(
		string spreadsheetId,
		List<IList<object>> records,
		CancellationToken ct = default
	)
	{
		Log.Debug(
			messageTemplate: "InsertRowsAsync entry {SpreadsheetId} {Count}",
			spreadsheetId,
			records.Count
		);
		var sheetId = await GetSheetIdAsync(spreadsheetId: spreadsheetId, ct: ct);

		List<RowData> rowDataList = [];
		foreach (IList<object> record in records)
		{
			List<CellData> cells = [];
			foreach (var value in record)
			{
				cells.Add(
					new CellData
					{
						UserEnteredValue = new ExtendedValue
						{
							StringValue = value?.ToString() ?? "",
						},
					}
				);
			}
			rowDataList.Add(new RowData { Values = cells });
		}

		BatchUpdateSpreadsheetRequest batchRequest = new()
		{
			Requests =
			[
				new Request
				{
					InsertDimension = new InsertDimensionRequest
					{
						Range = new DimensionRange
						{
							SheetId = sheetId,
							Dimension = "ROWS",
							StartIndex = 1,
							EndIndex = 1 + records.Count,
						},
					},
				},
				new Request
				{
					UpdateCells = new UpdateCellsRequest
					{
						Rows = rowDataList,
						Start = new GridCoordinate
						{
							SheetId = sheetId,
							RowIndex = 1,
							ColumnIndex = 0,
						},
						Fields = "userEnteredValue",
					},
				},
			],
		};

		await Resilience.ExecuteAsync(
			operation: "Sheets.BatchUpdate.InsertAndUpdateRows",
			async () =>
				await Service
					.Spreadsheets.BatchUpdate(body: batchRequest, spreadsheetId: spreadsheetId)
					.ExecuteAsync(cancellationToken: ct),
			ct: ct
		);
		Log.Debug(messageTemplate: "InsertRowsAsync exit");
	}

	private async Task<int> GetSheetIdAsync(string spreadsheetId, CancellationToken ct = default)
	{
		Spreadsheet spreadsheet = await GetSpreadsheetMetadataAsync(
			spreadsheetId: spreadsheetId,
			ct: ct
		);
		Sheet? sheet = spreadsheet.Sheets?.FirstOrDefault(s => s.Properties.Title == SheetName);
		return sheet?.Properties?.SheetId
			?? throw new InvalidOperationException($"Sheet '{SheetName}' not found.");
	}

	internal async Task SortSubsheetByColumnAsync(
		string spreadsheetId,
		string sheetName,
		int columnIndex,
		bool ascending = true,
		CancellationToken ct = default
	)
	{
		Sheet? sheet = await FindSheetAsync(
			spreadsheetId: spreadsheetId,
			sheetName: sheetName,
			ct: ct
		);
		if (sheet?.Properties?.SheetId is null)
		{
			Log.Warning(messageTemplate: "Sheet '{0}' not found for sorting", sheetName);
			return;
		}

		var rowCount = sheet.Properties.GridProperties?.RowCount ?? 1000;

		BatchUpdateSpreadsheetRequest request = new()
		{
			Requests =
			[
				new Request
				{
					SortRange = new SortRangeRequest
					{
						Range = new GridRange
						{
							SheetId = sheet.Properties.SheetId,
							StartRowIndex = 1,
							EndRowIndex = rowCount,
							StartColumnIndex = 0,
							EndColumnIndex = 5,
						},
						SortSpecs =
						[
							new SortSpec
							{
								DimensionIndex = columnIndex,
								SortOrder = ascending ? "ASCENDING" : "DESCENDING",
							},
						],
					},
				},
			],
		};

		await Resilience.ExecuteAsync(
			operation: "Sheets.BatchUpdate.Sort",
			async () =>
				await Service
					.Spreadsheets.BatchUpdate(body: request, spreadsheetId: spreadsheetId)
					.ExecuteAsync(cancellationToken: ct),
			ct: ct
		);
		Log.Debug(messageTemplate: "Sorted sheet '{0}' by column {1}", sheetName, columnIndex);
	}

	internal async Task DeleteRowsFromSubsheetAsync(
		string spreadsheetId,
		string sheetName,
		List<int> rowIndices,
		CancellationToken ct = default
	)
	{
		if (rowIndices.Count == 0)
			return;

		Sheet? sheet = await FindSheetAsync(
			spreadsheetId: spreadsheetId,
			sheetName: sheetName,
			ct: ct
		);
		if (sheet?.Properties?.SheetId is null)
		{
			Log.Warning(messageTemplate: "Sheet '{0}' not found for row deletion", sheetName);
			return;
		}

		var sortedIndices = Enumerable.ToList(Enumerable.OrderByDescending(rowIndices, i => i));
		List<(int Start, int End)> ranges = [];

		var rangeStart = sortedIndices[index: 0];
		var rangeEnd = sortedIndices[index: 0];

		for (var i = 1; i < sortedIndices.Count; i++)
		{
			if (sortedIndices[index: i] == rangeEnd - 1)
				rangeEnd = sortedIndices[index: i];
			else
			{
				ranges.Add((rangeEnd, rangeStart));
				rangeStart = sortedIndices[index: i];
				rangeEnd = sortedIndices[index: i];
			}
		}
		ranges.Add((rangeEnd, rangeStart));

		List<Request> requests = [];
		foreach ((var start, var end) in ranges)
		{
			requests.Add(
				new Request
				{
					DeleteDimension = new DeleteDimensionRequest
					{
						Range = new DimensionRange
						{
							SheetId = sheet.Properties.SheetId,
							Dimension = "ROWS",
							StartIndex = start - 1,
							EndIndex = end,
						},
					},
				}
			);
		}

		BatchUpdateSpreadsheetRequest batchRequest = new() { Requests = requests };
		await Resilience.ExecuteAsync(
			operation: "Sheets.BatchUpdate.DeleteRows",
			async () =>
				await Service
					.Spreadsheets.BatchUpdate(body: batchRequest, spreadsheetId: spreadsheetId)
					.ExecuteAsync(cancellationToken: ct),
			ct: ct
		);
		InvalidateCache(spreadsheetId: spreadsheetId);
		Log.Debug(
			messageTemplate: "Deleted {0} rows ({1} ranges) from sheet '{2}'",
			rowIndices.Count,
			ranges.Count,
			sheetName
		);
	}

	internal async Task AppendRowsAsync(
		string spreadsheetId,
		string sheetName,
		IList<IList<object>> rows,
		CancellationToken ct = default
	)
	{
		Log.Debug(
			messageTemplate: "AppendRowsAsync entry {SpreadsheetId} {SheetName} {Count}",
			spreadsheetId,
			sheetName,
			rows.Count
		);
		if (rows.Count == 0)
		{
			Log.Debug(messageTemplate: "AppendRowsAsync exit (no rows)");
			return;
		}

		var escapedName = SheetNameHelper.EscapeForFormula(name: sheetName);
		ValueRange body = new() { Values = rows };
		var range = $"{escapedName}!A:E";

		await Resilience.ExecuteAsync(
			operation: "Sheets.Values.Append",
			async () =>
			{
				SpreadsheetsResource.ValuesResource.AppendRequest appendRequest =
					Service.Spreadsheets.Values.Append(
						body: body,
						spreadsheetId: spreadsheetId,
						range: range
					);
				appendRequest.ValueInputOption = SpreadsheetsResource
					.ValuesResource
					.AppendRequest
					.ValueInputOptionEnum
					.USERENTERED;
				appendRequest.InsertDataOption = SpreadsheetsResource
					.ValuesResource
					.AppendRequest
					.InsertDataOptionEnum
					.INSERTROWS;
				return await appendRequest.ExecuteAsync(cancellationToken: ct);
			},
			ct: ct
		);

		Log.Debug(messageTemplate: "Appended {0} rows to sheet '{1}'", rows.Count, sheetName);
	}

	internal async Task<int> ExportEachSheetAsCSVAsync(
		string spreadsheetId,
		string outputDirectory,
		CancellationToken ct = default
	)
	{
		Log.Debug(
			messageTemplate: "ExportEachSheetAsCSVAsync entry {SpreadsheetId} {OutputDirectory}",
			spreadsheetId,
			outputDirectory
		);
		Directory.CreateDirectory(path: outputDirectory);

		Log.Information(messageTemplate: "Fetching spreadsheet metadata...");

		Spreadsheet spreadsheet = await Resilience.ExecuteAsync(
			operation: "Sheets.Get",
			async () =>
				await Service
					.Spreadsheets.Get(spreadsheetId: spreadsheetId)
					.ExecuteAsync(cancellationToken: ct),
			ct: ct
		);

		List<Sheet> sheets = spreadsheet.Sheets?.ToList() ?? [];
		if (sheets.Count == 0)
		{
			Log.Warning(messageTemplate: "No sheets found");
			return 0;
		}

		var existingFiles = Enumerable.ToHashSet(
			Enumerable.Select(
				Directory.GetFiles(path: outputDirectory, searchPattern: "*.csv"),
				f => Path.GetFileNameWithoutExtension(path: f)
			),
			comparer: StringComparer.OrdinalIgnoreCase
		);

		var toExport = Enumerable.ToList(
			Enumerable.Where(
				sheets,
				s => !existingFiles.Contains(SheetNameHelper.Sanitize(s.Properties?.Title ?? ""))
			)
		);

		if (toExport.Count == 0)
		{
			Log.Information(messageTemplate: "All {0} sheets already exported", sheets.Count);
			return sheets.Count;
		}

		var totalSheets = sheets.Count;
		var alreadyExported = totalSheets - toExport.Count;
		Log.Information(
			messageTemplate: "Exporting {0} sheets ({1} already done)...",
			toExport.Count,
			alreadyExported
		);

		foreach (Sheet sheet in toExport)
		{
			if (ct.IsCancellationRequested)
				break;

			var sheetTitle = sheet.Properties?.Title ?? "";
			var sheetId = sheet.Properties!.SheetId;
			var safeFileName = SheetNameHelper.Sanitize(name: sheetTitle);
			var outputPath = Path.Combine(path1: outputDirectory, $"{safeFileName}.csv");

			Log.Debug(messageTemplate: "Exporting: {0}", sheetTitle);

			var exportUrl =
				$"https://docs.google.com/spreadsheets/d/{spreadsheetId}/export?format=csv&gid={sheetId}";

			var response = await Resilience.ExecuteAsync(
				operation: "Sheets.ExportCSV",
				async () =>
					await Service.HttpClient.GetByteArrayAsync(
						new Uri(uriString: exportUrl),
						cancellationToken: ct
					),
				ct: ct
			);

			await File.WriteAllBytesAsync(path: outputPath, bytes: response, cancellationToken: ct);
			Log.Debug(messageTemplate: "Exported: {0}", sheetTitle);
		}

		Log.Information(messageTemplate: "Exported {0} sheets", toExport.Count);
		Log.Debug(messageTemplate: "ExportEachSheetAsCSVAsync exit {Count}", sheets.Count);
		return sheets.Count;
	}

	internal async Task<List<(string Id, string Url)>> FindDuplicateSpreadsheetsAsync(
		string title,
		CancellationToken ct = default
	)
	{
		var query =
			$"name = '{title.Replace(oldValue: "'", newValue: "\\'")}' and mimeType = 'application/vnd.google-apps.spreadsheet' and trashed = false";

		FilesResource.ListRequest request = DriveService.Files.List();
		request.Q = query;
		request.Fields = "files(id, name, webViewLink)";

		FileList response = await Resilience.ExecuteAsync(
			operation: "Drive.Files.List",
			async () => await request.ExecuteAsync(cancellationToken: ct),
			ct: ct
		);

		return response
				.Files?.Select(f => (f.Id, f.WebViewLink ?? GetSpreadsheetUrl(spreadsheetId: f.Id)))
				.ToList()
			?? [];
	}
}
