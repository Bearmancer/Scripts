using Azure.Core;
using Azure.Identity;
using Spectre.Console;

namespace CSharpScripts.Core.Auth;

internal static class AzureCredentialManager
{
	private const string CognitiveServicesScope = "https://cognitiveservices.azure.com/.default";

	public static void EnsureCredentials()
	{
		try
		{
			// Check if we already have credentials loaded
			var credential = new DefaultAzureCredential(
				new DefaultAzureCredentialOptions
				{
					// Limit check duration and avoid interactive browser prompts during the check
					ExcludeInteractiveBrowserCredential = true,
				}
			);

			var context = new TokenRequestContext(scopes: [CognitiveServicesScope]);
			// Try to get token synchronously
			credential.GetToken(context);

			// If we got here, credentials work!
			return;
		}
		catch (Exception ex)
			when (ex is CredentialUnavailableException or AuthenticationFailedException)
		{
			// Credentials are not configured, prompt the user
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

			// Set the environment variables in the current process
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

