namespace CSharpScripts.Services.Mail;

internal sealed class MailTmException : Exception
{
	internal MailTmException()
		: base() { }

	internal MailTmException(string message)
		: base(message) { }

	internal MailTmException(string message, Exception? inner)
		: base(message, inner) { }
}

internal sealed class MailTmService : ITempMailService
{
	private const string BaseUrl = "https://api.mail.tm";

	private const string PasswordChars =
		"ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";

	private string? _credentialAddress;
	private string? _credentialPassword;

	private string? AuthToken;
	private string? CurrentAccountId;

	private MailTmService()
	{
		Client = new RestClient(BaseUrl);
		Log.Debug("MailTmService initialized for new mailbox");
	}

	private MailTmService(string address, string password)
	{
		Client = new RestClient(BaseUrl);
		_credentialAddress = address;
		_credentialPassword = password;
		Log.Debug("MailTmService initialized for existing mailbox {Address}", address);
	}

	internal static MailTmService Create() => new();

	internal static MailTmService CreateForExisting(string address, string password) =>
		new(address, password);

	public string ProviderName => "mail.tm";

	internal RestClient Client { get; }

	public async Task<TempMailbox> CreateMailboxAsync(CancellationToken ct = default)
	{
		MailTmAccount account = await CreateInternalAccountAsync(ct);
		return new TempMailbox(
			account.Id,
			account.Address,
			ProviderName,
			account.CreatedAt.ToUniversalTime()
		);
	}

	public async Task<IReadOnlyList<TempEmail>> CheckInboxAsync(
		string mailboxId,
		CancellationToken ct = default
	)
	{
		if (IsNullOrEmpty(AuthToken))
		{
			if (IsNullOrEmpty(_credentialAddress) || IsNullOrEmpty(_credentialPassword))
				throw new MailTmException(
					"No credentials available. Create a mailbox or provide credentials."
				);
			await AuthenticateAsync(_credentialAddress, _credentialPassword, ct);
		}

		CurrentAccountId ??= mailboxId;
		List<MailTmMessage> messages = await GetInboxInternalAsync(ct);
		return [.. messages.Select(MapToTempEmail)];
	}

	public async Task DeleteMailboxAsync(string mailboxId, CancellationToken ct = default)
	{
		if (IsNullOrEmpty(AuthToken))
		{
			if (IsNullOrEmpty(_credentialAddress) || IsNullOrEmpty(_credentialPassword))
				throw new MailTmException(
					"No credentials available. Create a mailbox or provide credentials."
				);
			await AuthenticateAsync(_credentialAddress, _credentialPassword, ct);
		}

		CurrentAccountId = mailboxId;
		await DeleteInternalAccountAsync(ct);
	}

	public async Task<TempEmail?> GetEmailAsync(
		string mailboxId,
		string emailId,
		CancellationToken ct = default
	)
	{
		if (IsNullOrEmpty(AuthToken))
		{
			if (IsNullOrEmpty(_credentialAddress) || IsNullOrEmpty(_credentialPassword))
				throw new MailTmException(
					"No credentials available. Create a mailbox or provide credentials."
				);
			await AuthenticateAsync(_credentialAddress, _credentialPassword, ct);
		}

		MailTmMessage msg = await ReadMessageInternalAsync(emailId, ct);
		return MapToTempEmail(msg);
	}

	private async Task<MailTmAccount> CreateInternalAccountAsync(CancellationToken ct = default)
	{
		Log.Debug("CreateInternalAccountAsync entry");
		Log.Information("Starting Creating mail.tm account");

		var domain = await GetAvailableDomainAsync(ct);
		var username = $"user{DateTime.UtcNow.Ticks % 100000000}";
		var address = $"{username}@{domain}";
		var password = GenerateSecurePassword();
		_credentialAddress = address;
		_credentialPassword = password;

		return await Resilience.ExecuteAsync(
			"MailTm.CreateAccount",
			async () =>
			{
				RestRequest request = new("/accounts", Method.Post);
				request.AddJsonBody(new { address, password });

				RestResponse<MailTmAccount> response = await Client.ExecuteAsync<MailTmAccount>(
					request,
					ct
				);

				if (!response.IsSuccessful || response.Data is null)
					throw new MailTmException(
						$"Failed to create account: {response.StatusCode} - {response.Content}"
					);

				CurrentAccountId = response.Data.Id;

				await AuthenticateAsync(address, password, ct);

				Log.Information("Complete Account created: {Address}", address);
				Log.Information("MailTmAccountId {Id}", response.Data.Id);
				return response.Data;
			},
			ct
		);
	}

	private async Task<string> GetAvailableDomainAsync(CancellationToken ct = default)
	{
		RestRequest request = new("/domains");
		RestResponse response = await Client.ExecuteAsync(request, ct);

		if (!response.IsSuccessful || IsNullOrEmpty(response.Content))
			throw new MailTmException($"Failed to get domains: {response.StatusCode}");

		using var doc = JsonDocument.Parse(response.Content);
		JsonElement root = doc.RootElement;

		JsonElement domains =
			root.ValueKind == JsonValueKind.Array ? root
			: root.TryGetProperty("hydra:member", out JsonElement members) ? members
			: root;

		if (domains.ValueKind == JsonValueKind.Array && domains.GetArrayLength() > 0)
		{
			var domain = domains[0].GetProperty("domain").GetString();
			if (!IsNullOrEmpty(domain))
				return domain;
		}

		throw new MailTmException("No available domains found");
	}

	private async Task AuthenticateAsync(
		string address,
		string password,
		CancellationToken ct = default
	)
	{
		Log.Debug("Authenticating: {Address}", address);

		RestRequest request = new("/token", Method.Post);
		request.AddJsonBody(new { address, password });

		RestResponse<MailTmTokenResponse> response = await Client.ExecuteAsync<MailTmTokenResponse>(
			request,
			ct
		);

		if (!response.IsSuccessful || IsNullOrEmpty(response.Data?.Token))
			throw new MailTmException($"Authentication failed: {response.StatusCode}");

		AuthToken = response.Data.Token;
		CurrentAccountId ??= response.Data.Id;
		Log.Debug("Authentication successful");
	}

	private async Task<List<MailTmMessage>> GetInboxInternalAsync(CancellationToken ct = default)
	{
		Log.Debug("GetInboxInternalAsync entry");
		Log.Information("Starting Fetching inbox");

		return await Resilience.ExecuteAsync(
			"MailTm.GetInbox",
			async () =>
			{
				RestRequest request = new("/messages");
				request.AddHeader("Authorization", $"Bearer {AuthToken}");

				RestResponse response = await Client.ExecuteAsync(request, ct);

				if (!response.IsSuccessful || IsNullOrEmpty(response.Content))
					throw new MailTmException($"Failed to fetch inbox: {response.StatusCode}");

				using var doc = JsonDocument.Parse(response.Content);
				JsonElement root = doc.RootElement;
				List<MailTmMessage> messages = [];

				JsonElement messageArray =
					root.ValueKind == JsonValueKind.Array ? root
					: root.TryGetProperty("hydra:member", out JsonElement members) ? members
					: throw new MailTmException("Unexpected inbox response format");

				foreach (JsonElement elem in messageArray.EnumerateArray())
					messages.Add(ParseMessage(elem));

				Log.Information("Complete Found {Count} messages", messages.Count);
				return messages;
			},
			ct
		);
	}

	private async Task<MailTmMessage> ReadMessageInternalAsync(
		string messageId,
		CancellationToken ct = default
	)
	{
		Log.Debug("ReadMessageInternalAsync entry {MessageId}", messageId);
		Log.Information("Starting Reading message: {MessageId}", messageId);

		return await Resilience.ExecuteAsync(
			"MailTm.ReadMessage",
			async () =>
			{
				RestRequest request = new($"/messages/{messageId}");
				request.AddHeader("Authorization", $"Bearer {AuthToken}");

				RestResponse response = await Client.ExecuteAsync(request, ct);

				if (!response.IsSuccessful || IsNullOrEmpty(response.Content))
					throw new MailTmException(
						$"Failed to read message: {response.StatusCode} - {response.ErrorMessage ?? response.Content}"
					);

				using var doc = JsonDocument.Parse(response.Content);
				JsonElement root = doc.RootElement;

				return new MailTmMessage
				{
					Id = root.GetProperty("id").GetString() ?? "",
					AccountId = root.TryGetProperty("accountId", out JsonElement aid)
						? aid.GetString() ?? ""
						: "",
					Subject = root.TryGetProperty("subject", out JsonElement subj)
						? subj.GetString() ?? ""
						: "",
					From = root.TryGetProperty("from", out JsonElement from)
						? new MailTmAddress
						{
							Address = from.GetProperty("address").GetString() ?? "",
							Name = from.TryGetProperty("name", out JsonElement n)
								? n.GetString()
								: null,
						}
						: null,
					Text = root.TryGetProperty("text", out JsonElement txt)
						? txt.GetString()
						: null,
					Html =
						root.TryGetProperty("html", out JsonElement htm)
						&& htm.ValueKind != JsonValueKind.Null
							? htm.EnumerateArray().FirstOrDefault().GetString()
							: null,
					CreatedAt =
						root.TryGetProperty("createdAt", out JsonElement ca)
						&& DateTime.TryParse(ca.GetString(), out DateTime dt)
							? dt
							: DateTime.MinValue,
				};
			},
			ct
		);
	}

	private async Task DeleteInternalAccountAsync(CancellationToken ct = default)
	{
		Log.Debug("DeleteInternalAccountAsync entry");

		if (IsNullOrEmpty(CurrentAccountId))
			throw new MailTmException("Account ID not set.");

		Log.Information("Starting Deleting account {Id}", CurrentAccountId);

		await Resilience.ExecuteAsync(
			"MailTm.DeleteAccount",
			async () =>
			{
				RestRequest request = new($"/accounts/{CurrentAccountId}", Method.Delete);
				request.AddHeader("Authorization", $"Bearer {AuthToken}");

				RestResponse response = await Client.ExecuteAsync(request, ct);

				if (!response.IsSuccessful)
					throw new MailTmException($"Failed to delete account: {response.StatusCode}");

				AuthToken = null;
				CurrentAccountId = null;

				Log.Information("Complete Account deleted");
			},
			ct
		);
	}

	private static MailTmMessage ParseMessage(JsonElement elem) =>
		new()
		{
			Id = elem.GetProperty("id").GetString() ?? "",
			AccountId = elem.TryGetProperty("accountId", out JsonElement aid)
				? aid.GetString() ?? ""
				: "",
			Subject = elem.TryGetProperty("subject", out JsonElement subj)
				? subj.GetString() ?? ""
				: "",
			From = elem.TryGetProperty("from", out JsonElement from)
				? new MailTmAddress
				{
					Address = from.GetProperty("address").GetString() ?? "",
					Name = from.TryGetProperty("name", out JsonElement n) ? n.GetString() : null,
				}
				: null,
			CreatedAt =
				elem.TryGetProperty("createdAt", out JsonElement ca)
				&& DateTime.TryParse(ca.GetString(), out DateTime dt)
					? dt
					: DateTime.MinValue,
		};

	internal Dictionary<string, string> GetCredentials() =>
		new() { ["address"] = _credentialAddress ?? "", ["password"] = _credentialPassword ?? "" };

	private static TempEmail MapToTempEmail(MailTmMessage m) =>
		new(
			Id: m.Id,
			From: m.From?.Address ?? "unknown",
			Subject: m.Subject,
			Body: m.Text ?? m.Html ?? "",
			ReceivedAt: m.CreatedAt.ToUniversalTime(),
			IsHtml: m.Text is null && m.Html is not null
		);

	private static string GenerateSecurePassword(int length = 20) =>
		new([
			.. Enumerable
				.Range(0, length)
				.Select(_ =>
					PasswordChars[
						System.Security.Cryptography.RandomNumberGenerator.GetInt32(
							PasswordChars.Length
						)
					]
				),
		]);
}

internal record MailTmAccount
{
	[JsonPropertyName("id")]
	public required string Id { get; init; }

	[JsonPropertyName("address")]
	public required string Address { get; init; }

	[JsonPropertyName("quota")]
	public int Quota { get; init; }

	[JsonPropertyName("used")]
	public int Used { get; init; }

	[JsonPropertyName("isDisabled")]
	public bool IsDisabled { get; init; }

	[JsonPropertyName("isDeleted")]
	public bool IsDeleted { get; init; }

	[JsonPropertyName("createdAt")]
	public DateTime CreatedAt { get; init; }

	[JsonPropertyName("updatedAt")]
	public DateTime UpdatedAt { get; init; }
}

internal record MailTmTokenResponse
{
	[JsonPropertyName("token")]
	public required string Token { get; init; }

	[JsonPropertyName("id")]
	public required string Id { get; init; }
}

internal record MailTmAddress
{
	[JsonPropertyName("address")]
	public required string Address { get; init; }

	[JsonPropertyName("name")]
	public string? Name { get; init; }
}

internal record MailTmMessage
{
	[JsonPropertyName("id")]
	public required string Id { get; init; }

	[JsonPropertyName("accountId")]
	public required string AccountId { get; init; }

	[JsonPropertyName("msgid")]
	public string? MsgId { get; init; }

	[JsonPropertyName("from")]
	public MailTmAddress? From { get; init; }

	[JsonPropertyName("to")]
	public MailTmAddress[]? To { get; init; }

	[JsonPropertyName("cc")]
	public MailTmAddress[]? Cc { get; init; }

	[JsonPropertyName("subject")]
	public required string Subject { get; init; }

	[JsonPropertyName("text")]
	public string? Text { get; init; }

	[JsonPropertyName("html")]
	public string? Html { get; init; }

	[JsonPropertyName("createdAt")]
	public DateTime CreatedAt { get; init; }

	[JsonPropertyName("updatedAt")]
	public DateTime UpdatedAt { get; init; }

	[JsonPropertyName("isRead")]
	public bool IsRead { get; init; }

	[JsonPropertyName("isDeleted")]
	public bool IsDeleted { get; init; }
}
