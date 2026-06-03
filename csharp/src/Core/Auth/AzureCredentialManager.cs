using Azure.Core;
using Azure.Identity;
using Spectre.Console;

namespace Scripts.Core.Auth;

internal static class AzureCredentialManager
{
	private const string CognitiveServicesScope = "https://cognitiveservices.azure.com/.default";

	public static void EnsureCredentials()
	{
		try
		{
			var credential = new DefaultAzureCredential(
				new DefaultAzureCredentialOptions { ExcludeInteractiveBrowserCredential = true }
			);

			var context = new TokenRequestContext(scopes: [CognitiveServicesScope]);
			credential.GetToken(context);

			return;
		}
		catch (Exception ex)
			when (ex is CredentialUnavailableException or AuthenticationFailedException)
		{
			AnsiConsole.MarkupLine("[yellow]Azure credentials are not configured or invalid.[/]");
			AnsiConsole.MarkupLine("[blue]Please configure a Service Principal to proceed:[/]");

			var clientId = AnsiConsole.Prompt(
				new TextPrompt<string>("Enter [green]AZURE_CLIENT_ID[/]:")
					.PromptStyle("green")
					.ValidationErrorMessage("[red]Client ID cannot be empty.[/]")
					.Validate(val => !string.IsNullOrWhiteSpace(val))
			);

			var clientSecret = AnsiConsole.Prompt(
				new TextPrompt<string>("Enter [green]AZURE_CLIENT_SECRET[/]:")
					.PromptStyle("green")
					.Secret()
					.ValidationErrorMessage("[red]Client Secret cannot be empty.[/]")
					.Validate(val => !string.IsNullOrWhiteSpace(val))
			);

			var tenantId = AnsiConsole.Prompt(
				new TextPrompt<string>("Enter [green]AZURE_TENANT_ID[/]:")
					.PromptStyle("green")
					.ValidationErrorMessage("[red]Tenant ID cannot be empty.[/]")
					.Validate(val => !string.IsNullOrWhiteSpace(val))
			);

			SetEnvironmentVariable("AZURE_CLIENT_ID", clientId, EnvironmentVariableTarget.Process);
			SetEnvironmentVariable(
				"AZURE_CLIENT_SECRET",
				clientSecret,
				EnvironmentVariableTarget.Process
			);
			SetEnvironmentVariable("AZURE_TENANT_ID", tenantId, EnvironmentVariableTarget.Process);

			AnsiConsole.MarkupLine(
				"[green]Process environment variables populated successfully.[/]"
			);
		}
	}
}
