namespace CSharpScripts.CLI.Mail;

internal sealed class MailCreateCommand : AsyncCommand<MailCreateCommand.Settings>
{
	protected override async Task<int> ExecuteAsync(
		CommandContext context,
		Settings settings,
		CancellationToken cancellationToken
	)
	{
		var provider = settings.Provider;
		UI.Info("Creating temporary mailbox via {0}...", provider);

		ITempMailService service = TempMailServiceFactory.Create(provider);
		TempMailbox mailbox = await service.CreateMailboxAsync(cancellationToken);

		Dictionary<string, string> auth = service switch
		{
			MailTmService mailtm => mailtm.GetCredentials(),
			GuerrillaMailService gm => gm.GetCredentials(),
			SecMailService sec => sec.GetCredentials(),
			_ => [],
		};

		var state = new ActiveMailbox(
			mailbox.Id,
			mailbox.Address,
			mailbox.Provider,
			mailbox.CreatedAt,
			auth
		);
		await MailStateManager.SaveAsync(state, cancellationToken);

		UI.NewLine();
		UI.Ok("Mailbox created!");
		UI.KeyValue("Address", mailbox.Address);
		UI.KeyValue("Provider", mailbox.Provider);
		UI.KeyValue("ID", mailbox.Id);
		UI.NewLine();
		UI.Tip("Use 'tools mail check' to poll for incoming messages");
		Log.Information(
			"MailCreate {Provider} {Address} {Id}",
			mailbox.Provider,
			mailbox.Address,
			mailbox.Id
		);

		return 0;
	}

	internal sealed class Settings : CommandSettings
	{
		[CommandOption("-p|--provider")]
		[Description("Mail provider: mail.tm, guerrilla, 1secmail")]
		[DefaultValue("mail.tm")]
		public string Provider { get; init; } = "mail.tm";
	}
}
