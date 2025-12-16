namespace CSharpScripts.CLI.Commands;

public sealed class MailCreateCommand : AsyncCommand<MailCreateCommand.Settings>
{
    public sealed class Settings : CommandSettings { }

    public override async Task<int> ExecuteAsync(
        CommandContext context,
        Settings settings,
        CancellationToken cancellationToken
    )
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

        return 0;
    }
}

public sealed class MailCheckCommand : AsyncCommand<MailCheckCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandOption("-w|--wait")]
        [Description("Poll interval (sec)")]
        public int? WaitSeconds { get; init; }

        [CommandOption("-t|--timeout")]
        [Description("Max wait (sec)")]
        [DefaultValue(300)]
        public int TimeoutSeconds { get; init; } = 300;
    }

    public override async Task<int> ExecuteAsync(
        CommandContext context,
        Settings settings,
        CancellationToken cancellationToken
    )
    {
        Console.Info("Checking inbox...");

        MailTmService service = new();
        MailTmAccount account = await service.CreateAccountAsync();
        Console.KeyValue("Checking", account.Address);

        DateTime deadline = DateTime.UtcNow.AddSeconds(settings.TimeoutSeconds);

        while (!cancellationToken.IsCancellationRequested)
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
                    Console.KeyValue("From", msg.From?.Address ?? "");
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

            if (!settings.WaitSeconds.HasValue || DateTime.UtcNow >= deadline)
            {
                Console.Warning("No messages found.");
                break;
            }

            Console.Dim(
                $"No messages yet. Waiting {settings.WaitSeconds}s... (timeout at {deadline:HH:mm:ss})"
            );
            await Task.Delay(TimeSpan.FromSeconds(settings.WaitSeconds.Value), cancellationToken);
        }

        return 0;
    }
}

public sealed class MailDeleteCommand : AsyncCommand<MailDeleteCommand.Settings>
{
    public sealed class Settings : CommandSettings { }

    public override async Task<int> ExecuteAsync(
        CommandContext context,
        Settings settings,
        CancellationToken cancellationToken
    )
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

        return deleted ? 0 : 1;
    }
}
