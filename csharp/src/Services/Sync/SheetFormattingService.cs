#pragma warning disable IDE0007, IDE0051, IDE0005

using Google.Apis.Sheets.v4.Data;

namespace CSharpScripts.Services.Sync;

internal class SheetFormattingService(SpreadsheetsResource Spreadsheets)
{
	public async Task SortSubsheetByColumnAsync(
		string spreadsheetId,
		string sheetName,
		int columnIndex,
		bool ascending,
		CancellationToken ct
	)
	{
		SpreadsheetsResource.GetRequest metadataRequest = Spreadsheets.Get(spreadsheetId);
		Spreadsheet metadata = await metadataRequest.ExecuteAsync(ct);

		Sheet? sheet = metadata.Sheets?.FirstOrDefault(s => s.Properties?.Title == sheetName);
		if (sheet?.Properties?.SheetId == null)
			return;

		int sheetId = sheet.Properties.SheetId.Value;

		SortRangeRequest sortRequest = new()
		{
			Range = new GridRange { SheetId = sheetId, StartRowIndex = 1 },
			SortSpecs =
			[
				new SortSpec
				{
					DimensionIndex = columnIndex,
					SortOrder = ascending ? "ASCENDING" : "DESCENDING",
				},
			],
		};

		BatchUpdateSpreadsheetRequest batchRequest = new()
		{
			Requests = [new Request { SortRange = sortRequest }],
		};

		await Spreadsheets.BatchUpdate(batchRequest, spreadsheetId).ExecuteAsync(ct);
	}
}
