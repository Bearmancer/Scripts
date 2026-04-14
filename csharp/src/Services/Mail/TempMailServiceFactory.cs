namespace CSharpScripts.Services.Mail;

internal static class TempMailServiceFactory
{
	internal static IReadOnlyList<string> AvailableProviders =>
		["mail.tm", "guerrilla", "1secmail"];

	internal static ITempMailService Create(string provider) =>
		provider.EqualsIgnoreCase("mailtm") || provider.EqualsIgnoreCase("mail.tm")
			? MailTmService.Create()
		: provider.EqualsIgnoreCase("guerrilla") ? GuerrillaMailService.Create()
		: provider.EqualsIgnoreCase("1secmail") || provider.EqualsIgnoreCase("secmail")
			? SecMailService.Create()
		: throw new ArgumentException(
			$"Unknown provider: {provider}. Available: {Join(separator: ", ", values: AvailableProviders)}"
		);

	internal static ITempMailService CreateForExisting(ActiveMailbox mailbox)
	{
		Dictionary<string, string> auth = mailbox.Auth ?? [];

		return mailbox.Provider.EqualsIgnoreCase("mail.tm")
				? MailTmService.CreateForExisting(
					CollectionExtensions.GetValueOrDefault(
						auth,
						key: "address",
						defaultValue: mailbox.Address
					),
					CollectionExtensions.GetValueOrDefault(auth, key: "password", defaultValue: "")
				)
			: mailbox.Provider.EqualsIgnoreCase("guerrilla")
				? GuerrillaMailService.CreateForExisting(
					CollectionExtensions.GetValueOrDefault(auth, key: "sidToken", defaultValue: ""),
					emailAddress: mailbox.Address,
					int.TryParse(
						CollectionExtensions.GetValueOrDefault(auth, key: "seq", defaultValue: "0"),
						out var seq
					)
						? seq
						: 0
				)
			: mailbox.Provider.EqualsIgnoreCase("1secmail")
				? SecMailService.CreateForExisting(
					CollectionExtensions.GetValueOrDefault(auth, key: "login", defaultValue: ""),
					CollectionExtensions.GetValueOrDefault(auth, key: "domain", defaultValue: "")
				)
			: throw new ArgumentException($"Unknown provider: {mailbox.Provider}");
	}
}
