using Azure.Identity;
using CSharpScripts.Services.Cloud;

namespace CSharpScripts.CLI.Cloud;

internal sealed class CloudUsageCommand : BaseAsyncCommand<CloudUsageCommand.Settings>
{
	protected async override Task<int> ExecuteAsync(
		CommandContext context,
		Settings settings,
		CancellationToken cancellationToken
	)
	{
		return await ExecuteWithErrorHandlingAsync(
			service: ServiceType.Cloud,
			async () =>
			{
				if (!CloudUsageService.IsConfigured)
				{
					Ui.Warn(message: "AZURE_SUBSCRIPTION_ID is not set.");
					Ui.Info(message: "To configure Azure access:");
					Ui.Info(message: "  1. Run: az login");
					Ui.Info(message: "  2. Set: $env:AZURE_SUBSCRIPTION_ID = '<your-subscription-id>'");
					Ui.Info(
						message: "  3. Find your subscription ID with: az account show --query id -o tsv"
					);
					return;
				}

				Ui.Info(message: "Fetching Azure usage for current billing period...");

				AzureUsageReport report;
				try
				{
					report = await CloudUsageService.GetAzureUsageAsync(ct: cancellationToken);
				}
				catch (CredentialUnavailableException ex)
				{
					Ui.Error(message: "Azure credentials not available: {0}", ex.Message);
					Ui.Info(message: "Run 'az login' to authenticate, then retry.");
					return;
				}
				catch (AuthenticationFailedException ex)
				{
					Ui.Error(message: "Azure authentication failed: {0}", ex.Message);
					Ui.Info(message: "Run 'az login' to refresh credentials.");
					return;
				}

				RenderReport(report: report);
			}
		);
	}

	private static void RenderReport(AzureUsageReport report)
	{
		AnsiConsole.WriteLine();
		AnsiConsole.MarkupLine($"[bold]Azure Usage — {Markup.Escape(text: report.BillingPeriod)}[/]");
		AnsiConsole.MarkupLine($"[dim]Subscription: {Markup.Escape(text: report.SubscriptionId)}[/]");
		AnsiConsole.WriteLine();

		if (report.Services.Count == 0)
		{
			Ui.Info(message: "No usage data found for the current billing period.");
			return;
		}

		SpectreTable table = new();
		table.Border(border: TableBorder.Rounded);
		table.AddColumn(new TableColumn(header: "[bold]Service[/]").LeftAligned());
		table.AddColumn(new TableColumn(header: "[bold]Meter[/]").LeftAligned());
		table.AddColumn(new TableColumn(header: "[bold]Cost[/]").RightAligned());
		table.AddColumn(new TableColumn(header: "[bold]Free Tier[/]").Centered());

		foreach (ServiceUsage usage in report.Services)
		{
			var isFree = usage.Cost == 0m;
			var costDisplay = isFree
				? "[green]$0.00[/]"
				: $"[yellow]{usage.Currency} {usage.Cost:F4}[/]";
			var freeTierDisplay = isFree ? "[green]✔[/]" : "[dim]—[/]";

			table.AddRow(Markup.Escape(text: usage.ServiceName),
				Markup.Escape(text: usage.Meter),
				costDisplay,
				freeTierDisplay
			);
		}

		AnsiConsole.Write(renderable: table);

		var currency = report.Services.Count > 0 ? report.Services[index: 0].Currency : "USD";
		var totalFormatted =
			report.TotalCost == 0m
				? "[green]$0.00[/]"
				: $"[yellow]{currency} {report.TotalCost:F4}[/]";

		AnsiConsole.MarkupLine($"[bold]Total: {totalFormatted}[/]");
		AnsiConsole.WriteLine();
	}

	internal sealed class Settings : CommandSettings
	{
	}
}
