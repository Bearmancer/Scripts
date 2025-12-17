namespace CSharpScripts.Services.Sync.Google;

public class GoogleSheetsService(string clientId, string clientSecret) : IDisposable
{
    private const string SheetName = "Scrobbles";
    private const string SpreadsheetTitle = "last.fm scrobbles";

    private static readonly FrozenSet<object> Headers = FrozenSet.ToFrozenSet<object>([
        "Date",
        "Track Title",
        "Artist",
        "Album",
    ]);

    private readonly Dictionary<string, Spreadsheet> spreadsheetCache = [];

    public static string GetSpreadsheetUrl(string spreadsheetId) =>
        $"https://docs.google.com/spreadsheets/d/{spreadsheetId}";

    private readonly SheetsService service = new(
        new BaseClientService.Initializer
        {
            HttpClientInitializer = GoogleCredentialService.GetCredential(clientId, clientSecret),
            ApplicationName = "CSharpScripts",
        }
    );

    private readonly DriveService driveService = new(
        new BaseClientService.Initializer
        {
            HttpClientInitializer = GoogleCredentialService.GetCredential(clientId, clientSecret),
            ApplicationName = "CSharpScripts",
        }
    );

    private Spreadsheet GetSpreadsheetMetadata(string spreadsheetId, bool forceRefresh = false)
    {
        if (!forceRefresh && spreadsheetCache.TryGetValue(spreadsheetId, out var cached))
            return cached;

        var spreadsheet = Resilience.Execute(
            operation: "Sheets.Get",
            action: () =>
            {
                var request = service.Spreadsheets.Get(spreadsheetId);
                request.Fields =
                    "spreadsheetId,properties/title,sheets(properties(sheetId,title,index,gridProperties))";
                return request.Execute();
            }
        );

        spreadsheetCache[spreadsheetId] = spreadsheet;
        return spreadsheet;
    }

    private void InvalidateCache(string spreadsheetId) => spreadsheetCache.Remove(spreadsheetId);

    private Sheet? FindSheet(string spreadsheetId, string sheetName, bool forceRefresh = false)
    {
        var spreadsheet = GetSpreadsheetMetadata(spreadsheetId, forceRefresh);
        return spreadsheet.Sheets?.FirstOrDefault(s =>
            s.Properties?.Title?.Equals(sheetName, StringComparison.OrdinalIgnoreCase) == true
        );
    }

    internal string CreateSpreadsheet(string title = SpreadsheetTitle)
    {
        var response = Resilience.Execute(
            operation: "Sheets.Create",
            action: () =>
            {
                Spreadsheet spreadsheet = new()
                {
                    Properties = new SpreadsheetProperties { Title = title },
                };
                return service.Spreadsheets.Create(spreadsheet).Execute();
            }
        );
        return response?.SpreadsheetId
            ?? throw new InvalidOperationException("Failed to create spreadsheet");
    }

    internal void DeleteSpreadsheet(string spreadsheetId)
    {
        Console.Info("Deleting spreadsheet: {0}", spreadsheetId);
        Resilience.Execute(
            operation: "Drive.Delete",
            action: () => driveService.Files.Delete(spreadsheetId).Execute()
        );
        Console.Success("Spreadsheet deleted");
    }

    internal bool SpreadsheetExists(string spreadsheetId)
    {
        try
        {
            Resilience.Execute(
                operation: "Sheets.Get",
                action: () => service.Spreadsheets.Get(spreadsheetId).Execute()
            );
            return true;
        }
        catch
        {
            return false;
        }
    }

    internal void EnsureSubsheetExists(
        string spreadsheetId,
        string sheetName,
        IEnumerable<object> headers
    )
    {
        var existingSheet = FindSheet(spreadsheetId, sheetName);

        if (existingSheet == null)
        {
            var targetIndex = GetAlphabeticalInsertIndex(spreadsheetId, sheetName);

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
                action: () => service.Spreadsheets.BatchUpdate(request, spreadsheetId).Execute()
            );
            InvalidateCache(spreadsheetId);
        }

        EnsureHeadersForSheet(spreadsheetId, sheetName, headers);
    }

    private int GetAlphabeticalInsertIndex(string spreadsheetId, string newSheetName)
    {
        var existingNames = GetSubsheetNames(spreadsheetId);
        existingNames.Add(newSheetName);
        existingNames.Sort(StringComparer.OrdinalIgnoreCase);
        return existingNames.IndexOf(newSheetName);
    }

    internal void ReorderSheetsAlphabetically(string spreadsheetId)
    {
        var spreadsheet = GetSpreadsheetMetadata(spreadsheetId, true);
        var sheets =
            spreadsheet
                .Sheets?.Where(s => s.Properties?.Title != null && s.Properties?.SheetId != null)
                .ToList()
            ?? [];

        if (sheets.Count <= 1)
            return;

        var sortedSheets = sheets
            .OrderBy(s => s.Properties!.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var needsReorder = false;
        for (var i = 0; i < sheets.Count; i++)
            if (sheets[i].Properties!.SheetId != sortedSheets[i].Properties!.SheetId)
            {
                needsReorder = true;
                break;
            }

        if (!needsReorder)
            return;

        Console.Info("Reordering {0} sheets alphabetically...", sheets.Count);

        List<Request> requests = [];
        for (var i = 0; i < sortedSheets.Count; i++)
            requests.Add(
                new Request
                {
                    UpdateSheetProperties = new UpdateSheetPropertiesRequest
                    {
                        Properties = new SheetProperties
                        {
                            SheetId = sortedSheets[i].Properties!.SheetId,
                            Index = i,
                        },
                        Fields = "index",
                    },
                }
            );

        BatchUpdateSpreadsheetRequest batchRequest = new() { Requests = requests };
        Resilience.Execute(
            operation: "Sheets.BatchUpdate.ReorderSheets",
            action: () => service.Spreadsheets.BatchUpdate(batchRequest, spreadsheetId).Execute()
        );
        InvalidateCache(spreadsheetId);
        Console.Success("Sheets reordered alphabetically");
    }

    internal void DeleteSubsheet(string spreadsheetId, string sheetName)
    {
        var sheet = FindSheet(spreadsheetId, sheetName);
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
            action: () => service.Spreadsheets.BatchUpdate(request, spreadsheetId).Execute()
        );
        InvalidateCache(spreadsheetId);
    }

    internal List<string> GetSubsheetNames(string spreadsheetId)
    {
        var spreadsheet = GetSpreadsheetMetadata(spreadsheetId);
        return spreadsheet
                .Sheets?.Select(s => s.Properties?.Title ?? "")
                .Where(t => !IsNullOrEmpty(t))
                .ToList()
            ?? [];
    }

    internal void ClearSubsheet(string spreadsheetId, string sheetName)
    {
        var escapedName = EscapeSheetName(sheetName);
        var range = $"{escapedName}!A2:Z";
        Resilience.Execute(
            operation: "Sheets.Values.Clear",
            action: () =>
                service
                    .Spreadsheets.Values.Clear(new ClearValuesRequest(), spreadsheetId, range)
                    .Execute()
        );
    }

    internal void WriteRows(string spreadsheetId, string sheetName, IList<IList<object>> rows)
    {
        if (rows.Count == 0)
            return;

        var escapedName = EscapeSheetName(sheetName);
        ValueRange body = new() { Values = rows };
        var range = $"{escapedName}!A2";

        Resilience.Execute(
            operation: "Sheets.Values.Update",
            action: () =>
            {
                var updateRequest = service.Spreadsheets.Values.Update(body, spreadsheetId, range);
                updateRequest.ValueInputOption = SpreadsheetsResource
                    .ValuesResource
                    .UpdateRequest
                    .ValueInputOptionEnum
                    .USERENTERED;
                return updateRequest.Execute();
            }
        );
    }

    /// <summary>
    /// Writes records to a sheet with headers. Clears existing data first.
    /// </summary>
    internal void WriteRecords<T>(
        string spreadsheetId,
        string sheetName,
        IReadOnlyList<object> headers,
        IEnumerable<T> records,
        Func<T, IList<object>> rowMapper
    )
    {
        ClearSubsheet(spreadsheetId, sheetName);

        // Combine headers and data into single write operation
        List<IList<object>> allRows =
        [
            [.. headers],
            .. records.Select(rowMapper),
        ];

        if (allRows.Count > 0)
            WriteRows(spreadsheetId, sheetName, allRows);
    }

    /// <summary>
    /// Appends records to an existing sheet without clearing or rewriting headers.
    /// </summary>
    internal void AppendRecords<T>(
        string spreadsheetId,
        string sheetName,
        IEnumerable<T> records,
        Func<T, IList<object>> rowMapper
    )
    {
        List<IList<object>> rows = [.. records.Select(rowMapper)];
        if (rows.Count > 0)
            AppendRows(spreadsheetId, sheetName, rows);
    }

    internal void RenameSubsheet(string spreadsheetId, string oldName, string newName)
    {
        var sheet = FindSheet(spreadsheetId, oldName);
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
            action: () => service.Spreadsheets.BatchUpdate(request, spreadsheetId).Execute()
        );
        InvalidateCache(spreadsheetId);
        Console.Debug("Renamed sheet '{0}' to '{1}'", oldName, newName);
    }

    internal void CleanupDefaultSheet(string spreadsheetId)
    {
        var spreadsheet = GetSpreadsheetMetadata(spreadsheetId);

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
            action: () => service.Spreadsheets.BatchUpdate(request, spreadsheetId).Execute()
        );
        InvalidateCache(spreadsheetId);
    }

    private void EnsureHeadersForSheet(
        string spreadsheetId,
        string sheetName,
        IEnumerable<object> headers
    )
    {
        var escapedName = EscapeSheetName(sheetName);
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
            action: () =>
            {
                var updateRequest = service.Spreadsheets.Values.Update(body, spreadsheetId, range);
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
        name.Contains('\'') || name.Contains(' ') || name.Contains('-')
            ? $"'{name.Replace("'", "''")}'"
            : name;

    private static string SanitizeForFileName(string name) =>
        name.Replace(":", " -")
            .Replace("/", "-")
            .Replace("\\", "-")
            .Replace("?", "")
            .Replace("*", "")
            .Replace("[", "(")
            .Replace("]", ")");

    internal void EnsureSheetExists(string spreadsheetId)
    {
        var sheet = FindSheet(spreadsheetId, SheetName);

        if (sheet == null)
        {
            var spreadsheet = GetSpreadsheetMetadata(spreadsheetId);
            RenameDefaultSheet(spreadsheetId, spreadsheet);
        }

        EnsureHeaders(spreadsheetId);
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
            action: () => service.Spreadsheets.BatchUpdate(request, spreadsheetId).Execute()
        );
        InvalidateCache(spreadsheetId);
    }

    private void EnsureHeaders(string spreadsheetId)
    {
        var range = $"{SheetName}!1:1";
        var existing = Resilience
            .Execute(
                operation: "Sheets.Values.Get.Headers",
                action: () => service.Spreadsheets.Values.Get(spreadsheetId, range).Execute()
            )
            .Values;

        var needsUpdate =
            existing == null
            || existing.Count == 0
            || existing[0].Count != Headers.Count
            || !existing[0].SequenceEqual(Headers);

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
            action: () =>
            {
                var updateRequest = service.Spreadsheets.Values.Update(body, spreadsheetId, range);
                updateRequest.ValueInputOption = SpreadsheetsResource
                    .ValuesResource
                    .UpdateRequest
                    .ValueInputOptionEnum
                    .USERENTERED;
                return updateRequest.Execute();
            }
        );
    }

    internal DateTime? GetLatestScrobbleTime(string spreadsheetId)
    {
        try
        {
            var range = $"{SheetName}!A2";
            var response = Resilience.Execute(
                operation: "Sheets.Values.Get.LatestTime",
                action: () =>
                {
                    var request = service.Spreadsheets.Values.Get(spreadsheetId, range);
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

            var firstRow = response.Values[0];
            if (firstRow == null || firstRow.Count == 0)
                return null;

            var rawValue = firstRow[0];
            Console.Debug(
                "Sheet raw value: '{0}' (type: {1})",
                rawValue,
                rawValue?.GetType().Name ?? "null"
            );

            if (rawValue is double or int or long or float or decimal)
            {
                var serialDate = Convert.ToDouble(rawValue);
                var parsed = DateTime.FromOADate(serialDate);
                Console.Debug("Parsed from OADate: {0:yyyy/MM/dd HH:mm:ss}", parsed);
                return parsed;
            }

            var latestTimeStr = rawValue?.ToString()?.TrimStart('\'') ?? "";
            string[] formats =
            [
                "yyyy/MM/dd HH:mm:ss",
                "yyyy/MM/dd HH:mm",
                "yyyy/MM/dd H:mm:ss",
                "yyyy/MM/dd H:mm",
            ];
            if (
                DateTime.TryParseExact(
                    latestTimeStr,
                    formats,
                    null,
                    System.Globalization.DateTimeStyles.None,
                    out var parsedStr
                )
            )
            {
                Console.Debug("Parsed from string: {0:yyyy/MM/dd HH:mm:ss}", parsedStr);
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
        var spreadsheet = GetSpreadsheetMetadata(spreadsheetId);
        return spreadsheet.Sheets?.Any(s =>
                s.Properties?.Title?.Equals(SheetName, StringComparison.OrdinalIgnoreCase) == true
            )
            ?? false;
    }

    internal int GetScrobbleCount(string spreadsheetId)
    {
        var range = $"{SheetName}!A:A";
        var response = Resilience.Execute(
            operation: "Sheets.Values.Get.RowCount",
            action: () =>
            {
                var request = service.Spreadsheets.Values.Get(spreadsheetId, range);
                return request.Execute();
            }
        );

        if (response.Values == null || response.Values.Count <= 1)
            return 0;

        return response.Values.Count - 1;
    }

    /// <summary>
    /// Delete all scrobble rows on or after the specified date.
    /// Returns number of rows deleted.
    /// </summary>
    internal int DeleteScrobblesOnOrAfter(string spreadsheetId, DateTime fromDate)
    {
        if (!SheetExists(spreadsheetId))
            return 0;

        var range = $"{SheetName}!A2:A";
        var response = Resilience.Execute(
            operation: "Sheets.Values.Get.AllDates",
            action: () =>
            {
                var request = service.Spreadsheets.Values.Get(spreadsheetId, range);
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
            var rawValue = row[0];

            if (rawValue is double or int or long or float or decimal)
                rowDate = DateTime.FromOADate(Convert.ToDouble(rawValue));
            else
            {
                var dateStr = rawValue?.ToString()?.TrimStart('\'') ?? "";
                if (
                    DateTime.TryParseExact(
                        dateStr,
                        "yyyy/MM/dd HH:mm:ss",
                        null,
                        System.Globalization.DateTimeStyles.None,
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
            "Deleting {0} scrobbles from {1} onwards...",
            rowsToDelete,
            fromDate.ToString("yyyy/MM/dd")
        );

        var sheetId = GetSheetId(spreadsheetId);
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
            action: () => service.Spreadsheets.BatchUpdate(deleteRequest, spreadsheetId).Execute()
        );

        Console.Success("Deleted {0} scrobbles", rowsToDelete);
        return rowsToDelete;
    }

    internal List<Scrobble> GetNewScrobbles(string spreadsheetId, List<Scrobble> allScrobbles)
    {
        if (!SheetExists(spreadsheetId))
        {
            Console.Debug("Sheet does not exist, returning all scrobbles");
            EnsureSheetExists(spreadsheetId);
            return allScrobbles;
        }

        var latestInSheet = GetLatestScrobbleTime(spreadsheetId);
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

        InsertRows(spreadsheetId, records);
    }

    private void InsertRows(string spreadsheetId, List<IList<object>> records)
    {
        var sheetId = GetSheetId(spreadsheetId);

        // Build cell data for UpdateCells
        List<RowData> rowDataList = [];
        foreach (var record in records)
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

        // Single batchUpdate with both InsertDimension and UpdateCells
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
            action: () => service.Spreadsheets.BatchUpdate(batchRequest, spreadsheetId).Execute()
        );
    }

    private int GetSheetId(string spreadsheetId)
    {
        var spreadsheet = GetSpreadsheetMetadata(spreadsheetId);
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
        var sheet = FindSheet(spreadsheetId, sheetName);
        if (sheet?.Properties?.SheetId == null)
        {
            Console.Warning("Sheet '{0}' not found for sorting", sheetName);
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

        Resilience.Execute(
            operation: "Sheets.BatchUpdate.Sort",
            action: () => service.Spreadsheets.BatchUpdate(request, spreadsheetId).Execute()
        );
        Console.Debug("Sorted sheet '{0}' by column {1}", sheetName, columnIndex);
    }

    internal void DeleteRowsFromSubsheet(
        string spreadsheetId,
        string sheetName,
        List<int> rowIndices
    )
    {
        if (rowIndices.Count == 0)
            return;

        var sheet = FindSheet(spreadsheetId, sheetName);
        if (sheet?.Properties?.SheetId == null)
        {
            Console.Warning("Sheet '{0}' not found for row deletion", sheetName);
            return;
        }

        // Sort descending and merge consecutive indices into ranges
        var sortedIndices = rowIndices.OrderByDescending(i => i).ToList();
        List<(int Start, int End)> ranges = [];

        var rangeStart = sortedIndices[0];
        var rangeEnd = sortedIndices[0];

        for (var i = 1; i < sortedIndices.Count; i++)
        {
            if (sortedIndices[i] == rangeEnd - 1)
            {
                // Consecutive - extend range
                rangeEnd = sortedIndices[i];
            }
            else
            {
                // Gap - save current range and start new one
                ranges.Add((rangeEnd, rangeStart));
                rangeStart = sortedIndices[i];
                rangeEnd = sortedIndices[i];
            }
        }
        ranges.Add((rangeEnd, rangeStart));

        List<Request> requests = [];
        foreach (var (start, end) in ranges)
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
            action: () => service.Spreadsheets.BatchUpdate(batchRequest, spreadsheetId).Execute()
        );
        InvalidateCache(spreadsheetId);
        Console.Debug(
            "Deleted {0} rows ({1} ranges) from sheet '{2}'",
            rowIndices.Count,
            ranges.Count,
            sheetName
        );
    }

    internal void AppendRows(string spreadsheetId, string sheetName, IList<IList<object>> rows)
    {
        if (rows.Count == 0)
            return;

        var escapedName = EscapeSheetName(sheetName);
        ValueRange body = new() { Values = rows };
        var range = $"{escapedName}!A:E";

        Resilience.Execute(
            operation: "Sheets.Values.Append",
            action: () =>
            {
                var appendRequest = service.Spreadsheets.Values.Append(body, spreadsheetId, range);
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

        Console.Debug("Appended {0} rows to sheet '{1}'", rows.Count, sheetName);
    }

    internal string GetOrCreateSpreadsheet(
        string? currentSpreadsheetId,
        string? defaultSpreadsheetId,
        string spreadsheetTitle,
        Action<string> onSpreadsheetResolved
    )
    {
        if (!IsNullOrEmpty(currentSpreadsheetId) && SpreadsheetExists(currentSpreadsheetId))
            return currentSpreadsheetId;

        if (!IsNullOrEmpty(defaultSpreadsheetId))
        {
            if (SpreadsheetExists(defaultSpreadsheetId))
            {
                onSpreadsheetResolved(defaultSpreadsheetId);
                return defaultSpreadsheetId;
            }

            Console.Warning("Default spreadsheet not found: {0}", defaultSpreadsheetId);
        }

        Console.Info("Creating spreadsheet: {0}", spreadsheetTitle);
        var newId = CreateSpreadsheet(spreadsheetTitle);
        onSpreadsheetResolved(newId);

        Console.Info("Created new spreadsheet: {0}", newId);
        return newId;
    }

    internal int ExportEachSheetAsCSV(
        string spreadsheetId,
        string outputDirectory,
        CancellationToken ct = default
    )
    {
        CreateDirectory(outputDirectory);

        Console.Info("Fetching spreadsheet metadata...");

        var spreadsheet = Resilience.Execute(
            operation: "Sheets.Get",
            action: () => service.Spreadsheets.Get(spreadsheetId).Execute(),
            ct: ct
        );

        var sheets = spreadsheet.Sheets?.Where(s => s.Properties?.SheetId != null).ToList() ?? [];
        var existingFiles = GetFiles(outputDirectory, "*.csv").Select(GetFileName).ToHashSet();
        var toExport = sheets
            .Where(s => !existingFiles.Contains($"{SanitizeForFileName(s.Properties!.Title!)}.csv"))
            .ToList();

        if (toExport.Count == 0)
            return sheets.Count;

        var totalSheets = sheets.Count;
        var alreadyExported = totalSheets - toExport.Count;
        var exported = 0;

        Console.Suppress = true;

        AnsiConsole
            .Progress()
            .AutoClear(true)
            .HideCompleted(false)
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

                    var sheetTitle = sheet.Properties!.Title!;
                    var sheetId = sheet.Properties.SheetId;
                    var safeFileName = SanitizeForFileName(sheetTitle);
                    var outputPath = Combine(outputDirectory, $"{safeFileName}.csv");

                    task.Description = $"Exporting: {sheetTitle}";

                    var exportUrl =
                        $"https://docs.google.com/spreadsheets/d/{spreadsheetId}/export?format=csv&gid={sheetId}";

                    using var httpClient = new HttpClient();
                    httpClient.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue(
                            "Bearer",
                            GoogleCredentialService.GetAccessToken(
                                driveService.HttpClientInitializer as UserCredential
                            )
                        );

                    var response = Resilience.Execute(
                        operation: "Sheets.ExportCSV",
                        action: () => httpClient.GetByteArrayAsync(exportUrl).Result,
                        ct: ct
                    );

                    WriteAllBytes(outputPath, response);
                    exported++;
                    task.Increment(1);
                }
            });

        Console.Suppress = false;

        return sheets.Count;
    }

    /// <summary>
    /// Async version of ExportEachSheetAsCSV with proper async HTTP calls.
    /// </summary>
    internal async Task<int> ExportEachSheetAsCSVAsync(
        string spreadsheetId,
        string outputDirectory,
        CancellationToken ct = default
    )
    {
        CreateDirectory(outputDirectory);

        Console.Info("Fetching spreadsheet metadata...");

        var spreadsheet = await Resilience.ExecuteAsync(
            operation: "Sheets.Get",
            action: async () => await service.Spreadsheets.Get(spreadsheetId).ExecuteAsync(ct),
            ct: ct
        );

        var sheets = spreadsheet.Sheets?.ToList() ?? [];
        if (sheets.Count == 0)
        {
            Console.Warning("No sheets found");
            return 0;
        }

        var existingFiles = GetFiles(outputDirectory, "*.csv")
            .Select(f => GetFileNameWithoutExtension(f))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var toExport = sheets
            .Where(s => !existingFiles.Contains(SanitizeForFileName(s.Properties?.Title ?? "")))
            .ToList();

        if (toExport.Count == 0)
        {
            Console.Info("All {0} sheets already exported", sheets.Count);
            return sheets.Count;
        }

        var totalSheets = sheets.Count;
        var alreadyExported = totalSheets - toExport.Count;
        Console.Info("Exporting {0} sheets ({1} already done)...", toExport.Count, alreadyExported);

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer",
                await GoogleCredentialService.GetAccessTokenAsync(
                    driveService.HttpClientInitializer as UserCredential,
                    ct
                )
            );

        foreach (var sheet in toExport)
        {
            if (ct.IsCancellationRequested)
                break;

            var sheetTitle = sheet.Properties!.Title!;
            var sheetId = sheet.Properties.SheetId;
            var safeFileName = SanitizeForFileName(sheetTitle);
            var outputPath = Combine(outputDirectory, $"{safeFileName}.csv");

            Console.Dim($"Exporting: {sheetTitle}");

            var exportUrl =
                $"https://docs.google.com/spreadsheets/d/{spreadsheetId}/export?format=csv&gid={sheetId}";

            var response = await Resilience.ExecuteAsync(
                operation: "Sheets.ExportCSV",
                action: async () => await httpClient.GetByteArrayAsync(exportUrl, ct),
                ct: ct
            );

            await WriteAllBytesAsync(outputPath, response, ct);
        }

        Console.Success("Exported {0} sheets", toExport.Count);
        return sheets.Count;
    }

    internal List<(string Id, string Url)> FindDuplicateSpreadsheets(string title)
    {
        var query =
            $"name = '{title.Replace("'", "\\'")}' and mimeType = 'application/vnd.google-apps.spreadsheet' and trashed = false";

        var request = driveService.Files.List();
        request.Q = query;
        request.Fields = "files(id, name, webViewLink)";

        var response = Resilience.Execute(
            operation: "Drive.Files.List",
            action: () => request.Execute()
        );

        return response
                .Files?.Select(f => (f.Id, f.WebViewLink ?? GetSpreadsheetUrl(f.Id)))
                .ToList()
            ?? [];
    }

    public void Dispose()
    {
        service?.Dispose();
        driveService?.Dispose();
        GC.SuppressFinalize(this);
    }
}
