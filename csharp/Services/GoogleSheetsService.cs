namespace CSharpScripts.Services;

internal class GoogleSheetsService(string clientId, string clientSecret)
{
    const string SheetName = "Scrobbles";
    const string SpreadsheetTitle = "last.fm scrobbles";

    static readonly FrozenSet<object> Headers = FrozenSet.ToFrozenSet<object>([
        "Date",
        "Track Title",
        "Artist",
        "Album",
    ]);

    internal static string GetSpreadsheetUrl(string spreadsheetId) =>
        $"https://docs.google.com/spreadsheets/d/{spreadsheetId}";

    readonly SheetsService service = new(
        new BaseClientService.Initializer
        {
            HttpClientInitializer = GoogleCredentialService.GetCredential(clientId, clientSecret),
            ApplicationName = "CSharpScripts",
        }
    );

    readonly DriveService driveService = new(
        new BaseClientService.Initializer
        {
            HttpClientInitializer = GoogleCredentialService.GetCredential(clientId, clientSecret),
            ApplicationName = "CSharpScripts",
        }
    );

    internal string CreateSpreadsheet(string title = SpreadsheetTitle)
    {
        var response = ApiConfig.ExecuteWithRetry(
            operationName: "Sheets.Create",
            action: () =>
            {
                Spreadsheet spreadsheet = new()
                {
                    Properties = new SpreadsheetProperties { Title = title },
                };
                return service.Spreadsheets.Create(spreadsheet).Execute();
            },
            postAction: () => ApiConfig.Delay(ServiceType.Sheets)
        );
        return response?.SpreadsheetId
            ?? throw new InvalidOperationException("Failed to create spreadsheet");
    }

    internal void DeleteSpreadsheet(string spreadsheetId)
    {
        Logger.Info("Deleting spreadsheet: {0}", spreadsheetId);
        ApiConfig.ExecuteWithRetry(
            operationName: "Drive.Delete",
            action: () => driveService.Files.Delete(spreadsheetId).Execute(),
            postAction: () => ApiConfig.Delay(ServiceType.Sheets)
        );
        Logger.Success("Spreadsheet deleted");
    }

    internal bool SpreadsheetExists(string spreadsheetId)
    {
        try
        {
            ApiConfig.ExecuteWithRetry(
                operationName: "Sheets.Get",
                action: () => service.Spreadsheets.Get(spreadsheetId).Execute(),
                postAction: () => ApiConfig.Delay(ServiceType.Sheets)
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
        var spreadsheet = ApiConfig.ExecuteWithRetry(
            operationName: "Sheets.Get",
            action: () => service.Spreadsheets.Get(spreadsheetId).Execute(),
            postAction: () => ApiConfig.Delay(ServiceType.Sheets)
        );
        var existingSheet = spreadsheet.Sheets?.FirstOrDefault(s =>
            s.Properties?.Title?.Equals(sheetName, StringComparison.OrdinalIgnoreCase) == true
        );

        if (existingSheet == null)
        {
            BatchUpdateSpreadsheetRequest request = new()
            {
                Requests =
                [
                    new Request
                    {
                        AddSheet = new AddSheetRequest
                        {
                            Properties = new SheetProperties { Title = sheetName },
                        },
                    },
                ],
            };
            ApiConfig.ExecuteWithRetry(
                operationName: "Sheets.BatchUpdate.AddSheet",
                action: () => service.Spreadsheets.BatchUpdate(request, spreadsheetId).Execute(),
                postAction: () => ApiConfig.Delay(ServiceType.Sheets)
            );
        }

        EnsureHeadersForSheet(spreadsheetId, sheetName, headers);
    }

    internal void DeleteSubsheet(string spreadsheetId, string sheetName)
    {
        var spreadsheet = ApiConfig.ExecuteWithRetry(
            operationName: "Sheets.Get",
            action: () => service.Spreadsheets.Get(spreadsheetId).Execute(),
            postAction: () => ApiConfig.Delay(ServiceType.Sheets)
        );
        var sheet = spreadsheet.Sheets?.FirstOrDefault(s =>
            s.Properties?.Title?.Equals(sheetName, StringComparison.OrdinalIgnoreCase) == true
        );

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
        ApiConfig.ExecuteWithRetry(
            operationName: "Sheets.BatchUpdate.DeleteSheet",
            action: () => service.Spreadsheets.BatchUpdate(request, spreadsheetId).Execute(),
            postAction: () => ApiConfig.Delay(ServiceType.Sheets)
        );
    }

    internal List<string> GetSubsheetNames(string spreadsheetId)
    {
        var spreadsheet = ApiConfig.ExecuteWithRetry(
            operationName: "Sheets.Get",
            action: () => service.Spreadsheets.Get(spreadsheetId).Execute(),
            postAction: () => ApiConfig.Delay(ServiceType.Sheets)
        );
        return spreadsheet
                .Sheets?.Select(s => s.Properties?.Title ?? "")
                .Where(t => !IsNullOrEmpty(t))
                .ToList() ?? [];
    }

    internal void ClearSubsheet(string spreadsheetId, string sheetName)
    {
        var escapedName = EscapeSheetName(sheetName);
        var range = $"{escapedName}!A2:Z";
        ApiConfig.ExecuteWithRetry(
            operationName: "Sheets.Values.Clear",
            action: () =>
                service
                    .Spreadsheets.Values.Clear(new ClearValuesRequest(), spreadsheetId, range)
                    .Execute(),
            postAction: () => ApiConfig.Delay(ServiceType.Sheets)
        );
    }

    internal void WriteRows(string spreadsheetId, string sheetName, IList<IList<object>> rows)
    {
        if (rows.Count == 0)
            return;

        var escapedName = EscapeSheetName(sheetName);
        ValueRange body = new() { Values = rows };
        var range = $"{escapedName}!A2";

        ApiConfig.ExecuteWithRetry(
            operationName: "Sheets.Values.Update",
            action: () =>
            {
                var updateRequest = service.Spreadsheets.Values.Update(body, spreadsheetId, range);
                updateRequest.ValueInputOption = SpreadsheetsResource
                    .ValuesResource
                    .UpdateRequest
                    .ValueInputOptionEnum
                    .USERENTERED;
                return updateRequest.Execute();
            },
            postAction: () => ApiConfig.Delay(ServiceType.Sheets)
        );
    }

    internal void RenameSubsheet(string spreadsheetId, string oldName, string newName)
    {
        var spreadsheet = ApiConfig.ExecuteWithRetry(
            operationName: "Sheets.Get",
            action: () => service.Spreadsheets.Get(spreadsheetId).Execute(),
            postAction: () => ApiConfig.Delay(ServiceType.Sheets)
        );
        var sheet = spreadsheet.Sheets?.FirstOrDefault(s =>
            s.Properties?.Title?.Equals(oldName, StringComparison.OrdinalIgnoreCase) == true
        );

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
        ApiConfig.ExecuteWithRetry(
            operationName: "Sheets.BatchUpdate.Rename",
            action: () => service.Spreadsheets.BatchUpdate(request, spreadsheetId).Execute(),
            postAction: () => ApiConfig.Delay(ServiceType.Sheets)
        );
        Logger.Debug("Renamed sheet '{0}' to '{1}'", oldName, newName);
    }

    internal void CleanupDefaultSheet(string spreadsheetId)
    {
        var spreadsheet = ApiConfig.ExecuteWithRetry(
            operationName: "Sheets.Get",
            action: () => service.Spreadsheets.Get(spreadsheetId).Execute(),
            postAction: () => ApiConfig.Delay(ServiceType.Sheets)
        );

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
        ApiConfig.ExecuteWithRetry(
            operationName: "Sheets.BatchUpdate.DeleteSheet1",
            action: () => service.Spreadsheets.BatchUpdate(request, spreadsheetId).Execute(),
            postAction: () => ApiConfig.Delay(ServiceType.Sheets)
        );
    }

    void EnsureHeadersForSheet(string spreadsheetId, string sheetName, IEnumerable<object> headers)
    {
        var escapedName = EscapeSheetName(sheetName);
        var range = $"{escapedName}!1:1";
        ValueRange body = new() { Values = [headers.ToList()] };
        ApiConfig.ExecuteWithRetry(
            operationName: "Sheets.Values.Update.Headers",
            action: () =>
            {
                var updateRequest = service.Spreadsheets.Values.Update(body, spreadsheetId, range);
                updateRequest.ValueInputOption = SpreadsheetsResource
                    .ValuesResource
                    .UpdateRequest
                    .ValueInputOptionEnum
                    .USERENTERED;
                return updateRequest.Execute();
            },
            postAction: () => ApiConfig.Delay(ServiceType.Sheets)
        );
    }

    static string EscapeSheetName(string name) =>
        name.Contains('\'') || name.Contains(' ') || name.Contains('-')
            ? $"'{name.Replace("'", "''")}'"
            : name;

    static string SanitizeForFileName(string name) =>
        name.Replace(":", " -")
            .Replace("/", "-")
            .Replace("\\", "-")
            .Replace("?", "")
            .Replace("*", "")
            .Replace("[", "(")
            .Replace("]", ")");

    internal void EnsureSheetExists(string spreadsheetId)
    {
        var spreadsheet = ApiConfig.ExecuteWithRetry(
            operationName: "Sheets.Get",
            action: () => service.Spreadsheets.Get(spreadsheetId).Execute(),
            postAction: () => ApiConfig.Delay(ServiceType.Sheets)
        );
        var sheet = spreadsheet.Sheets?.FirstOrDefault(s =>
            s.Properties?.Title?.Equals(SheetName, StringComparison.OrdinalIgnoreCase) == true
        );

        if (sheet == null)
            RenameDefaultSheet(spreadsheetId, spreadsheet);

        EnsureHeaders(spreadsheetId);
    }

    void RenameDefaultSheet(string spreadsheetId, Spreadsheet spreadsheet)
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
        ApiConfig.ExecuteWithRetry(
            operationName: "Sheets.BatchUpdate.RenameDefault",
            action: () => service.Spreadsheets.BatchUpdate(request, spreadsheetId).Execute(),
            postAction: () => ApiConfig.Delay(ServiceType.Sheets)
        );
    }

    void EnsureHeaders(string spreadsheetId)
    {
        var range = $"{SheetName}!1:1";
        var existing = ApiConfig
            .ExecuteWithRetry(
                operationName: "Sheets.Values.Get.Headers",
                action: () => service.Spreadsheets.Values.Get(spreadsheetId, range).Execute(),
                postAction: () => ApiConfig.Delay(ServiceType.Sheets)
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
        ApiConfig.ExecuteWithRetry(
            operationName: "Sheets.Values.Update.Headers",
            action: () =>
            {
                var updateRequest = service.Spreadsheets.Values.Update(body, spreadsheetId, range);
                updateRequest.ValueInputOption = SpreadsheetsResource
                    .ValuesResource
                    .UpdateRequest
                    .ValueInputOptionEnum
                    .USERENTERED;
                return updateRequest.Execute();
            },
            postAction: () => ApiConfig.Delay(ServiceType.Sheets)
        );
    }

    internal DateTime? GetLatestScrobbleTime(string spreadsheetId)
    {
        try
        {
            var range = $"{SheetName}!A2";
            var response = ApiConfig.ExecuteWithRetry(
                operationName: "Sheets.Values.Get.LatestTime",
                action: () =>
                {
                    var request = service.Spreadsheets.Values.Get(spreadsheetId, range);
                    request.ValueRenderOption = SpreadsheetsResource
                        .ValuesResource
                        .GetRequest
                        .ValueRenderOptionEnum
                        .UNFORMATTEDVALUE;
                    return request.Execute();
                },
                postAction: () => ApiConfig.Delay(ServiceType.Sheets)
            );

            if (response.Values == null || response.Values.Count == 0)
                return null;

            var firstRow = response.Values[0];
            if (firstRow == null || firstRow.Count == 0)
                return null;

            var rawValue = firstRow[0];
            Logger.Debug(
                "Sheet raw value: '{0}' (type: {1})",
                rawValue,
                rawValue?.GetType().Name ?? "null"
            );

            if (rawValue is double or int or long or float or decimal)
            {
                var serialDate = Convert.ToDouble(rawValue);
                var parsed = DateTime.FromOADate(serialDate);
                Logger.Debug("Parsed from OADate: {0:yyyy/MM/dd HH:mm:ss}", parsed);
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
                    provider: null,
                    style: System.Globalization.DateTimeStyles.None,
                    out var parsedStr
                )
            )
            {
                Logger.Debug("Parsed from string: {0:yyyy/MM/dd HH:mm:ss}", parsedStr);
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
        try
        {
            var spreadsheet = ApiConfig.ExecuteWithRetry(
                operationName: "Sheets.Get",
                action: () => service.Spreadsheets.Get(spreadsheetId).Execute(),
                postAction: () => ApiConfig.Delay(ServiceType.Sheets)
            );
            return spreadsheet.Sheets?.Any(s =>
                    s.Properties?.Title?.Equals(SheetName, StringComparison.OrdinalIgnoreCase)
                    == true
                ) ?? false;
        }
        catch
        {
            return false;
        }
    }

    internal int GetScrobbleCount(string spreadsheetId)
    {
        var range = $"{SheetName}!A:A";
        var response = ApiConfig.ExecuteWithRetry(
            operationName: "Sheets.Values.Get.RowCount",
            action: () =>
            {
                var request = service.Spreadsheets.Values.Get(spreadsheetId, range);
                return request.Execute();
            },
            postAction: () => ApiConfig.Delay(ServiceType.Sheets)
        );

        if (response.Values == null || response.Values.Count <= 1)
            return 0;

        return response.Values.Count - 1;
    }

    internal List<Scrobble> GetNewScrobbles(string spreadsheetId, List<Scrobble> allScrobbles)
    {
        if (!SheetExists(spreadsheetId))
        {
            Logger.Debug("Sheet does not exist, returning all scrobbles");
            EnsureSheetExists(spreadsheetId);
            return allScrobbles;
        }

        var latestInSheet = GetLatestScrobbleTime(spreadsheetId);
        if (latestInSheet == null)
            return allScrobbles;

        return allScrobbles.Where(s => s.PlayedAt > latestInSheet).ToList();
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

    void InsertRows(string spreadsheetId, IList<IList<object>> records)
    {
        int sheetId = GetSheetId(spreadsheetId);

        BatchUpdateSpreadsheetRequest insertRequest = new()
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
            ],
        };
        ApiConfig.ExecuteWithRetry(
            operationName: "Sheets.BatchUpdate.InsertRows",
            action: () => service.Spreadsheets.BatchUpdate(insertRequest, spreadsheetId).Execute(),
            postAction: () => ApiConfig.Delay(ServiceType.Sheets)
        );

        ValueRange body = new() { Values = records };
        ApiConfig.ExecuteWithRetry(
            operationName: "Sheets.Values.Update.InsertedRows",
            action: () =>
            {
                var updateRequest = service.Spreadsheets.Values.Update(
                    body,
                    spreadsheetId,
                    range: $"{SheetName}!A2"
                );
                updateRequest.ValueInputOption = SpreadsheetsResource
                    .ValuesResource
                    .UpdateRequest
                    .ValueInputOptionEnum
                    .USERENTERED;
                return updateRequest.Execute();
            },
            postAction: () => ApiConfig.Delay(ServiceType.Sheets)
        );
    }

    int GetSheetId(string spreadsheetId)
    {
        var sheets = ApiConfig
            .ExecuteWithRetry(
                operationName: "Sheets.Get.SheetId",
                action: () => service.Spreadsheets.Get(spreadsheetId).Execute(),
                postAction: () => ApiConfig.Delay(ServiceType.Sheets)
            )
            .Sheets;
        var sheet = sheets?.FirstOrDefault(s => s.Properties.Title == SheetName);
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
        var spreadsheet = ApiConfig.ExecuteWithRetry(
            operationName: "Sheets.Get",
            action: () => service.Spreadsheets.Get(spreadsheetId).Execute(),
            postAction: () => ApiConfig.Delay(ServiceType.Sheets)
        );
        var sheet = spreadsheet.Sheets?.FirstOrDefault(s =>
            s.Properties?.Title?.Equals(sheetName, StringComparison.OrdinalIgnoreCase) == true
        );

        if (sheet?.Properties?.SheetId == null)
        {
            Logger.Warning("Sheet '{0}' not found for sorting", sheetName);
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

        ApiConfig.ExecuteWithRetry(
            operationName: "Sheets.BatchUpdate.Sort",
            action: () => service.Spreadsheets.BatchUpdate(request, spreadsheetId).Execute(),
            postAction: () => ApiConfig.Delay(ServiceType.Sheets)
        );
        Logger.Debug("Sorted sheet '{0}' by column {1}", sheetName, columnIndex);
    }

    internal void DeleteRowsFromSubsheet(
        string spreadsheetId,
        string sheetName,
        List<int> rowIndices
    )
    {
        if (rowIndices.Count == 0)
            return;

        var spreadsheet = ApiConfig.ExecuteWithRetry(
            operationName: "Sheets.Get",
            action: () => service.Spreadsheets.Get(spreadsheetId).Execute(),
            postAction: () => ApiConfig.Delay(ServiceType.Sheets)
        );
        var sheet = spreadsheet.Sheets?.FirstOrDefault(s =>
            s.Properties?.Title?.Equals(sheetName, StringComparison.OrdinalIgnoreCase) == true
        );

        if (sheet?.Properties?.SheetId == null)
        {
            Logger.Warning("Sheet '{0}' not found for row deletion", sheetName);
            return;
        }

        var sortedIndices = rowIndices.OrderByDescending(i => i).ToList();

        List<Request> requests = [];
        foreach (var rowIndex in sortedIndices)
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
                            StartIndex = rowIndex - 1,
                            EndIndex = rowIndex,
                        },
                    },
                }
            );
        }

        BatchUpdateSpreadsheetRequest batchRequest = new() { Requests = requests };
        ApiConfig.ExecuteWithRetry(
            operationName: "Sheets.BatchUpdate.DeleteRows",
            action: () => service.Spreadsheets.BatchUpdate(batchRequest, spreadsheetId).Execute(),
            postAction: () => ApiConfig.Delay(ServiceType.Sheets)
        );

        Logger.Debug("Deleted {0} rows from sheet '{1}'", rowIndices.Count, sheetName);
    }

    internal void AppendRows(string spreadsheetId, string sheetName, IList<IList<object>> rows)
    {
        if (rows.Count == 0)
            return;

        var escapedName = EscapeSheetName(sheetName);
        ValueRange body = new() { Values = rows };
        var range = $"{escapedName}!A:E";

        ApiConfig.ExecuteWithRetry(
            operationName: "Sheets.Values.Append",
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
            },
            postAction: () => ApiConfig.Delay(ServiceType.Sheets)
        );

        Logger.Debug("Appended {0} rows to sheet '{1}'", rows.Count, sheetName);
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

            Logger.Warning("Default spreadsheet not found: {0}", defaultSpreadsheetId);
        }

        Logger.Info("Creating spreadsheet: {0}", spreadsheetTitle);
        var newId = CreateSpreadsheet(title: spreadsheetTitle);
        onSpreadsheetResolved(newId);

        Logger.Info("Created new spreadsheet: {0}", newId);
        return newId;
    }

    internal void ExportEachSheetAsCSV(string spreadsheetId, string outputDirectory)
    {
        Logger.Info("Exporting each sheet as CSV to: {0}", outputDirectory);
        CreateDirectory(outputDirectory);

        var spreadsheet = ApiConfig.ExecuteWithRetry(
            operationName: "Sheets.Get",
            action: () => service.Spreadsheets.Get(spreadsheetId).Execute(),
            postAction: () => ApiConfig.Delay(ServiceType.Sheets)
        );

        foreach (var sheet in spreadsheet.Sheets ?? [])
        {
            var sheetTitle = sheet.Properties?.Title ?? "Untitled";
            var sheetId = sheet.Properties?.SheetId;

            if (sheetId == null)
                continue;

            var safeFileName = SanitizeForFileName(sheetTitle);
            var outputPath = Combine(outputDirectory, $"{safeFileName}.csv");

            Logger.Debug("Exporting sheet '{0}' to {1}", sheetTitle, outputPath);

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

            var response = ApiConfig.ExecuteWithRetry(
                operationName: "Sheets.ExportCSV",
                action: () => httpClient.GetByteArrayAsync(exportUrl).Result,
                postAction: () => ApiConfig.Delay(ServiceType.Sheets)
            );

            WriteAllBytes(outputPath, response);
        }

        Logger.Success("Exported {0} sheets as CSV", spreadsheet.Sheets?.Count ?? 0);
    }

    internal List<(string Id, string Url)> FindDuplicateSpreadsheets(string title)
    {
        var query =
            $"name = '{title.Replace("'", "\\'")}' and mimeType = 'application/vnd.google-apps.spreadsheet' and trashed = false";

        var request = driveService.Files.List();
        request.Q = query;
        request.Fields = "files(id, name, webViewLink)";

        var response = ApiConfig.ExecuteWithRetry(
            operationName: "Drive.Files.List",
            action: () => request.Execute(),
            postAction: () => ApiConfig.Delay(ServiceType.Sheets)
        );

        return response
                .Files?.Select(f => (f.Id, f.WebViewLink ?? GetSpreadsheetUrl(f.Id)))
                .ToList() ?? [];
    }
}
