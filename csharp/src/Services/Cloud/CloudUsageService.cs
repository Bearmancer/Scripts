namespace CSharpScripts.Services.Cloud;

using System.Net.Http.Headers;
using Azure.Core;
using Azure.Identity;

internal static class CloudUsageService
{
	private const string ManagementScope = "https://management.azure.com/.default";
	private const string ApiVersion = "2023-11-01";

	private static readonly HttpClient Http = new();

	internal static bool IsConfigured =>
		GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID") is not null;

	internal static async Task<AzureUsageReport> GetAzureUsageAsync(CancellationToken ct = default)
	{
		var subscriptionId =
			GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID")
			?? throw new InvalidOperationException(
				"AZURE_SUBSCRIPTION_ID environment variable is not set. "
					+ "Run 'az login' and set AZURE_SUBSCRIPTION_ID to your subscription ID."
			);

		TokenCredential credential = new DefaultAzureCredential();
		AccessToken token = await credential.GetTokenAsync(
			new TokenRequestContext([ManagementScope]),
			ct
		);

		var billingPeriod = DateTime.Now.ToString("MMMM yyyy", CultureInfo.InvariantCulture);

		var url =
			$"https://management.azure.com/subscriptions/{subscriptionId}"
			+ $"/providers/Microsoft.CostManagement/query?api-version={ApiVersion}";

		var requestBody = """
			{
			  "type": "ActualCost",
			  "timeframe": "BillingMonthToDate",
			  "dataset": {
			    "granularity": "None",
			    "grouping": [
			      { "type": "Dimension", "name": "ServiceName" },
			      { "type": "Dimension", "name": "MeterName" }
			    ],
			    "aggregation": {
			      "totalCost": {
			        "name": "PreTaxCost",
			        "function": "Sum"
			      }
			    }
			  }
			}
			""";

		using var request = new HttpRequestMessage(HttpMethod.Post, url);
		request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
		request.Content = new StringContent(
			requestBody,
			System.Text.Encoding.UTF8,
			"application/json"
		);

		using HttpResponseMessage response = await Http.SendAsync(request, ct);
		var json = await response.Content.ReadAsStringAsync(ct);

		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException(
				$"Azure Cost Management API returned {(int)response.StatusCode}: {json}"
			);
		}

		return ParseResponse(subscriptionId, billingPeriod, json);
	}

	private static AzureUsageReport ParseResponse(
		string subscriptionId,
		string billingPeriod,
		string json
	)
	{
		using var doc = JsonDocument.Parse(json);
		JsonElement properties = doc.RootElement.GetProperty("properties");
		JsonElement columns = properties.GetProperty("columns");
		JsonElement rows = properties.GetProperty("rows");

		var costIndex = FindColumnIndex(columns, "PreTaxCost");
		var serviceIndex = FindColumnIndex(columns, "ServiceName");
		var meterIndex = FindColumnIndex(columns, "MeterName");
		var currencyIndex = FindColumnIndex(columns, "Currency");

		List<ServiceUsage> services = [];

		foreach (JsonElement row in rows.EnumerateArray())
		{
			JsonElement[] cells = [.. row.EnumerateArray()];

			var cost =
				cells[costIndex].ValueKind == JsonValueKind.Number
					? cells[costIndex].GetDecimal()
					: 0m;

			var serviceName = cells[serviceIndex].GetString() ?? Empty;
			var meterName = cells[meterIndex].GetString() ?? Empty;
			var currency =
				currencyIndex >= 0 && currencyIndex < cells.Length
					? cells[currencyIndex].GetString() ?? "USD"
					: "USD";

			services.Add(new ServiceUsage(serviceName, meterName, cost, currency));
		}

		services.Sort(
			(a, b) =>
			{
				var costCompare = b.Cost.CompareTo(a.Cost);
				return costCompare != 0
					? costCompare
					: Compare(a.ServiceName, b.ServiceName, OrdinalIgnoreCase);
			}
		);

		var totalCost = services.Aggregate(0m, (sum, s) => sum + s.Cost);

		return new AzureUsageReport(subscriptionId, services, totalCost, billingPeriod);
	}

	private static int FindColumnIndex(JsonElement columns, string name)
	{
		var index = 0;
		foreach (JsonElement col in columns.EnumerateArray())
		{
			if (string.Equals(col.GetProperty("name").GetString(), name, OrdinalIgnoreCase))
				return index;
			index++;
		}

		return -1;
	}
}
