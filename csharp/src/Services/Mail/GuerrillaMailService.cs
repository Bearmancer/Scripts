using System.Web;

namespace CSharpScripts.Services.Mail;

public sealed class GuerrillaMailException : Exception
{
	internal GuerrillaMailException() { }

	internal GuerrillaMailException(string message)
		: base(message: message) { }

	internal GuerrillaMailException(string message, Exception? inner)
		: base(message: message, innerException: inner) { }
}

internal sealed class GuerrillaMailService : ITempMailService
{
	private const string BaseUrl = "https://www.guerrillamail.com/ajax.php";

	private static readonly HttpClient Http = new()
	{
		DefaultRequestHeaders = { { "User-Agent", "Mozilla/5.0 (compatible; CSharpScripts/1.0)" } },
	};

	private string? _emailAddress;
	private int _seq;

	private string? _sidToken;

	private GuerrillaMailService() { }

	private GuerrillaMailService(string sidToken, string emailAddress, int seq)
	{
		_sidToken = sidToken;
		_emailAddress = emailAddress;
		_seq = seq;
	}

	public string ProviderName => "guerrilla";

	public async Task<TempMailbox> CreateMailboxAsync(CancellationToken ct = default)
	{
		Log.Debug("GuerrillaMailService.CreateMailboxAsync entry");

		var url = new Uri(
			$"{BaseUrl}?f=get_email_address&lang=en&t={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}"
		);
		HttpResponseMessage response = await Http.GetAsync(requestUri: url, ct);
		response.EnsureSuccessStatusCode();

		using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
		JsonElement root = doc.RootElement;

		_emailAddress =
			root.GetProperty(propertyName: "email_addr").GetString()
			?? throw new GuerrillaMailException(message: "No email address in response");
		_sidToken =
			root.GetProperty(propertyName: "sid_token").GetString()
			?? throw new GuerrillaMailException(message: "No sid_token in response");
		_seq = root.TryGetProperty(propertyName: "seq", out JsonElement seqEl)
			? seqEl.GetInt32()
			: 0;

		var id = _emailAddress;
		Log.Information("GuerrillaMailService created mailbox {Address}", _emailAddress);
		return new TempMailbox(
			Id: id,
			Address: _emailAddress,
			Provider: ProviderName,
			CreatedAt: DateTime.UtcNow
		);
	}

	public async Task<IReadOnlyList<TempEmail>> CheckInboxAsync(
		string mailboxId,
		CancellationToken ct = default
	)
	{
		Log.Debug("GuerrillaMailService.CheckInboxAsync entry {MailboxId}", mailboxId);

		if (IsNullOrEmpty(value: _sidToken))
		{
			throw new GuerrillaMailException(
				message: "No session. Call CreateMailboxAsync or provide credentials."
			);
		}

		var url = new Uri(
			$"{BaseUrl}?f=check_email&seq={_seq}&sid_token={HttpUtility.UrlEncode(str: _sidToken)}"
		);
		HttpResponseMessage response = await Http.GetAsync(requestUri: url, ct);
		response.EnsureSuccessStatusCode();

		using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
		JsonElement root = doc.RootElement;

		if (root.TryGetProperty(propertyName: "sid_token", out JsonElement newToken))
			_sidToken = newToken.GetString() ?? _sidToken;

		if (!root.TryGetProperty(propertyName: "list", out JsonElement list))
			return [];

		List<TempEmail> emails = [];
		foreach (JsonElement item in list.EnumerateArray())
		{
			var msgId = item.TryGetProperty(propertyName: "mail_id", out JsonElement mid)
				? mid.GetString() ?? ""
				: "";
			if (IsNullOrEmpty(value: msgId))
				continue;

			var from = item.TryGetProperty(propertyName: "mail_from", out JsonElement mf)
				? mf.GetString() ?? "unknown"
				: "unknown";
			var subject = item.TryGetProperty(propertyName: "mail_subject", out JsonElement ms)
				? ms.GetString() ?? ""
				: "";
			var body = item.TryGetProperty(propertyName: "mail_excerpt", out JsonElement me)
				? me.GetString() ?? ""
				: "";
			DateTime receivedAt =
				item.TryGetProperty(propertyName: "mail_timestamp", out JsonElement mt)
				&& long.TryParse(mt.GetString(), out var unix)
					? DateTimeOffset.FromUnixTimeSeconds(seconds: unix).UtcDateTime
					: DateTime.UtcNow;

			emails.Add(
				new TempEmail(
					Id: msgId,
					From: from,
					Subject: subject,
					Body: body,
					ReceivedAt: receivedAt,
					IsHtml: false
				)
			);
		}

		if (root.TryGetProperty(propertyName: "seq", out JsonElement seqEl))
			_seq = seqEl.GetInt32();

		Log.Information("GuerrillaMailService found {Count} messages", emails.Count);
		return emails;
	}

	public async Task DeleteMailboxAsync(string mailboxId, CancellationToken ct = default)
	{
		Log.Debug("GuerrillaMailService.DeleteMailboxAsync entry {MailboxId}", mailboxId);

		if (IsNullOrEmpty(value: _sidToken))
		{
			throw new GuerrillaMailException(
				message: "No session. Call CreateMailboxAsync or provide credentials."
			);
		}

		using FormUrlEncodedContent content = new(
			new Dictionary<string, string> { [key: "sid_token"] = _sidToken, [key: "lang"] = "en" }
		);

		var url = new Uri($"{BaseUrl}?f=forget_me");
		HttpResponseMessage response = await Http.PostAsync(requestUri: url, content: content, ct);
		response.EnsureSuccessStatusCode();

		_sidToken = null;
		_emailAddress = null;
		Log.Information("GuerrillaMailService deleted mailbox {MailboxId}", mailboxId);
	}

	public async Task<TempEmail?> GetEmailAsync(
		string mailboxId,
		string emailId,
		CancellationToken ct = default
	)
	{
		Log.Debug("GuerrillaMailService.GetEmailAsync entry {EmailId}", emailId);

		if (IsNullOrEmpty(value: _sidToken))
		{
			throw new GuerrillaMailException(
				message: "No session. Call CreateMailboxAsync or provide credentials."
			);
		}

		var url = new Uri(
			$"{BaseUrl}?f=fetch_email&email_id={HttpUtility.UrlEncode(str: emailId)}&sid_token={HttpUtility.UrlEncode(str: _sidToken)}"
		);
		HttpResponseMessage response = await Http.GetAsync(requestUri: url, ct);
		response.EnsureSuccessStatusCode();

		using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
		JsonElement root = doc.RootElement;

		if (root.ValueKind is JsonValueKind.False or JsonValueKind.Null)
			return null;

		var from = root.TryGetProperty(propertyName: "mail_from", out JsonElement mf)
			? mf.GetString() ?? "unknown"
			: "unknown";
		var subject = root.TryGetProperty(propertyName: "mail_subject", out JsonElement ms)
			? ms.GetString() ?? ""
			: "";
		var bodyHtml = root.TryGetProperty(propertyName: "mail_body", out JsonElement mb)
			? mb.GetString() ?? ""
			: "";
		var bodyText = root.TryGetProperty(propertyName: "mail_text_only", out JsonElement mt)
			? mt.GetString() ?? ""
			: "";
		var isHtml = !IsNullOrEmpty(value: bodyHtml);
		DateTime receivedAt =
			root.TryGetProperty(propertyName: "mail_timestamp", out JsonElement ts)
			&& long.TryParse(ts.GetString(), out var unix)
				? DateTimeOffset.FromUnixTimeSeconds(seconds: unix).UtcDateTime
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

	internal static GuerrillaMailService Create() => new();

	internal static GuerrillaMailService CreateForExisting(
		string sidToken,
		string emailAddress,
		int seq
	) => new(sidToken: sidToken, emailAddress: emailAddress, seq: seq);

	internal Dictionary<string, string> GetCredentials() =>
		new() { [key: "sidToken"] = _sidToken ?? "", [key: "seq"] = _seq.ToString() };
}



