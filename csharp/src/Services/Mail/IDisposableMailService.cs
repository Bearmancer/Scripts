namespace CSharpScripts.Services.Mail;

public interface IDisposableMailService
{
    Task<MailAccount> CreateAccountAsync();
    Task<List<MailMessage>> GetInboxAsync();
    Task<MailMessage> ReadMessageAsync(string messageId);
    Task ForgetSessionAsync();
}
