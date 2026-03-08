namespace CSharpScripts.CLI.Mail;

internal sealed class MailCheckCommand : AsyncCommand<MailCheckCommand.Settings>
{
	public override async Task<int> ExecuteAsync(
		CommandContext context,
		Settings settings,
		CancellationToken cancellationToken
	)
	{
		ActiveMailbox? state = await LoadMailboxStateAsync(settings.MailboxId, cancellationToken);
		if (state is null)
			return 1;

		ITempMailService service = TempMailServiceFactory.CreateForExisting(state);
		UI.Info("Checking inbox for {0}...", state.Address);

		if (!settings.Watch)
		{
			IReadOnlyList<TempEmail> emails = await service.CheckInboxAsync(
				state.Id,
				cancellationToken
			);
			RenderEmails(emails, state.Address);
			return 0;
		}

		UI.Info("Watch mode active — polling every 5s (Ctrl+C to stop)");
		HashSet<string> seenIds = [];

		while (!cancellationToken.IsCancellationRequested)
		{
			cancellationToken.ThrowIfCancellationRequested();
			IReadOnlyList<TempEmail> emails = await service.CheckInboxAsync(
				state.Id,
				cancellationToken
			);
			var newEmails = emails.Where(e => seenIds.Add(e.Id)).ToList();

			if (newEmails.Count > 0)
			{
				UI.NewLine();
				UI.Ok("New message(s) arrived!");
				RenderEmails(newEmails, state.Address);
				Log.Information("MailCheck_NewMessages {Count}", newEmails.Count);
			}

			await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
		}

		return 0;
	}

	private static async Task<ActiveMailbox?> LoadMailboxStateAsync(
		string? mailboxId,
		CancellationToken ct
	)
	{
		if (!IsNullOrEmpty(mailboxId))
		{
			UI.Warn("No provider known for bare mailbox ID — use stored state or re-create.");
			return null;
		}

		ActiveMailbox? state = await MailStateManager.LoadAsync(ct);
		if (state is null)
		{
			UI.Error("No active mailbox found. Run 'tools mail create' first.");
			return null;
		}

		return state;
	}

	private static void RenderEmails(IReadOnlyList<TempEmail> emails, string address)
	{
		if (emails.Count == 0)
		{
			UI.Warn("No messages in {0}", address);
			return;
		}

		UI.NewLine();
		SpectreTable table = new SpectreTable()
			.Border(TableBorder.Rounded)
			.AddColumn("#")
			.AddColumn("From")
			.AddColumn("Subject")
			.AddColumn("Received");

		for (var i = 0; i < emails.Count; i++)
		{
			TempEmail e = emails[i];
			table.AddRow(
				(i + 1).ToString(),
				Markup.Escape(e.From),
				Markup.Escape(e.Subject),
				e.ReceivedAt.ToLocalTime().ToString("yyyy/MM/dd HH:mm:ss")
			);
		}

		AnsiConsole.Write(table);
		Log.Information("MailCheck {Address} {Count} messages", address, emails.Count);
	}

	internal sealed class Settings : CommandSettings
	{
		[CommandArgument(0, "[mailbox-id]")]
		[Description("Mailbox ID (uses active mailbox from state if omitted)")]
		public string? MailboxId { get; init; }

		[CommandOption("-w|--watch")]
		[Description("Poll every 5 seconds for new messages")]
		public bool Watch { get; init; }
	}
}
