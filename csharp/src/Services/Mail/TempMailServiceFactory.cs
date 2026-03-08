namespace CSharpScripts.Services.Mail;

internal static class TempMailServiceFactory
{
	internal static IReadOnlyList<string> AvailableProviders =>
		["mail.tm", "guerrilla", "1secmail"];

	internal static ITempMailService Create(string provider) =>
		provider.ToLowerInvariant() switch
		{
			"mailtm" or "mail.tm" => MailTmService.Create(),
			"guerrilla" => GuerrillaMailService.Create(),
			"1secmail" or "secmail" => SecMailService.Create(),
			_ => throw new ArgumentException(
				$"Unknown provider: {provider}. Available: {Join(", ", AvailableProviders)}"
			),
		};

	internal static ITempMailService CreateForExisting(ActiveMailbox mailbox)
	{
		Dictionary<string, string> auth = mailbox.Auth ?? [];

		return mailbox.Provider.ToLowerInvariant() switch
		{
			"mail.tm" => MailTmService.CreateForExisting(
				auth.GetValueOrDefault("address", mailbox.Address),
				auth.GetValueOrDefault("password", "")
			),
			"guerrilla" => GuerrillaMailService.CreateForExisting(
				auth.GetValueOrDefault("sidToken", ""),
				mailbox.Address,
				int.TryParse(auth.GetValueOrDefault("seq", "0"), out var seq) ? seq : 0
			),
			"1secmail" => SecMailService.CreateForExisting(
				auth.GetValueOrDefault("login", ""),
				auth.GetValueOrDefault("domain", "")
			),
			_ => throw new ArgumentException($"Unknown provider: {mailbox.Provider}"),
		};
	}
}
