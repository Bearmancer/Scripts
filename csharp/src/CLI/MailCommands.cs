using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using CSharpScripts.Services.Mail;

namespace CSharpScripts.CLI;

/// <summary>
/// NAME
///   mail - Temporary email management
///
/// DESCRIPTION
///   Commands to create and manage temporary email addresses using the
///   Mail.tm service. Useful for one-time signups and testing.
///
/// COMMANDS
///   create    Create a new temporary email account
///   check     Check inbox for incoming messages
///   delete    Delete the temporary email account
///
/// EXAMPLES
///   cli mail create
///   cli mail check --wait 10
///   cli mail delete
/// </summary>
[Command("mail", Description = "Temporary email management")]
public sealed class MailGroupCommand : ICommand
{
    public ValueTask ExecuteAsync(IConsole console)
    {
        Console.Rule("Mail Commands");
        Console.NewLine();

        Console.MarkupLine("[blue bold]COMMANDS[/]");
        Console.MarkupLine("  [cyan]create[/]    Create a new temporary email account");
        Console.MarkupLine("  [cyan]check[/]     Check inbox for incoming messages");
        Console.MarkupLine("  [cyan]delete[/]    Delete the temporary email account");
        Console.NewLine();

        Console.MarkupLine("[blue bold]CHECK OPTIONS[/]");
        Console.MarkupLine("  [cyan]-w, --wait[/]       Polling interval in seconds");
        Console.MarkupLine(
            "  [cyan]-t, --timeout[/]    Max wait time in seconds [grey](default: 300)[/]"
        );
        Console.NewLine();

        Console.MarkupLine("[blue bold]EXAMPLES[/]");
        Console.MarkupLine("  [dim]$[/] cli mail create");
        Console.MarkupLine("  [dim]$[/] cli mail check");
        Console.MarkupLine("  [dim]$[/] cli mail check --wait 10 --timeout 120");
        Console.MarkupLine("  [dim]$[/] cli mail delete");

        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// NAME
///   mail create - Create temporary email
///
/// DESCRIPTION
///   Creates a new temporary email account using the Mail.tm service.
///   The account can be used to receive emails for testing or one-time
///   signups. Account credentials are stored locally for subsequent
///   check and delete operations.
///
/// USAGE
///   cli mail create
///
/// EXAMPLES
///   cli mail create
/// </summary>
[Command("mail create", Description = "Create temporary email")]
public sealed class MailCreateCommand : ICommand
{
    public async ValueTask ExecuteAsync(IConsole console)
    {
        Console.Info("Creating temporary email account...");

        MailTmService service = new();
        MailTmAccount account = await service.CreateAccountAsync();

        Console.NewLine();
        Console.Success("Account created!");
        Console.KeyValue("Address", account.Address);
        Console.KeyValue("ID", account.Id);
        Console.NewLine();
        Console.Tip("Use 'cli mail check' to check for incoming messages");
    }
}

/// <summary>
/// NAME
///   mail check - Check inbox for messages
///
/// DESCRIPTION
///   Checks the inbox of the current temporary email account for new
///   messages. Use --wait to poll for messages at the specified interval,
///   and --timeout to set a maximum wait time.
///
/// USAGE
///   cli mail check [options]
///
/// OPTIONS
///   -w, --wait        Polling interval in seconds (omit for single check)
///   -t, --timeout     Maximum wait time in seconds (default: 300)
///
/// EXAMPLES
///   cli mail check                       # Single check
///   cli mail check --wait 10             # Poll every 10 seconds
///   cli mail check --wait 5 --timeout 60 # Poll for max 60 seconds
/// </summary>
[Command("mail check", Description = "Check inbox for messages")]
public sealed class MailCheckCommand : ICommand
{
    [CommandOption("wait", 'w', Description = "Polling interval in seconds")]
    public int? WaitSeconds { get; init; }

    [CommandOption("timeout", 't', Description = "Max wait time in seconds")]
    public int TimeoutSeconds { get; init; } = 300;

    public async ValueTask ExecuteAsync(IConsole console)
    {
        Console.Info("Checking inbox...");

        MailTmService service = new();
        MailTmAccount account = await service.CreateAccountAsync();
        Console.KeyValue("Checking", account.Address);

        DateTime deadline = DateTime.UtcNow.AddSeconds(TimeoutSeconds);

        while (true)
        {
            List<MailTmMessage> messages = await service.GetInboxAsync();

            if (messages.Count > 0)
            {
                Console.NewLine();
                Console.Success("Found {0} message(s):", messages.Count);
                Console.NewLine();

                foreach (MailTmMessage msg in messages)
                {
                    Console.Rule(msg.Subject);
                    Console.KeyValue("From", msg.From?.Address ?? "Unknown");
                    Console.KeyValue("Date", msg.CreatedAt.ToString("yyyy/MM/dd HH:mm:ss"));
                    Console.KeyValue("ID", msg.Id);

                    MailTmMessage full = await service.ReadMessageAsync(msg.Id);
                    if (!IsNullOrWhiteSpace(full.Text))
                    {
                        Console.NewLine();
                        Console.Dim(full.Text.Length > 500 ? full.Text[..500] + "..." : full.Text);
                    }
                    Console.NewLine();
                }

                break;
            }

            if (!WaitSeconds.HasValue || DateTime.UtcNow >= deadline)
            {
                Console.Warning("No messages found.");
                break;
            }

            Console.Dim(
                $"No messages yet. Waiting {WaitSeconds}s... (timeout at {deadline:HH:mm:ss})"
            );
            await Task.Delay(TimeSpan.FromSeconds(WaitSeconds.Value));
        }
    }
}

/// <summary>
/// NAME
///   mail delete - Delete temporary email
///
/// DESCRIPTION
///   Deletes the current temporary email account and all associated
///   messages. The account cannot be recovered after deletion.
///
/// USAGE
///   cli mail delete
///
/// EXAMPLES
///   cli mail delete
/// </summary>
[Command("mail delete", Description = "Delete temporary email")]
public sealed class MailDeleteCommand : ICommand
{
    public async ValueTask ExecuteAsync(IConsole console)
    {
        Console.Info("Deleting account...");

        MailTmService service = new();
        MailTmAccount account = await service.CreateAccountAsync();
        Console.KeyValue("Account", account.Address);

        bool deleted = await service.DeleteAccountAsync();

        if (deleted)
            Console.Success("Account deleted.");
        else
            Console.Error("Failed to delete account.");
    }
}
