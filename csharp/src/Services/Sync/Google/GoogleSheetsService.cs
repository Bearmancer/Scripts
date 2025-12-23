namespace CSharpScripts.Services.Sync.Google;

#region GoogleSheetsService

public class GoogleSheetsService : IDisposable
{
    private const string SheetName = "Scrobbles";
    private const string SpreadsheetTitle = "last.fm scrobbles";

    private static readonly FrozenSet<object> Headers = FrozenSet.ToFrozenSet<object>([
        "Date",
        "Track Title",
        "Artist",
        "Album",
    ]);

    private readonly DriveService driveService = new(Config.GoogleInitializer);

    private readonly SheetsService service = new(Config.GoogleInitializer);

    private readonly Dictionary<string, Spreadsheet> spreadsheetCache = [];

    public void Dispose()
    {
        service?.Dispose();
        driveService?.Dispose();
        GC.SuppressFinalize(this);
    }

    public static string GetSpreadsheetUrl(string spreadsheetId) =>
        $"https://docs.google.com/spreadsheets/d/{spreadsheetId}";

    private Spreadsheet GetSpreadsheetMetadata(string spreadsheetId, bool forceRefresh = false)
    {
        if (!forceRefresh && spreadsheetCache.TryGetValue(key: spreadsheetId, out var cached))
            return cached;

        var spreadsheet = Resilience.Execute(
            operation: "Sheets.Get",
            () =>
            {
                var request = service.Spreadsheets.Get(spreadsheetId: spreadsheetId);
                request.Fields =
                    "spreadsheetId,properties/title,sheets(properties(sheetId,title,index,gridProperties))";
                return request.Execute();
            }
        );

        spreadsheetCache[key: spreadsheetId] = spreadsheet;
        return spreadsheet;
    }

    private void InvalidateCache(string spreadsheetId) =>
        spreadsheetCache.Remove(key: spreadsheetId);

    private Sheet? FindSheet(string spreadsheetId, string sheetName, bool forceRefresh = false)
    {
        var spreadsheet = GetSpreadsheetMetadata(
            spreadsheetId: spreadsheetId,
            forceRefresh: forceRefresh
        );
        return spreadsheet.Sheets?.FirstOrDefault(s =>
            s.Properties?.Title?.Equals(
                value: sheetName,
                comparisonType: StringComparison.OrdinalIgnoreCase
            ) == true
        );
    }

    internal string CreateSpreadsheet(string title = SpreadsheetTitle)
    {
        var response = Resilience.Execute(
            operation: "Sheets.Create",
            () =>
            {
                Spreadsheet spreadsheet = new()
                {
                    Properties = new SpreadsheetProperties { Title = title },
                };
                return service.Spreadsheets.Create(body: spreadsheet).Execute();
            }
        );
        return response?.SpreadsheetId
            ?? throw new InvalidOperationException(message: "Failed to create spreadsheet");
    }

    internal void DeleteSpreadsheet(string spreadsheetId)
    {
        Console.Info(message: "Deleting spreadsheet: {0}", spreadsheetId);
        Resilience.Execute(
            operation: "Drive.Delete",
            () => driveService.Files.Delete(fileId: spreadsheetId).Execute()
        );
        Console.Success(message: "Spreadsheet deleted");
    }

    internal bool SpreadsheetExists(string spreadsheetId)
    {
        try
        {
            Resilience.Execute(
                operation: "Sheets.Get",
                () => service.Spreadsheets.Get(spreadsheetId: spreadsheetId).Execute()
            );
            return true;
        }
        catch
        {
            return false;
        }
    }

    #region Subsheet Management

    internal void EnsureSubsheetExists(
        string spreadsheetId,
        string sheetName,
        IEnumerable<object> headers
    )
    {
        var existingSheet = FindSheet(spreadsheetId: spreadsheetId, sheetName: sheetName);

        if (existingSheet == null)
        {
            int targetIndex = GetAlphabeticalInsertIndex(
                spreadsheetId: spreadsheetId,
                newSheetName: sheetName
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
            Resilience.Execute(
                operation: "Sheets.BatchUpdate.AddSheet",
                () =>
                    service
                        .Spreadsheets.BatchUpdate(body: request, spreadsheetId: spreadsheetId)
                        .Execute()
            );
            InvalidateCache(spreadsheetId: spreadsheetId);
        }

        EnsureHeadersForSheet(spreadsheetId: spreadsheetId, sheetName: sheetName, headers: headers);
    }

    private int GetAlphabeticalInsertIndex(string spreadsheetId, string newSheetName)
    {
        var existingNames = GetSubsheetNames(spreadsheetId: spreadsheetId);
        existingNames.Add(item: newSheetName);
        existingNames.Sort(comparer: StringComparer.OrdinalIgnoreCase);
        return existingNames.IndexOf(item: newSheetName);
    }

    internal void ReorderSheetsAlphabetically(string spreadsheetId)
    {
        var spreadsheet = GetSpreadsheetMetadata(spreadsheetId: spreadsheetId, forceRefresh: true);
        var sheets =
            spreadsheet
                .Sheets?.Where(s => s.Properties?.Title != null && s.Properties?.SheetId != null)
                .ToList()
            ?? [];

        if (sheets.Count <= 1)
            return;

        var sortedSheets = sheets
            .OrderBy(s => s.Properties!.Title, comparer: StringComparer.OrdinalIgnoreCase)
            .ToList();

        var needsReorder = false;
        for (var i = 0; i < sheets.Count; i++)
            if (sheets[index: i].Properties!.SheetId != sortedSheets[index: i].Properties!.SheetId)
            {
                needsReorder = true;
                break;
            }

        if (!needsReorder)
            return;

        Console.Info(message: "Reordering {0} sheets alphabetically...", sheets.Count);

        List<Request> requests = [];
        for (var i = 0; i < sortedSheets.Count; i++)
            requests.Add(
                new Request
                {
                    UpdateSheetProperties = new UpdateSheetPropertiesRequest
                    {
                        Properties = new SheetProperties
                        {
                            SheetId = sortedSheets[index: i].Properties!.SheetId,
                            Index = i,
                        },
                        Fields = "index",
                    },
                }
            );

        BatchUpdateSpreadsheetRequest batchRequest = new() { Requests = requests };
        Resilience.Execute(
            operation: "Sheets.BatchUpdate.ReorderSheets",
            () =>
                service
                    .Spreadsheets.BatchUpdate(body: batchRequest, spreadsheetId: spreadsheetId)
                    .Execute()
        );
        InvalidateCache(spreadsheetId: spreadsheetId);
        Console.Success(message: "Sheets reordered alphabetically");
    }

    internal void DeleteSubsheet(string spreadsheetId, string sheetName)
    {
        var sheet = FindSheet(spreadsheetId: spreadsheetId, sheetName: sheetName);
        if (sheet?.Properties?.SheetId == null)
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
        Resilience.Execute(
            operation: "Sheets.BatchUpdate.DeleteSheet",
            () =>
                service
                    .Spreadsheets.BatchUpdate(body: request, spreadsheetId: spreadsheetId)
                    .Execute()
        );
        InvalidateCache(spreadsheetId: spreadsheetId);
    }

    internal List<string> GetSubsheetNames(string spreadsheetId)
    {
        var spreadsheet = GetSpreadsheetMetadata(spreadsheetId: spreadsheetId);
        return spreadsheet
                .Sheets?.Select(s => s.Properties?.Title ?? "")
                .Where(t => !IsNullOrEmpty(value: t))
                .ToList()
            ?? [];
    }

    internal void ClearSubsheet(string spreadsheetId, string sheetName)
    {
        string escapedName = EscapeSheetName(name: sheetName);
        var range = $"{escapedName}!A2:Z";
        Resilience.Execute(
            operation: "Sheets.Values.Clear",
            () =>
                service
                    .Spreadsheets.Values.Clear(
                        new ClearValuesRequest(),
                        spreadsheetId: spreadsheetId,
                        range: range
                    )
                    .Execute()
        );
    }

    internal void WriteRows(string spreadsheetId, string sheetName, IList<IList<object>> rows)
    {
        if (rows.Count == 0)
            return;

        string escapedName = EscapeSheetName(name: sheetName);
        ValueRange body = new() { Values = rows };
        var range = $"{escapedName}!A2";

        Resilience.Execute(
            operation: "Sheets.Values.Update",
            () =>
            {
                var updateRequest = service.Spreadsheets.Values.Update(
                    body: body,
                    spreadsheetId: spreadsheetId,
                    range: range
                );
                updateRequest.ValueInputOption = SpreadsheetsResource
                    .ValuesResource
                    .UpdateRequest
                    .ValueInputOptionEnum
                    .USERENTERED;
                return updateRequest.Execute();
            }
        );
    }

    internal void WriteRecords<T>(
        string spreadsheetId,
        string sheetName,
        IReadOnlyList<object> headers,
        IEnumerable<T> records,
        Func<T, IList<object>> rowMapper
    )
    {
        ClearSubsheet(spreadsheetId: spreadsheetId, sheetName: sheetName);

        List<IList<object>> allRows =
        [
            [.. headers],
            .. records.Select(selector: rowMapper),
        ];

        if (allRows.Count > 0)
            WriteRows(spreadsheetId: spreadsheetId, sheetName: sheetName, rows: allRows);
    }

    internal void AppendRecords<T>(
        string spreadsheetId,
        string sheetName,
        IEnumerable<T> records,
        Func<T, IList<object>> rowMapper
    )
    {
        List<IList<object>> rows = [.. records.Select(selector: rowMapper)];
        if (rows.Count > 0)
            AppendRows(spreadsheetId: spreadsheetId, sheetName: sheetName, rows: rows);
    }

    internal void RenameSubsheet(string spreadsheetId, string oldName, string newName)
    {
        var sheet = FindSheet(spreadsheetId: spreadsheetId, sheetName: oldName);
        if (sheet?.Properties?.SheetId == null)
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
        Resilience.Execute(
            operation: "Sheets.BatchUpdate.Rename",
            () =>
                service
                    .Spreadsheets.BatchUpdate(body: request, spreadsheetId: spreadsheetId)
                    .Execute()
        );
        InvalidateCache(spreadsheetId: spreadsheetId);
        Console.Debug(message: "Renamed sheet '{0}' to '{1}'", oldName, newName);
    }

    internal void CleanupDefaultSheet(string spreadsheetId)
    {
        var spreadsheet = GetSpreadsheetMetadata(spreadsheetId: spreadsheetId);

        if (spreadsheet.Sheets?.Count <= 1)
            return;

        var defaultSheet = spreadsheet.Sheets?.FirstOrDefault(s => s.Properties?.Title == "Sheet1");
        if (defaultSheet?.Properties?.SheetId == null)
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
        Resilience.Execute(
            operation: "Sheets.BatchUpdate.DeleteSheet1",
            () =>
                service
                    .Spreadsheets.BatchUpdate(body: request, spreadsheetId: spreadsheetId)
                    .Execute()
        );
        InvalidateCache(spreadsheetId: spreadsheetId);
    }

    private void EnsureHeadersForSheet(
        string spreadsheetId,
        string sheetName,
        IEnumerable<object> headers
    )
    {
        string escapedName = EscapeSheetName(name: sheetName);
        var range = $"{escapedName}!1:1";
        ValueRange body = new()
        {
            Values =
            [
                [.. headers],
            ],
        };
        Resilience.Execute(
            operation: "Sheets.Values.Update.Headers",
            () =>
            {
                var updateRequest = service.Spreadsheets.Values.Update(
                    body: body,
                    spreadsheetId: spreadsheetId,
                    range: range
                );
                updateRequest.ValueInputOption = SpreadsheetsResource
                    .ValuesResource
                    .UpdateRequest
                    .ValueInputOptionEnum
                    .USERENTERED;
                return updateRequest.Execute();
            }
        );
    }

    private static string EscapeSheetName(string name) =>
        name.Contains(value: '\'') || name.Contains(value: ' ') || name.Contains(value: '-')
            ? $"'{name.Replace(oldValue: "'", newValue: "''")}'"
            : name;

    private static string SanitizeForFileName(string name) =>
        name.Replace(oldValue: ":", newValue: " -")
            .Replace(oldValue: "/", newValue: "-")
            .Replace(oldValue: "\\", newValue: "-")
            .Replace(oldValue: "?", newValue: "")
            .Replace(oldValue: "*", newValue: "")
            .Replace(oldValue: "[", newValue: "(")
            .Replace(oldValue: "]", newValue: ")");

    internal void EnsureSheetExists(string spreadsheetId)
    {
        var sheet = FindSheet(spreadsheetId: spreadsheetId, sheetName: SheetName);

        if (sheet == null)
        {
            var spreadsheet = GetSpreadsheetMetadata(spreadsheetId: spreadsheetId);
            RenameDefaultSheet(spreadsheetId: spreadsheetId, spreadsheet: spreadsheet);
        }

        EnsureHeaders(spreadsheetId: spreadsheetId);
    }

    private void RenameDefaultSheet(string spreadsheetId, Spreadsheet spreadsheet)
    {
        var defaultSheet = spreadsheet.Sheets?.FirstOrDefault();
        if (
            defaultSheet?.Properties?.Title == SheetName
            || defaultSheet?.Properties?.SheetId == null
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
        Resilience.Execute(
            operation: "Sheets.BatchUpdate.RenameDefault",
            () =>
                service
                    .Spreadsheets.BatchUpdate(body: request, spreadsheetId: spreadsheetId)
                    .Execute()
        );
        InvalidateCache(spreadsheetId: spreadsheetId);
    }

    private void EnsureHeaders(string spreadsheetId)
    {
        var range = $"{SheetName}!1:1";
        var existing = Resilience
            .Execute(
                operation: "Sheets.Values.Get.Headers",
                () =>
                    service
                        .Spreadsheets.Values.Get(spreadsheetId: spreadsheetId, range: range)
                        .Execute()
            )
            .Values;

        bool needsUpdate =
            existing == null
            || existing.Count == 0
            || existing[index: 0].Count != Headers.Count
            || !existing[index: 0].SequenceEqual(second: Headers);

        if (!needsUpdate)
            return;

        ValueRange body = new()
        {
            Values =
            [
                [.. Headers],
            ],
        };
        Resilience.Execute(
            operation: "Sheets.Values.Update.Headers",
            () =>
            {
                var updateRequest = service.Spreadsheets.Values.Update(
                    body: body,
                    spreadsheetId: spreadsheetId,
                    range: range
                );
                updateRequest.ValueInputOption = SpreadsheetsResource
                    .ValuesResource
                    .UpdateRequest
                    .ValueInputOptionEnum
                    .USERENTERED;
                return updateRequest.Execute();
            }
        );
    }

    #endregion

    #region Scrobble Operations

    internal DateTime? GetLatestScrobbleTime(string spreadsheetId)
    {
        try
        {
            var range = $"{SheetName}!A2";
            var response = Resilience.Execute(
                operation: "Sheets.Values.Get.LatestTime",
                () =>
                {
                    var request = service.Spreadsheets.Values.Get(
                        spreadsheetId: spreadsheetId,
                        range: range
                    );
                    request.ValueRenderOption = SpreadsheetsResource
                        .ValuesResource
                        .GetRequest
                        .ValueRenderOptionEnum
                        .UNFORMATTEDVALUE;
                    return request.Execute();
                }
            );

            if (response.Values == null || response.Values.Count == 0)
                return null;

            var firstRow = response.Values[index: 0];
            if (firstRow == null || firstRow.Count == 0)
                return null;

            object? rawValue = firstRow[index: 0];
            Console.Debug(
                message: "Sheet raw value: '{0}' (type: {1})",
                rawValue,
                rawValue?.GetType().Name ?? "null"
            );

            if (rawValue is double or int or long or float or decimal)
            {
                var serialDate = Convert.ToDouble(value: rawValue);
                var parsed = DateTime.FromOADate(d: serialDate);
                Console.Debug(message: "Parsed from OADate: {0:yyyy/MM/dd HH:mm:ss}", parsed);
                return parsed;
            }

            string latestTimeStr = rawValue?.ToString()?.TrimStart(trimChar: '\'') ?? "";
            string[] formats =
            [
                "yyyy/MM/dd HH:mm:ss",
                "yyyy/MM/dd HH:mm",
                "yyyy/MM/dd H:mm:ss",
                "yyyy/MM/dd H:mm",
            ];
            if (
                DateTime.TryParseExact(
                    s: latestTimeStr,
                    formats: formats,
                    provider: null,
                    style: DateTimeStyles.None,
                    out var parsedStr
                )
            )
            {
                Console.Debug(message: "Parsed from string: {0:yyyy/MM/dd HH:mm:ss}", parsedStr);
                return parsedStr;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    internal bool SheetExists(string spreadsheetId)
    {
        var spreadsheet = GetSpreadsheetMetadata(spreadsheetId: spreadsheetId);
        return spreadsheet.Sheets?.Any(s =>
                s.Properties?.Title?.Equals(
                    value: SheetName,
                    comparisonType: StringComparison.OrdinalIgnoreCase
                ) == true
            )
            ?? false;
    }

    internal int GetScrobbleCount(string spreadsheetId)
    {
        var range = $"{SheetName}!A:A";
        var response = Resilience.Execute(
            operation: "Sheets.Values.Get.RowCount",
            () =>
            {
                var request = service.Spreadsheets.Values.Get(
                    spreadsheetId: spreadsheetId,
                    range: range
                );
                return request.Execute();
            }
        );

        if (response.Values == null || response.Values.Count <= 1)
            return 0;

        return response.Values.Count - 1;
    }

    internal int DeleteScrobblesOnOrAfter(string spreadsheetId, DateTime fromDate)
    {
        if (!SheetExists(spreadsheetId: spreadsheetId))
            return 0;

        var range = $"{SheetName}!A2:A";
        var response = Resilience.Execute(
            operation: "Sheets.Values.Get.AllDates",
            () =>
            {
                var request = service.Spreadsheets.Values.Get(
                    spreadsheetId: spreadsheetId,
                    range: range
                );
                request.ValueRenderOption = SpreadsheetsResource
                    .ValuesResource
                    .GetRequest
                    .ValueRenderOptionEnum
                    .UNFORMATTEDVALUE;
                return request.Execute();
            }
        );

        if (response.Values == null || response.Values.Count == 0)
            return 0;

        var rowsToDelete = 0;
        foreach (var row in response.Values)
        {
            if (row == null || row.Count == 0)
                break;

            DateTime? rowDate = null;
            object? rawValue = row[index: 0];

            if (rawValue is double or int or long or float or decimal)
            {
                rowDate = DateTime.FromOADate(Convert.ToDouble(value: rawValue));
            }
            else
            {
                string dateStr = rawValue?.ToString()?.TrimStart(trimChar: '\'') ?? "";
                if (
                    DateTime.TryParseExact(
                        s: dateStr,
                        format: "yyyy/MM/dd HH:mm:ss",
                        provider: null,
                        style: DateTimeStyles.None,
                        out var parsed
                    )
                )
                    rowDate = parsed;
            }

            if (rowDate == null || rowDate < fromDate)
                break;

            rowsToDelete++;
        }

        if (rowsToDelete == 0)
            return 0;

        Console.Info(
            message: "Deleting {0} scrobbles from {1} onwards...",
            rowsToDelete,
            fromDate.ToString(format: "yyyy/MM/dd")
        );

        int sheetId = GetSheetId(spreadsheetId: spreadsheetId);
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

        Resilience.Execute(
            operation: "Sheets.BatchUpdate.DeleteRows",
            () =>
                service
                    .Spreadsheets.BatchUpdate(body: deleteRequest, spreadsheetId: spreadsheetId)
                    .Execute()
        );

        Console.Success(message: "Deleted {0} scrobbles", rowsToDelete);
        return rowsToDelete;
    }

    internal List<Scrobble> GetNewScrobbles(string spreadsheetId, List<Scrobble> allScrobbles)
    {
        if (!SheetExists(spreadsheetId: spreadsheetId))
        {
            Console.Debug(message: "Sheet does not exist, returning all scrobbles");
            EnsureSheetExists(spreadsheetId: spreadsheetId);
            return allScrobbles;
        }

        var latestInSheet = GetLatestScrobbleTime(spreadsheetId: spreadsheetId);
        if (latestInSheet == null)
            return allScrobbles;

        return [.. allScrobbles.Where(s => s.PlayedAt > latestInSheet)];
    }

    internal void WriteScrobbles(string spreadsheetId, List<Scrobble> scrobbles)
    {
        var records = scrobbles
            .Select(s =>
                (IList<object>)["'" + s.FormattedDate, s.TrackName, s.ArtistName, s.AlbumName]
            )
            .ToList();

        InsertRows(spreadsheetId: spreadsheetId, records: records);
    }

    private void InsertRows(string spreadsheetId, List<IList<object>> records)
    {
        int sheetId = GetSheetId(spreadsheetId: spreadsheetId);

        List<RowData> rowDataList = [];
        foreach (var record in records)
        {
            List<CellData> cells = [];
            foreach (var value in record)
                cells.Add(
                    new CellData
                    {
                        UserEnteredValue = new ExtendedValue
                        {
                            StringValue = value?.ToString() ?? "",
                        },
                    }
                );
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

        Resilience.Execute(
            operation: "Sheets.BatchUpdate.InsertAndUpdateRows",
            () =>
                service
                    .Spreadsheets.BatchUpdate(body: batchRequest, spreadsheetId: spreadsheetId)
                    .Execute()
        );
    }

    private int GetSheetId(string spreadsheetId)
    {
        var spreadsheet = GetSpreadsheetMetadata(spreadsheetId: spreadsheetId);
        var sheet = spreadsheet.Sheets?.FirstOrDefault(s => s.Properties.Title == SheetName);
        return sheet?.Properties?.SheetId
            ?? throw new InvalidOperationException($"Sheet '{SheetName}' not found.");
    }

    internal void SortSubsheetByColumn(
        string spreadsheetId,
        string sheetName,
        int columnIndex,
        bool ascending = true
    )
    {
        var sheet = FindSheet(spreadsheetId: spreadsheetId, sheetName: sheetName);
        if (sheet?.Properties?.SheetId == null)
        {
            Console.Warning(message: "Sheet '{0}' not found for sorting", sheetName);
            return;
        }

        int rowCount = sheet.Properties.GridProperties?.RowCount ?? 1000;

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

        Resilience.Execute(
            operation: "Sheets.BatchUpdate.Sort",
            () =>
                service
                    .Spreadsheets.BatchUpdate(body: request, spreadsheetId: spreadsheetId)
                    .Execute()
        );
        Console.Debug(message: "Sorted sheet '{0}' by column {1}", sheetName, columnIndex);
    }

    internal void DeleteRowsFromSubsheet(
        string spreadsheetId,
        string sheetName,
        List<int> rowIndices
    )
    {
        if (rowIndices.Count == 0)
            return;

        var sheet = FindSheet(spreadsheetId: spreadsheetId, sheetName: sheetName);
        if (sheet?.Properties?.SheetId == null)
        {
            Console.Warning(message: "Sheet '{0}' not found for row deletion", sheetName);
            return;
        }

        var sortedIndices = rowIndices.OrderByDescending(i => i).ToList();
        List<(int Start, int End)> ranges = [];

        int rangeStart = sortedIndices[index: 0];
        int rangeEnd = sortedIndices[index: 0];

        for (var i = 1; i < sortedIndices.Count; i++)
            if (sortedIndices[index: i] == rangeEnd - 1)
            {
                rangeEnd = sortedIndices[index: i];
            }
            else
            {
                ranges.Add((rangeEnd, rangeStart));
                rangeStart = sortedIndices[index: i];
                rangeEnd = sortedIndices[index: i];
            }
        ranges.Add((rangeEnd, rangeStart));

        List<Request> requests = [];
        foreach ((int start, int end) in ranges)
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

        BatchUpdateSpreadsheetRequest batchRequest = new() { Requests = requests };
        Resilience.Execute(
            operation: "Sheets.BatchUpdate.DeleteRows",
            () =>
                service
                    .Spreadsheets.BatchUpdate(body: batchRequest, spreadsheetId: spreadsheetId)
                    .Execute()
        );
        InvalidateCache(spreadsheetId: spreadsheetId);
        Console.Debug(
            message: "Deleted {0} rows ({1} ranges) from sheet '{2}'",
            rowIndices.Count,
            ranges.Count,
            sheetName
        );
    }

    internal void AppendRows(string spreadsheetId, string sheetName, IList<IList<object>> rows)
    {
        if (rows.Count == 0)
            return;

        string escapedName = EscapeSheetName(name: sheetName);
        ValueRange body = new() { Values = rows };
        var range = $"{escapedName}!A:E";

        Resilience.Execute(
            operation: "Sheets.Values.Append",
            () =>
            {
                var appendRequest = service.Spreadsheets.Values.Append(
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
                return appendRequest.Execute();
            }
        );

        Console.Debug(message: "Appended {0} rows to sheet '{1}'", rows.Count, sheetName);
    }

    internal string GetOrCreateSpreadsheet(
        string? currentSpreadsheetId,
        string? defaultSpreadsheetId,
        string spreadsheetTitle,
        Action<string> onSpreadsheetResolved
    )
    {
        if (
            !IsNullOrEmpty(value: currentSpreadsheetId)
            && SpreadsheetExists(spreadsheetId: currentSpreadsheetId)
        )
            return currentSpreadsheetId;

        if (!IsNullOrEmpty(value: defaultSpreadsheetId))
        {
            if (SpreadsheetExists(spreadsheetId: defaultSpreadsheetId))
            {
                onSpreadsheetResolved(obj: defaultSpreadsheetId);
                return defaultSpreadsheetId;
            }

            Console.Warning(message: "Default spreadsheet not found: {0}", defaultSpreadsheetId);
        }

        Console.Info(message: "Creating spreadsheet: {0}", spreadsheetTitle);
        string newId = CreateSpreadsheet(title: spreadsheetTitle);
        onSpreadsheetResolved(obj: newId);

        Console.Info(message: "Created new spreadsheet: {0}", newId);
        return newId;
    }

    #endregion

    #region Export Operations

    internal int ExportEachSheetAsCSV(
        string spreadsheetId,
        string outputDirectory,
        CancellationToken ct = default
    )
    {
        CreateDirectory(path: outputDirectory);

        Console.Info(message: "Fetching spreadsheet metadata...");

        var spreadsheet = Resilience.Execute(
            operation: "Sheets.Get",
            () => service.Spreadsheets.Get(spreadsheetId: spreadsheetId).Execute(),
            ct: ct
        );

        var sheets = spreadsheet.Sheets?.Where(s => s.Properties?.SheetId != null).ToList() ?? [];
        var existingFiles = GetFiles(path: outputDirectory, searchPattern: "*.csv")
            .Select(selector: GetFileName)
            .ToHashSet();
        var toExport = sheets
            .Where(s => !existingFiles.Contains($"{SanitizeForFileName(s.Properties!.Title!)}.csv"))
            .ToList();

        if (toExport.Count == 0)
            return sheets.Count;

        int totalSheets = sheets.Count;
        int alreadyExported = totalSheets - toExport.Count;
        var exported = 0;

        Console.Suppress = true;

        AnsiConsole
            .Progress()
            .AutoClear(enabled: true)
            .HideCompleted(enabled: false)
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new RemainingTimeColumn(),
                new SpinnerColumn()
            )
            .Start(ctx =>
            {
                var task = ctx.AddTask(
                    $"Exporting sheets ({alreadyExported}/{totalSheets} done)",
                    maxValue: totalSheets
                );
                task.Value = alreadyExported;

                foreach (var sheet in toExport)
                {
                    if (ct.IsCancellationRequested)
                        break;

                    string sheetTitle = sheet.Properties!.Title!;
                    int? sheetId = sheet.Properties.SheetId;
                    string safeFileName = SanitizeForFileName(name: sheetTitle);
                    string outputPath = Combine(path1: outputDirectory, $"{safeFileName}.csv");

                    task.Description = $"Exporting: {sheetTitle}";

                    var exportUrl =
                        $"https://docs.google.com/spreadsheets/d/{spreadsheetId}/export?format=csv&gid={sheetId}";

                    byte[] response = Resilience.Execute(
                        operation: "Sheets.ExportCSV",
                        () => service.HttpClient.GetByteArrayAsync(requestUri: exportUrl).Result,
                        ct: ct
                    );

                    WriteAllBytes(path: outputPath, bytes: response);
                    exported++;
                    task.Increment(value: 1);
                }
            });

        Console.Suppress = false;

        return sheets.Count;
    }

    internal async Task<int> ExportEachSheetAsCSVAsync(
        string spreadsheetId,
        string outputDirectory,
        CancellationToken ct = default
    )
    {
        CreateDirectory(path: outputDirectory);

        Console.Info(message: "Fetching spreadsheet metadata...");

        var spreadsheet = await Resilience.ExecuteAsync(
            operation: "Sheets.Get",
            async () =>
                await service
                    .Spreadsheets.Get(spreadsheetId: spreadsheetId)
                    .ExecuteAsync(cancellationToken: ct),
            ct: ct
        );

        var sheets = spreadsheet.Sheets?.ToList() ?? [];
        if (sheets.Count == 0)
        {
            Console.Warning(message: "No sheets found");
            return 0;
        }

        var existingFiles = GetFiles(path: outputDirectory, searchPattern: "*.csv")
            .Select(f => GetFileNameWithoutExtension(path: f))
            .ToHashSet(comparer: StringComparer.OrdinalIgnoreCase);

        var toExport = sheets
            .Where(s => !existingFiles.Contains(SanitizeForFileName(s.Properties?.Title ?? "")))
            .ToList();

        if (toExport.Count == 0)
        {
            Console.Info(message: "All {0} sheets already exported", sheets.Count);
            return sheets.Count;
        }

        int totalSheets = sheets.Count;
        int alreadyExported = totalSheets - toExport.Count;
        Console.Info(
            message: "Exporting {0} sheets ({1} already done)...",
            toExport.Count,
            alreadyExported
        );

        foreach (var sheet in toExport)
        {
            if (ct.IsCancellationRequested)
                break;

            string sheetTitle = sheet.Properties!.Title!;
            int? sheetId = sheet.Properties.SheetId;
            string safeFileName = SanitizeForFileName(name: sheetTitle);
            string outputPath = Combine(path1: outputDirectory, $"{safeFileName}.csv");

            Console.Dim($"Exporting: {sheetTitle}");

            var exportUrl =
                $"https://docs.google.com/spreadsheets/d/{spreadsheetId}/export?format=csv&gid={sheetId}";

            byte[] response = await Resilience.ExecuteAsync(
                operation: "Sheets.ExportCSV",
                async () =>
                    await service.HttpClient.GetByteArrayAsync(
                        requestUri: exportUrl,
                        cancellationToken: ct
                    ),
                ct: ct
            );

            await WriteAllBytesAsync(path: outputPath, bytes: response, cancellationToken: ct);
        }

        Console.Success(message: "Exported {0} sheets", toExport.Count);
        return sheets.Count;
    }

    internal List<(string Id, string Url)> FindDuplicateSpreadsheets(string title)
    {
        var query =
            $"name = '{title.Replace(oldValue: "'", newValue: "\\'")}' and mimeType = 'application/vnd.google-apps.spreadsheet' and trashed = false";

        var request = driveService.Files.List();
        request.Q = query;
        request.Fields = "files(id, name, webViewLink)";

        var response = Resilience.Execute(operation: "Drive.Files.List", () => request.Execute());

        return response
                .Files?.Select(f => (f.Id, f.WebViewLink ?? GetSpreadsheetUrl(spreadsheetId: f.Id)))
                .ToList()
            ?? [];
    }

    #endregion
}

#endregion
