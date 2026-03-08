namespace CSharpScripts.CLI.Cloud;

using Azure.Identity;
using CSharpScripts.Services.Cloud;

internal sealed class CloudUsageCommand : BaseAsyncCommand<CloudUsageCommand.Settings>
{
	internal sealed class Settings : CommandSettings { }

	public override async Task<int> ExecuteAsync(
		CommandContext context,
		Settings settings,
		CancellationToken cancellationToken
	)
	{
		return await ExecuteWithErrorHandlingAsync(
			ServiceType.Cloud,
			async () =>
			{
				if (!CloudUsageService.IsConfigured)
				{
					UI.Warn("AZURE_SUBSCRIPTION_ID is not set.");
					UI.Info("To configure Azure access:");
					UI.Info("  1. Run: az login");
					UI.Info("  2. Set: $env:AZURE_SUBSCRIPTION_ID = '<your-subscription-id>'");
					UI.Info(
						"  3. Find your subscription ID with: az account show --query id -o tsv"
					);
					return;
				}

				UI.Info("Fetching Azure usage for current billing period...");

				AzureUsageReport report;
				try
				{
					report = await CloudUsageService.GetAzureUsageAsync(cancellationToken);
				}
				catch (CredentialUnavailableException ex)
				{
					UI.Error("Azure credentials not available: {0}", ex.Message);
					UI.Info("Run 'az login' to authenticate, then retry.");
					return;
				}
				catch (AuthenticationFailedException ex)
				{
					UI.Error("Azure authentication failed: {0}", ex.Message);
					UI.Info("Run 'az login' to refresh credentials.");
					return;
				}

				RenderReport(report);
			}
		);
	}

	private static void RenderReport(AzureUsageReport report)
	{
		AnsiConsole.WriteLine();
		AnsiConsole.MarkupLine($"[bold]Azure Usage — {Markup.Escape(report.BillingPeriod)}[/]");
		AnsiConsole.MarkupLine($"[dim]Subscription: {Markup.Escape(report.SubscriptionId)}[/]");
		AnsiConsole.WriteLine();

		if (report.Services.Count == 0)
		{
			UI.Info("No usage data found for the current billing period.");
			return;
		}

		SpectreTable table = new();
		table.Border(TableBorder.Rounded);
		table.AddColumn(new TableColumn("[bold]Service[/]").LeftAligned());
		table.AddColumn(new TableColumn("[bold]Meter[/]").LeftAligned());
		table.AddColumn(new TableColumn("[bold]Cost[/]").RightAligned());
		table.AddColumn(new TableColumn("[bold]Free Tier[/]").Centered());

		foreach (ServiceUsage usage in report.Services)
		{
			var isFree = usage.Cost == 0m;
			var costDisplay = isFree
				? "[green]$0.00[/]"
				: $"[yellow]{usage.Currency} {usage.Cost:F4}[/]";
			var freeTierDisplay = isFree ? "[green]✔[/]" : "[dim]—[/]";

			table.AddRow(
				Markup.Escape(usage.ServiceName),
				Markup.Escape(usage.Meter),
				costDisplay,
				freeTierDisplay
			);
		}

		AnsiConsole.Write(table);

		var currency = report.Services.Count > 0 ? report.Services[0].Currency : "USD";
		var totalFormatted =
			report.TotalCost == 0m
				? "[green]$0.00[/]"
				: $"[yellow]{currency} {report.TotalCost:F4}[/]";

		AnsiConsole.MarkupLine($"[bold]Total: {totalFormatted}[/]");
		AnsiConsole.WriteLine();
	}
}
