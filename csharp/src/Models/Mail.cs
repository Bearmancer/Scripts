namespace CSharpScripts.Models;

internal sealed record MailAccount(string Address, DateTime CreatedAt);

internal sealed record MailMessage(
	string Id,
	string From,
	string Subject,
	string Body,
	DateTime ReceivedAt,
	bool IsRead
);

internal record TempMailbox(string Id, string Address, string Provider, DateTime CreatedAt);

internal record TempEmail(
	string Id,
	string From,
	string Subject,
	string Body,
	DateTime ReceivedAt,
	bool IsHtml
);

internal record ActiveMailbox(
	string Id,
	string Address,
	string Provider,
	DateTime CreatedAt,
	Dictionary<string, string>? Auth = null
);


