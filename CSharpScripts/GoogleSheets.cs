using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;

namespace CSharpScripts;

public static class GoogleSheets
{
    public static async Task AppendAsync(
        SheetsService service,
        string spreadsheetId,
        string rangeA1,
        IList<IList<object>> rows
    )
    {
        if (rows.Count == 0)
            throw new ArgumentException("Rows cannot be empty", nameof(rows));

        var valueRange = new ValueRange { Values = rows };
        var request = service.Spreadsheets.Values.Append(
            body: valueRange,
            spreadsheetId: spreadsheetId,
            range: rangeA1
        );

        request.ValueInputOption = SpreadsheetsResource
            .ValuesResource
            .AppendRequest
            .ValueInputOptionEnum
            .RAW;
        request.InsertDataOption = SpreadsheetsResource
            .ValuesResource
            .AppendRequest
            .InsertDataOptionEnum
            .INSERTROWS;

        await request.ExecuteAsync();
    }

    public static async Task UpdateAsync(
        SheetsService service,
        string spreadsheetId,
        string rangeA1,
        IList<IList<object>> rows
    )
    {
        if (rows.Count == 0)
            throw new ArgumentException("Rows cannot be empty", nameof(rows));

        var valueRange = new ValueRange { Values = rows };
        var request = service.Spreadsheets.Values.Update(
            body: valueRange,
            spreadsheetId: spreadsheetId,
            range: rangeA1
        );

        request.ValueInputOption = SpreadsheetsResource
            .ValuesResource
            .UpdateRequest
            .ValueInputOptionEnum
            .USERENTERED;

        await request.ExecuteAsync();
    }

    public static async Task<ValueRange?> GetAsync(
        SheetsService service,
        string spreadsheetId,
        string rangeA1
    )
    {
        var request = service.Spreadsheets.Values.Get(spreadsheetId: spreadsheetId, range: rangeA1);

        request.ValueRenderOption = SpreadsheetsResource
            .ValuesResource
            .GetRequest
            .ValueRenderOptionEnum
            .FORMATTEDVALUE;

        return await request.ExecuteAsync();
    }

    public static async Task EnsureSheetExistsAsync(
        SheetsService service,
        string spreadsheetId,
        string sheetName
    )
    {
        var spreadsheet = await service.Spreadsheets.Get(spreadsheetId).ExecuteAsync();
        var exists = spreadsheet.Sheets.Any(sheet =>
            string.Equals(sheet.Properties.Title, sheetName, StringComparison.OrdinalIgnoreCase)
        );

        if (exists)
            return;

        var request = new Request
        {
            AddSheet = new AddSheetRequest
            {
                Properties = new SheetProperties { Title = sheetName },
            },
        };

        await service
            .Spreadsheets.BatchUpdate(
                body: new BatchUpdateSpreadsheetRequest { Requests = [request] },
                spreadsheetId: spreadsheetId
            )
            .ExecuteAsync();
    }

    public static async Task<int> GetSheetIdAsync(
        SheetsService service,
        string spreadsheetId,
        string sheetName
    )
    {
        var spreadsheet = await service.Spreadsheets.Get(spreadsheetId).ExecuteAsync();
        return spreadsheet
                .Sheets.FirstOrDefault(sheet => sheet.Properties.Title == sheetName)
                ?.Properties.SheetId
            ?? throw new InvalidOperationException($"Sheet '{sheetName}' not found");
    }

    public static async Task InsertRowsAsync(
        SheetsService service,
        string spreadsheetId,
        int sheetId,
        int startIndex,
        int rowCount
    )
    {
        var request = new Request
        {
            InsertDimension = new InsertDimensionRequest
            {
                Range = new DimensionRange
                {
                    SheetId = sheetId,
                    Dimension = "ROWS",
                    StartIndex = startIndex,
                    EndIndex = startIndex + rowCount,
                },
            },
        };

        await service
            .Spreadsheets.BatchUpdate(
                body: new BatchUpdateSpreadsheetRequest { Requests = [request] },
                spreadsheetId: spreadsheetId
            )
            .ExecuteAsync();
    }

    public static async Task FormatHeaderRowAsync(
        SheetsService service,
        string spreadsheetId,
        int sheetId,
        int columnCount
    )
    {
        var request = new Request
        {
            RepeatCell = new RepeatCellRequest
            {
                Range = new GridRange
                {
                    SheetId = sheetId,
                    StartRowIndex = 0,
                    EndRowIndex = 1,
                    StartColumnIndex = 0,
                    EndColumnIndex = columnCount,
                },
                Cell = new CellData
                {
                    UserEnteredFormat = new CellFormat
                    {
                        TextFormat = new TextFormat { Bold = true },
                    },
                },
                Fields = "userEnteredFormat(textFormat)",
            },
        };

        await service
            .Spreadsheets.BatchUpdate(
                body: new BatchUpdateSpreadsheetRequest { Requests = [request] },
                spreadsheetId: spreadsheetId
            )
            .ExecuteAsync();
    }
}
