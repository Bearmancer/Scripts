using System.Security.Cryptography;

namespace CSharpScripts.Services.Mail;

public sealed class MailTmException : Exception
{
	internal MailTmException() { }

	internal MailTmException(string message)
		: base(message: message) { }

	internal MailTmException(string message, Exception? inner)
		: base(message: message, innerException: inner) { }
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
		Client = new RestClient(baseUrl: BaseUrl);
		Log.Debug("MailTmService initialized for new mailbox");
	}

	private MailTmService(string address, string password)
	{
		Client = new RestClient(baseUrl: BaseUrl);
		_credentialAddress = address;
		_credentialPassword = password;
		Log.Debug("MailTmService initialized for existing mailbox {Address}", address);
	}

	internal RestClient Client { get; }

	public string ProviderName => "mail.tm";

	public async Task<TempMailbox> CreateMailboxAsync(CancellationToken ct = default)
	{
		MailTmAccount account = await CreateInternalAccountAsync(ct);
		return new TempMailbox(
			Id: account.Id,
			Address: account.Address,
			Provider: ProviderName,
			account.CreatedAt.ToUniversalTime()
		);
	}

	public async Task<IReadOnlyList<TempEmail>> CheckInboxAsync(
		string mailboxId,
		CancellationToken ct = default
	)
	{
		if (IsNullOrEmpty(value: AuthToken))
		{
			if (
				IsNullOrEmpty(value: _credentialAddress)
				|| IsNullOrEmpty(value: _credentialPassword)
			)
			{
				throw new MailTmException(
					message: "No credentials available. Create a mailbox or provide credentials."
				);
			}
			await AuthenticateAsync(address: _credentialAddress, password: _credentialPassword, ct);
		}

		CurrentAccountId ??= mailboxId;
		List<MailTmMessage> messages = await GetInboxInternalAsync(ct);
		return messages.ConvertAll(MapToTempEmail);
	}

	public async Task DeleteMailboxAsync(string mailboxId, CancellationToken ct = default)
	{
		if (IsNullOrEmpty(value: AuthToken))
		{
			if (
				IsNullOrEmpty(value: _credentialAddress)
				|| IsNullOrEmpty(value: _credentialPassword)
			)
			{
				throw new MailTmException(
					message: "No credentials available. Create a mailbox or provide credentials."
				);
			}
			await AuthenticateAsync(address: _credentialAddress, password: _credentialPassword, ct);
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
		if (IsNullOrEmpty(value: AuthToken))
		{
			if (
				IsNullOrEmpty(value: _credentialAddress)
				|| IsNullOrEmpty(value: _credentialPassword)
			)
			{
				throw new MailTmException(
					message: "No credentials available. Create a mailbox or provide credentials."
				);
			}
			await AuthenticateAsync(address: _credentialAddress, password: _credentialPassword, ct);
		}

		MailTmMessage msg = await ReadMessageInternalAsync(messageId: emailId, ct);
		return MapToTempEmail(m: msg);
	}

	internal static MailTmService Create() => new();

	internal static MailTmService CreateForExisting(string address, string password) =>
		new(address: address, password: password);

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
			operation: "MailTm.CreateAccount",
			async () =>
			{
				RestRequest request = new(resource: "/accounts", method: Method.Post);
				RestRequestExtensions.AddJsonBody(request, new { address, password });

				RestResponse<MailTmAccount> response =
					await RestClientExtensions.ExecuteAsync<MailTmAccount>(
						Client,
						request: request,
						ct
					);

				if (!response.IsSuccessful || response.Data is null)
				{
					throw new MailTmException(
						$"Failed to create account: {response.StatusCode} - {response.Content}"
					);
				}

				CurrentAccountId = response.Data.Id;

				await AuthenticateAsync(address: address, password: password, ct);

				Log.Information("Complete Account created: {Address}", address);
				Log.Information("MailTmAccountId {Id}", response.Data.Id);
				return response.Data;
			},
			ct
		);
	}

	private async Task<string> GetAvailableDomainAsync(CancellationToken ct = default)
	{
		RestRequest request = new(resource: "/domains");
		RestResponse response = await Client.ExecuteAsync(request: request, ct);

		if (!response.IsSuccessful || IsNullOrEmpty(value: response.Content))
			throw new MailTmException($"Failed to get domains: {response.StatusCode}");

		using var doc = JsonDocument.Parse(json: response.Content);
		JsonElement root = doc.RootElement;

		JsonElement domains =
			root.ValueKind == JsonValueKind.Array ? root
			: root.TryGetProperty(propertyName: "hydra:member", out JsonElement members) ? members
			: root;

		if (domains.ValueKind == JsonValueKind.Array && domains.GetArrayLength() > 0)
		{
			var domain = domains[index: 0].GetProperty(propertyName: "domain").GetString();
			if (!IsNullOrEmpty(value: domain))
				return domain;
		}

		throw new MailTmException(message: "No available domains found");
	}

	private async Task AuthenticateAsync(
		string address,
		string password,
		CancellationToken ct = default
	)
	{
		Log.Debug("Authenticating: {Address}", address);

		RestRequest request = new(resource: "/token", method: Method.Post);
		RestRequestExtensions.AddJsonBody(request, new { address, password });

		RestResponse<MailTmTokenResponse> response =
			await RestClientExtensions.ExecuteAsync<MailTmTokenResponse>(
				Client,
				request: request,
				ct
			);

		if (!response.IsSuccessful || IsNullOrEmpty(value: response.Data?.Token))
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
			operation: "MailTm.GetInbox",
			async () =>
			{
				RestRequest request = new(resource: "/messages");
				RestRequestExtensions.AddHeader(
					request,
					name: "Authorization",
					$"Bearer {AuthToken}"
				);

				RestResponse response = await Client.ExecuteAsync(request: request, ct);

				if (!response.IsSuccessful || IsNullOrEmpty(value: response.Content))
					throw new MailTmException($"Failed to fetch inbox: {response.StatusCode}");

				using var doc = JsonDocument.Parse(json: response.Content);
				JsonElement root = doc.RootElement;
				List<MailTmMessage> messages = [];

				JsonElement messageArray =
					root.ValueKind == JsonValueKind.Array ? root
					: root.TryGetProperty(propertyName: "hydra:member", out JsonElement members)
						? members
					: throw new MailTmException(message: "Unexpected inbox response format");

				foreach (JsonElement elem in messageArray.EnumerateArray())
					messages.Add(ParseMessage(elem: elem));

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
			operation: "MailTm.ReadMessage",
			async () =>
			{
				RestRequest request = new($"/messages/{messageId}");
				RestRequestExtensions.AddHeader(
					request,
					name: "Authorization",
					$"Bearer {AuthToken}"
				);

				RestResponse response = await Client.ExecuteAsync(request: request, ct);

				if (!response.IsSuccessful || IsNullOrEmpty(value: response.Content))
				{
					throw new MailTmException(
						$"Failed to read message: {response.StatusCode} - {response.ErrorMessage ?? response.Content}"
					);
				}

				using var doc = JsonDocument.Parse(json: response.Content);
				JsonElement root = doc.RootElement;

				return new MailTmMessage
				{
					Id = root.GetProperty(propertyName: "id").GetString() ?? "",
					AccountId = root.TryGetProperty(propertyName: "accountId", out JsonElement aid)
						? aid.GetString() ?? ""
						: "",
					Subject = root.TryGetProperty(propertyName: "subject", out JsonElement subj)
						? subj.GetString() ?? ""
						: "",
					From = root.TryGetProperty(propertyName: "from", out JsonElement from)
						? new MailTmAddress
						{
							Address = from.GetProperty(propertyName: "address").GetString() ?? "",
							Name = from.TryGetProperty(propertyName: "name", out JsonElement n)
								? n.GetString()
								: null,
						}
						: null,
					Text = root.TryGetProperty(propertyName: "text", out JsonElement txt)
						? txt.GetString()
						: null,
					Html =
						root.TryGetProperty(propertyName: "html", out JsonElement htm)
						&& htm.ValueKind != JsonValueKind.Null
							? Enumerable.FirstOrDefault(htm.EnumerateArray()).GetString()
							: null,
					CreatedAt =
						root.TryGetProperty(propertyName: "createdAt", out JsonElement ca)
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

		if (IsNullOrEmpty(value: CurrentAccountId))
			throw new MailTmException(message: "Account ID not set.");

		Log.Information("Starting Deleting account {Id}", CurrentAccountId);

		await Resilience.ExecuteAsync(
			operation: "MailTm.DeleteAccount",
			async () =>
			{
				RestRequest request = new($"/accounts/{CurrentAccountId}", method: Method.Delete);
				RestRequestExtensions.AddHeader(
					request,
					name: "Authorization",
					$"Bearer {AuthToken}"
				);

				RestResponse response = await Client.ExecuteAsync(request: request, ct);

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
			Id = elem.GetProperty(propertyName: "id").GetString() ?? "",
			AccountId = elem.TryGetProperty(propertyName: "accountId", out JsonElement aid)
				? aid.GetString() ?? ""
				: "",
			Subject = elem.TryGetProperty(propertyName: "subject", out JsonElement subj)
				? subj.GetString() ?? ""
				: "",
			From = elem.TryGetProperty(propertyName: "from", out JsonElement from)
				? new MailTmAddress
				{
					Address = from.GetProperty(propertyName: "address").GetString() ?? "",
					Name = from.TryGetProperty(propertyName: "name", out JsonElement n)
						? n.GetString()
						: null,
				}
				: null,
			CreatedAt =
				elem.TryGetProperty(propertyName: "createdAt", out JsonElement ca)
				&& DateTime.TryParse(ca.GetString(), out DateTime dt)
					? dt
					: DateTime.MinValue,
		};

	internal Dictionary<string, string> GetCredentials() =>
		new()
		{
			[key: "address"] = _credentialAddress ?? "",
			[key: "password"] = _credentialPassword ?? "",
		};

	private static TempEmail MapToTempEmail(MailTmMessage m) =>
		new(
			Id: m.Id,
			m.From?.Address ?? "unknown",
			Subject: m.Subject,
			m.Text ?? m.Html ?? "",
			m.CreatedAt.ToUniversalTime(),
			m.Text is null && m.Html is { }
		);

	private static string GenerateSecurePassword(int length = 20) =>
		string.Create(
			length,
			PasswordChars,
			static (span, chars) =>
			{
				for (var i = 0; i < span.Length; i++)
					span[i] = chars[RandomNumberGenerator.GetInt32(chars.Length)];
			}
		);
}

internal record MailTmAccount
{
	[JsonPropertyName(name: "id")]
	public required string Id { get; init; }

	[JsonPropertyName(name: "address")]
	public required string Address { get; init; }

	[JsonPropertyName(name: "quota")]
	public int Quota { get; init; }

	[JsonPropertyName(name: "used")]
	public int Used { get; init; }

	[JsonPropertyName(name: "isDisabled")]
	public bool IsDisabled { get; init; }

	[JsonPropertyName(name: "isDeleted")]
	public bool IsDeleted { get; init; }

	[JsonPropertyName(name: "createdAt")]
	public DateTime CreatedAt { get; init; }

	[JsonPropertyName(name: "updatedAt")]
	public DateTime UpdatedAt { get; init; }
}

internal record MailTmTokenResponse
{
	[JsonPropertyName(name: "token")]
	public required string Token { get; init; }

	[JsonPropertyName(name: "id")]
	public required string Id { get; init; }
}

internal record MailTmAddress
{
	[JsonPropertyName(name: "address")]
	public required string Address { get; init; }

	[JsonPropertyName(name: "name")]
	public string? Name { get; init; }
}

internal record MailTmMessage
{
	[JsonPropertyName(name: "id")]
	public required string Id { get; init; }

	[JsonPropertyName(name: "accountId")]
	public required string AccountId { get; init; }

	[JsonPropertyName(name: "msgid")]
	public string? MsgId { get; init; }

	[JsonPropertyName(name: "from")]
	public MailTmAddress? From { get; init; }

	[JsonPropertyName(name: "to")]
	public MailTmAddress[]? To { get; init; }

	[JsonPropertyName(name: "cc")]
	public MailTmAddress[]? Cc { get; init; }

	[JsonPropertyName(name: "subject")]
	public required string Subject { get; init; }

	[JsonPropertyName(name: "text")]
	public string? Text { get; init; }

	[JsonPropertyName(name: "html")]
	public string? Html { get; init; }

	[JsonPropertyName(name: "createdAt")]
	public DateTime CreatedAt { get; init; }

	[JsonPropertyName(name: "updatedAt")]
	public DateTime UpdatedAt { get; init; }

	[JsonPropertyName(name: "isRead")]
	public bool IsRead { get; init; }

	[JsonPropertyName(name: "isDeleted")]
	public bool IsDeleted { get; init; }
}



