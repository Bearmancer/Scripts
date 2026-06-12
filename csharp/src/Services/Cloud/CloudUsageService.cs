using System.Net.Http.Headers;
using System.Text;
using Azure.Core;

namespace Scripts.Services.Cloud;

internal static class CloudUsageService
{
	private const string ManagementScope = "https://management.azure.com/.default";
	private const string ApiVersion = "2023-11-01";

	private static readonly HttpClient Http = new();

	internal static bool IsConfigured =>
		GetEnvironmentVariable(variable: "AZURE_SUBSCRIPTION_ID") is { };

	internal static async Task<AzureUsageReport> GetAzureUsageAsync(CancellationToken ct = default)
	{
		var subscriptionId =
			GetEnvironmentVariable(variable: "AZURE_SUBSCRIPTION_ID")
			?? throw new InvalidOperationException(
				"AZURE_SUBSCRIPTION_ID environment variable is not set. "
					+ "Run 'az login' and set AZURE_SUBSCRIPTION_ID to your subscription ID."
			);

		var token = await Core.Auth.AzureAuth.Credential.GetTokenAsync(
			new TokenRequestContext([ManagementScope]),
			cancellationToken: ct
		);

		var billingPeriod = DateTimeOffset.Now.ToString(
			format: "MMMM yyyy",
			formatProvider: CultureInfo.InvariantCulture
		);

		var url =
			$"https://management.azure.com/subscriptions/{subscriptionId}"
			+ $"/providers/Microsoft.CostManagement/query?api-version={ApiVersion}";

		const string requestBody = /*lang=json,strict*/ """
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

		using HttpRequestMessage request = new(method: HttpMethod.Post, requestUri: url);
		request.Headers.Authorization = new AuthenticationHeaderValue(
			scheme: "Bearer",
			parameter: token.Token
		);
		request.Content = new StringContent(
			content: requestBody,
			encoding: Encoding.UTF8,
			mediaType: "application/json"
		);

		using HttpResponseMessage response = await Http.SendAsync(
			request: request,
			cancellationToken: ct
		);
		var json = await response.Content.ReadAsStringAsync(cancellationToken: ct);

		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException(
				$"Azure Cost Management API returned {(int)response.StatusCode}: {json}"
			);
		}

		return ParseResponse(
			subscriptionId: subscriptionId,
			billingPeriod: billingPeriod,
			json: json
		);
	}

	private static AzureUsageReport ParseResponse(
		string subscriptionId,
		string billingPeriod,
		string json
	)
	{
		using JsonDocument doc = JsonDocument.Parse(json: json);
		JsonElement properties = doc.RootElement.GetProperty(propertyName: "properties");
		JsonElement columns = properties.GetProperty(propertyName: "columns");
		JsonElement rows = properties.GetProperty(propertyName: "rows");

		var costIndex = FindColumnIndex(columns: columns, name: "PreTaxCost");
		var serviceIndex = FindColumnIndex(columns: columns, name: "ServiceName");
		var meterIndex = FindColumnIndex(columns: columns, name: "MeterName");
		var currencyIndex = FindColumnIndex(columns: columns, name: "Currency");

		List<ServiceUsage> services = [];

		foreach (JsonElement row in rows.EnumerateArray())
		{
			var i = 0;
			var cost = 0m;
			var serviceName = "";
			var meterName = "";
			var currency = "USD";
			foreach (JsonElement cell in row.EnumerateArray())
			{
				if (i == costIndex)
					cost = cell.ValueKind == JsonValueKind.Number ? cell.GetDecimal() : 0m;
				else if (i == serviceIndex)
					serviceName = cell.GetString() ?? "";
				else if (i == meterIndex)
					meterName = cell.GetString() ?? "";
				else if (i == currencyIndex && currencyIndex >= 0)
					currency = cell.GetString() ?? "USD";
				i++;
			}

			services.Add(
				new ServiceUsage(
					ServiceName: serviceName,
					Meter: meterName,
					Cost: cost,
					Currency: currency
				)
			);
		}

		services.Sort(
			(a, b) =>
			{
				var costCompare = b.Cost.CompareTo(value: a.Cost);
				return costCompare != 0
					? costCompare
					: Compare(
						strA: a.ServiceName,
						strB: b.ServiceName,
						comparisonType: OrdinalIgnoreCase
					);
			}
		);

		var totalCost = 0m;
		foreach (ServiceUsage s in services)
			totalCost += s.Cost;

		return new AzureUsageReport(
			SubscriptionId: subscriptionId,
			Services: services,
			TotalCost: totalCost,
			BillingPeriod: billingPeriod
		);
	}

	private static int FindColumnIndex(JsonElement columns, string name)
	{
		var index = 0;
		foreach (JsonElement col in columns.EnumerateArray())
		{
			if (
				col.GetProperty(propertyName: "name").GetString()?.EqualsIgnoreCase(other: name)
				?? false
			)
				return index;

			index++;
		}

		return -1;
	}
}
