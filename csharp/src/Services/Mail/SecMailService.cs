using System.Net;

namespace CSharpScripts.Services.Mail;

internal sealed class SecMailException : Exception
{
	internal SecMailException() { }

	internal SecMailException(string message)
		: base(message: message) { }

	internal SecMailException(string message, Exception? inner)
		: base(message: message, innerException: inner) { }
}

internal sealed class SecMailService : ITempMailService
{
	private const string BaseUrl = "https://www.1secmail.com/api/v1/";

	private static readonly HttpClient Http = new()
	{
		DefaultRequestHeaders = { { "User-Agent", "Mozilla/5.0 (compatible; CSharpScripts/1.0)" } },
	};

	private string? _domain;

	private string? _login;

	private SecMailService() { }

	private SecMailService(string login, string domain)
	{
		_login = login;
		_domain = domain;
	}

	public string ProviderName => "1secmail";

	public async Task<TempMailbox> CreateMailboxAsync(CancellationToken ct = default)
	{
		Log.Debug("SecMailService.CreateMailboxAsync entry");

		var url = new Uri($"{BaseUrl}?action=genRandomMailbox&count=1");
		HttpResponseMessage response = await Http.GetAsync(requestUri: url, ct);
		response.EnsureSuccessStatusCode();

		var json = await response.Content.ReadAsStringAsync(ct);
		using var doc = JsonDocument.Parse(json: json);
		JsonElement root = doc.RootElement;

		if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() == 0)
			throw new SecMailException(message: "No mailbox returned from 1secmail API");

		var address =
			root[index: 0].GetString()
			?? throw new SecMailException(message: "Empty address in 1secmail response");

		var atIndex = address.IndexOf(value: '@', comparisonType: OrdinalIgnoreCase);
		if (atIndex < 0)
			throw new SecMailException($"Invalid address format from 1secmail: {address}");

		_login = address[..atIndex];
		_domain = address[(atIndex + 1)..];

		Log.Information("SecMailService created mailbox {Address}", address);
		return new TempMailbox(
			Id: address,
			Address: address,
			Provider: ProviderName,
			CreatedAt: DateTime.UtcNow
		);
	}

	public async Task<IReadOnlyList<TempEmail>> CheckInboxAsync(
		string mailboxId,
		CancellationToken ct = default
	)
	{
		Log.Debug("SecMailService.CheckInboxAsync entry {MailboxId}", mailboxId);
		ResolveLoginDomain(mailboxId: mailboxId);

		var url = new Uri($"{BaseUrl}?action=getMessages&login={_login}&domain={_domain}");
		HttpResponseMessage response = await Http.GetAsync(requestUri: url, ct);
		response.EnsureSuccessStatusCode();

		using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
		JsonElement root = doc.RootElement;

		if (root.ValueKind != JsonValueKind.Array)
			return [];

		List<TempEmail> emails = [];
		foreach (JsonElement item in root.EnumerateArray())
		{
			var msgId = item.TryGetProperty(propertyName: "id", out JsonElement idEl)
				? idEl.GetInt32().ToString()
				: "";
			var from = item.TryGetProperty(propertyName: "from", out JsonElement mf)
				? mf.GetString() ?? "unknown"
				: "unknown";
			var subject = item.TryGetProperty(propertyName: "subject", out JsonElement ms)
				? ms.GetString() ?? ""
				: "";
			DateTime receivedAt =
				item.TryGetProperty(propertyName: "date", out JsonElement dt)
				&& DateTime.TryParse(dt.GetString(), out DateTime parsed)
					? parsed.ToUniversalTime()
					: DateTime.UtcNow;

			emails.Add(
				new TempEmail(
					Id: msgId,
					From: from,
					Subject: subject,
					Body: "",
					ReceivedAt: receivedAt,
					IsHtml: false
				)
			);
		}

		Log.Information("SecMailService found {Count} messages", emails.Count);
		return emails;
	}

	public Task DeleteMailboxAsync(string mailboxId, CancellationToken ct = default)
	{
		Log.Information(
			"SecMailService: 1secmail does not support deletion — mailbox expires automatically"
		);
		_login = null;
		_domain = null;
		return Task.CompletedTask;
	}

	public async Task<TempEmail?> GetEmailAsync(
		string mailboxId,
		string emailId,
		CancellationToken ct = default
	)
	{
		Log.Debug("SecMailService.GetEmailAsync entry {EmailId}", emailId);
		ResolveLoginDomain(mailboxId: mailboxId);

		var url = new Uri(
			$"{BaseUrl}?action=readMessage&login={_login}&domain={_domain}&id={emailId}"
		);
		HttpResponseMessage response = await Http.GetAsync(requestUri: url, ct);

		if (response.StatusCode == HttpStatusCode.NotFound)
			return null;

		response.EnsureSuccessStatusCode();

		using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
		JsonElement root = doc.RootElement;

		var from = root.TryGetProperty(propertyName: "from", out JsonElement mf)
			? mf.GetString() ?? "unknown"
			: "unknown";
		var subject = root.TryGetProperty(propertyName: "subject", out JsonElement ms)
			? ms.GetString() ?? ""
			: "";
		var bodyHtml = root.TryGetProperty(propertyName: "htmlBody", out JsonElement hb)
			? hb.GetString() ?? ""
			: "";
		var bodyText = root.TryGetProperty(propertyName: "textBody", out JsonElement tb)
			? tb.GetString() ?? ""
			: "";
		var isHtml = !IsNullOrEmpty(value: bodyHtml);
		DateTime receivedAt =
			root.TryGetProperty(propertyName: "date", out JsonElement dt)
			&& DateTime.TryParse(dt.GetString(), out DateTime parsed)
				? parsed.ToUniversalTime()
				: DateTime.UtcNow;

		return new TempEmail(
			Id: emailId,
			From: from,
			Subject: subject,
			isHtml ? bodyHtml : bodyText,
			ReceivedAt: receivedAt,
			IsHtml: isHtml
		);
	}

	internal static SecMailService Create() => new();

	internal static SecMailService CreateForExisting(string login, string domain) =>
		new(login: login, domain: domain);

	internal Dictionary<string, string> GetCredentials() =>
		new() { [key: "login"] = _login ?? "", [key: "domain"] = _domain ?? "" };

	private void ResolveLoginDomain(string mailboxId)
	{
		if (!IsNullOrEmpty(value: _login) && !IsNullOrEmpty(value: _domain))
			return;

		var atIndex = mailboxId.IndexOf(value: '@', comparisonType: OrdinalIgnoreCase);
		if (atIndex < 0)
			throw new SecMailException($"Cannot resolve login/domain from mailboxId: {mailboxId}");

		_login = mailboxId[..atIndex];
		_domain = mailboxId[(atIndex + 1)..];
	}
}
