namespace CSharpScripts.CLI.Commands;

#region MailCreateCommand

public sealed class MailCreateCommand : AsyncCommand<MailCreateCommand.Settings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        Settings settings,
        CancellationToken cancellationToken
    )
    {
        Console.Info(message: "Creating temporary email account...");

        MailTmService service = new();
        var account = await service.CreateAccountAsync();

        Console.NewLine();
        Console.Success(message: "Account created!");
        Console.KeyValue(key: "Address", value: account.Address);
        Console.KeyValue(key: "ID", value: account.Id);
        Console.NewLine();
        Console.Tip(text: "Use 'cli mail check' to check for incoming messages");

        return 0;
    }

    public sealed class Settings : CommandSettings { }
}

#endregion

#region MailCheckCommand

public sealed class MailCheckCommand : AsyncCommand<MailCheckCommand.Settings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        Settings settings,
        CancellationToken cancellationToken
    )
    {
        Console.Info(message: "Checking inbox...");

        MailTmService service = new();
        var account = await service.CreateAccountAsync();
        Console.KeyValue(key: "Checking", value: account.Address);

        var deadline = DateTime.UtcNow.AddSeconds(value: settings.TimeoutSeconds);

        while (!cancellationToken.IsCancellationRequested)
        {
            var messages = await service.GetInboxAsync();

            if (messages.Count > 0)
            {
                Console.NewLine();
                Console.Success(message: "Found {0} message(s):", messages.Count);
                Console.NewLine();

                foreach (var msg in messages)
                {
                    Console.Rule(text: msg.Subject);
                    Console.KeyValue(key: "From", msg.From?.Address ?? "");
                    Console.KeyValue(
                        key: "Date",
                        msg.CreatedAt.ToString(format: "yyyy/MM/dd HH:mm:ss")
                    );
                    Console.KeyValue(key: "ID", value: msg.Id);

                    var full = await service.ReadMessageAsync(messageId: msg.Id);
                    if (!IsNullOrWhiteSpace(value: full.Text))
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
                Console.Warning(message: "No messages found.");
                break;
            }

            Console.Dim(
                $"No messages yet. Waiting {settings.WaitSeconds}s... (timeout at {deadline:HH:mm:ss})"
            );
            await Task.Delay(
                TimeSpan.FromSeconds(seconds: settings.WaitSeconds.Value),
                cancellationToken: cancellationToken
            );
        }

        return 0;
    }

    public sealed class Settings : CommandSettings
    {
        [CommandOption(template: "-w|--wait")]
        [Description(description: "Poll interval (sec)")]
        public int? WaitSeconds { get; init; }

        [CommandOption(template: "-t|--timeout")]
        [Description(description: "Max wait (sec)")]
        [DefaultValue(value: 300)]
        public int TimeoutSeconds { get; init; } = 300;
    }
}

#endregion

#region MailDeleteCommand

public sealed class MailDeleteCommand : AsyncCommand<MailDeleteCommand.Settings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        Settings settings,
        CancellationToken cancellationToken
    )
    {
        Console.Info(message: "Deleting account...");

        MailTmService service = new();
        var account = await service.CreateAccountAsync();
        Console.KeyValue(key: "Account", value: account.Address);

        bool deleted = await service.DeleteAccountAsync();

        if (deleted)
            Console.Success(message: "Account deleted.");
        else
            Console.Error(message: "Failed to delete account.");

        return deleted ? 0 : 1;
    }

    public sealed class Settings : CommandSettings { }
}

#endregion
