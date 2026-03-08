namespace CSharpScripts.CLI.Mail;

internal sealed class MailDeleteCommand : AsyncCommand<MailDeleteCommand.Settings>
{
	public override async Task<int> ExecuteAsync(
		CommandContext context,
		Settings settings,
		CancellationToken cancellationToken
	)
	{
		ActiveMailbox? state = await MailStateManager.LoadAsync(cancellationToken);
		if (state is null)
		{
			UI.Error("No active mailbox found. Run 'tools mail create' first.");
			return 1;
		}

		UI.Info("Deleting mailbox {0}...", state.Address);

		ITempMailService service = TempMailServiceFactory.CreateForExisting(state);
		await service.DeleteMailboxAsync(state.Id, cancellationToken);

		MailStateManager.Delete();

		UI.Ok("Mailbox deleted.");
		Log.Information("MailDelete {Provider} {Address}", state.Provider, state.Address);

		return 0;
	}

	internal sealed class Settings : CommandSettings { }
}
