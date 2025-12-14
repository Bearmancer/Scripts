namespace CSharpScripts.Models;

public record MailAccount(string Address, DateTime CreatedAt);

public record MailMessage(
    string Id,
    string From,
    string Subject,
    string Body,
    DateTime ReceivedAt,
    bool IsRead
);
