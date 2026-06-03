namespace Scripts.Models;

internal record AzureUsageReport(
	string SubscriptionId,
	IReadOnlyList<ServiceUsage> Services,
	decimal TotalCost,
	string BillingPeriod
);

internal record ServiceUsage(string ServiceName, string Meter, decimal Cost, string Currency);
