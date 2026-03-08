namespace CSharpScripts.Services.Mail;

internal sealed class SecMailException : Exception
{
	internal SecMailException()
		: base() { }

	internal SecMailException(string message)
		: base(message) { }

	internal SecMailException(string message, Exception? inner)
		: base(message, inner) { }
}

internal sealed class SecMailService : ITempMailService
{
	private const string BaseUrl = "https://www.1secmail.com/api/v1/";

	private static readonly HttpClient Http = new()
	{
		DefaultRequestHeaders = { { "User-Agent", "Mozilla/5.0 (compatible; CSharpScripts/1.0)" } },
	};

	private string? _login;
	private string? _domain;

	private SecMailService() { }

	private SecMailService(string login, string domain)
	{
		_login = login;
		_domain = domain;
	}

	internal static SecMailService Create() => new();

	internal static SecMailService CreateForExisting(string login, string domain) =>
		new(login, domain);

	public string ProviderName => "1secmail";

	internal Dictionary<string, string> GetCredentials() =>
		new() { ["login"] = _login ?? "", ["domain"] = _domain ?? "" };

	public async Task<TempMailbox> CreateMailboxAsync(CancellationToken ct = default)
	{
		Log.Debug("SecMailService.CreateMailboxAsync entry");

		var url = new Uri($"{BaseUrl}?action=genRandomMailbox&count=1");
		HttpResponseMessage response = await Http.GetAsync(url, ct);
		response.EnsureSuccessStatusCode();

		var json = await response.Content.ReadAsStringAsync(ct);
		using var doc = JsonDocument.Parse(json);
		JsonElement root = doc.RootElement;

		if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() == 0)
			throw new SecMailException("No mailbox returned from 1secmail API");

		var address =
			root[0].GetString() ?? throw new SecMailException("Empty address in 1secmail response");

		var atIndex = address.IndexOf('@', OrdinalIgnoreCase);
		if (atIndex < 0)
			throw new SecMailException($"Invalid address format from 1secmail: {address}");

		_login = address[..atIndex];
		_domain = address[(atIndex + 1)..];

		Log.Information("SecMailService created mailbox {Address}", address);
		return new TempMailbox(address, address, ProviderName, DateTime.UtcNow);
	}

	public async Task<IReadOnlyList<TempEmail>> CheckInboxAsync(
		string mailboxId,
		CancellationToken ct = default
	)
	{
		Log.Debug("SecMailService.CheckInboxAsync entry {MailboxId}", mailboxId);
		ResolveLoginDomain(mailboxId);

		var url = new Uri($"{BaseUrl}?action=getMessages&login={_login}&domain={_domain}");
		HttpResponseMessage response = await Http.GetAsync(url, ct);
		response.EnsureSuccessStatusCode();

		using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
		JsonElement root = doc.RootElement;

		if (root.ValueKind != JsonValueKind.Array)
			return [];

		List<TempEmail> emails = [];
		foreach (JsonElement item in root.EnumerateArray())
		{
			var msgId = item.TryGetProperty("id", out JsonElement idEl)
				? idEl.GetInt32().ToString()
				: "";
			var from = item.TryGetProperty("from", out JsonElement mf)
				? mf.GetString() ?? "unknown"
				: "unknown";
			var subject = item.TryGetProperty("subject", out JsonElement ms)
				? ms.GetString() ?? ""
				: "";
			DateTime receivedAt =
				item.TryGetProperty("date", out JsonElement dt)
				&& DateTime.TryParse(dt.GetString(), out DateTime parsed)
					? parsed.ToUniversalTime()
					: DateTime.UtcNow;

			emails.Add(new TempEmail(msgId, from, subject, Body: "", receivedAt, IsHtml: false));
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
		ResolveLoginDomain(mailboxId);

		var url = new Uri(
			$"{BaseUrl}?action=readMessage&login={_login}&domain={_domain}&id={emailId}"
		);
		HttpResponseMessage response = await Http.GetAsync(url, ct);

		if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
			return null;

		response.EnsureSuccessStatusCode();

		using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
		JsonElement root = doc.RootElement;

		var from = root.TryGetProperty("from", out JsonElement mf)
			? mf.GetString() ?? "unknown"
			: "unknown";
		var subject = root.TryGetProperty("subject", out JsonElement ms)
			? ms.GetString() ?? ""
			: "";
		var bodyHtml = root.TryGetProperty("htmlBody", out JsonElement hb)
			? hb.GetString() ?? ""
			: "";
		var bodyText = root.TryGetProperty("textBody", out JsonElement tb)
			? tb.GetString() ?? ""
			: "";
		var isHtml = !IsNullOrEmpty(bodyHtml);
		DateTime receivedAt =
			root.TryGetProperty("date", out JsonElement dt)
			&& DateTime.TryParse(dt.GetString(), out DateTime parsed)
				? parsed.ToUniversalTime()
				: DateTime.UtcNow;

		return new TempEmail(
			emailId,
			from,
			subject,
			isHtml ? bodyHtml : bodyText,
			receivedAt,
			isHtml
		);
	}

	private void ResolveLoginDomain(string mailboxId)
	{
		if (!IsNullOrEmpty(_login) && !IsNullOrEmpty(_domain))
			return;

		var atIndex = mailboxId.IndexOf('@', OrdinalIgnoreCase);
		if (atIndex < 0)
			throw new SecMailException($"Cannot resolve login/domain from mailboxId: {mailboxId}");

		_login = mailboxId[..atIndex];
		_domain = mailboxId[(atIndex + 1)..];
	}
}
