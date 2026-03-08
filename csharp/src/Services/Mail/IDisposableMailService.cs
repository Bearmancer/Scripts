namespace CSharpScripts.Services.Mail;

internal interface ITempMailService
{
	string ProviderName { get; }
	Task<TempMailbox> CreateMailboxAsync(CancellationToken ct = default);
	Task<IReadOnlyList<TempEmail>> CheckInboxAsync(
		string mailboxId,
		CancellationToken ct = default
	);
	Task DeleteMailboxAsync(string mailboxId, CancellationToken ct = default);
	Task<TempEmail?> GetEmailAsync(
		string mailboxId,
		string emailId,
		CancellationToken ct = default
	);
}
