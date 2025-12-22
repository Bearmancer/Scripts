namespace CSharpScripts.Services.Mail;

public sealed class MailTmException(string message, Exception? inner = null)
    : Exception(message, inner);

public sealed class MailTmService : IDisposableMailService
{
    const string BASE_URL = "https://api.mail.tm";
    const string PASSWORD_CHARS =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";

    internal RestClient Client { get; }
    string? AuthToken;
    string? CurrentAccountId;

    public MailTmService()
    {
        Client = new RestClient(BASE_URL);
        Console.Info("MailTmService initialized with centralized resiliency");
    }

    #region IDisposableMailService

    async Task<MailAccount> IDisposableMailService.CreateAccountAsync()
    {
        MailTmAccount account = await CreateAccountAsync();
        return new MailAccount(account.Address, DateTime.UtcNow);
    }

    async Task<List<MailMessage>> IDisposableMailService.GetInboxAsync()
    {
        List<MailTmMessage> messages = await GetInboxAsync();
        return
        [
            .. messages.Select(m => new MailMessage(
                Id: m.Id,
                From: m.From?.Address ?? "unknown",
                Subject: m.Subject,
                Body: m.Text ?? m.Html ?? "",
                ReceivedAt: m.CreatedAt.ToUniversalTime(),
                IsRead: m.IsRead
            )),
        ];
    }

    async Task<MailMessage> IDisposableMailService.ReadMessageAsync(string messageId)
    {
        MailTmMessage m = await ReadMessageAsync(messageId);
        return new MailMessage(
            Id: m.Id,
            From: m.From?.Address ?? "unknown",
            Subject: m.Subject,
            Body: m.Text ?? m.Html ?? "",
            ReceivedAt: m.CreatedAt.ToUniversalTime(),
            IsRead: m.IsRead
        );
    }

    async Task IDisposableMailService.ForgetSessionAsync() => await DeleteAccountAsync();

    #endregion

    public async Task<MailTmAccount> CreateAccountAsync()
    {
        Console.Starting("Creating mail.tm account");

        string domain = await GetAvailableDomainAsync();
        string username = $"test_{DateTime.UtcNow.Ticks}";
        string address = $"{username}@{domain}";
        string password = GenerateSecurePassword();

        return await Resilience.ExecuteAsync(
            operation: "MailTm.CreateAccount",
            action: async () =>
            {
                RestRequest request = new("/accounts", Method.Post);
                request.AddJsonBody(new { address, password });

                RestResponse<MailTmAccount> response = await Client.ExecuteAsync<MailTmAccount>(
                    request
                );

                if (!response.IsSuccessful || response.Data is null)
                    throw new MailTmException(
                        $"Failed to create account: {response.StatusCode} - {response.Content}"
                    );

                CurrentAccountId = response.Data.Id;

                await AuthenticateAsync(address, password);

                Console.Complete($"Account created: {address}");
                Console.KeyValue("Account ID", response.Data.Id);

                return response.Data;
            }
        );
    }

    async Task<string> GetAvailableDomainAsync()
    {
        RestRequest request = new("/domains", Method.Get);
        RestResponse response = await Client.ExecuteAsync(request);

        if (!response.IsSuccessful || IsNullOrEmpty(response.Content))
            throw new MailTmException($"Failed to get domains: {response.StatusCode}");

        using JsonDocument doc = JsonDocument.Parse(response.Content);
        JsonElement root = doc.RootElement;

        JsonElement domains =
            root.ValueKind == JsonValueKind.Array ? root
            : root.TryGetProperty("hydra:member", out JsonElement members) ? members
            : root;

        if (domains.ValueKind == JsonValueKind.Array && domains.GetArrayLength() > 0)
        {
            string? domain = domains[0].GetProperty("domain").GetString();
            if (!IsNullOrEmpty(domain))
                return domain;
        }

        throw new MailTmException("No available domains found");
    }

    async Task AuthenticateAsync(string address, string password)
    {
        Console.Debug($"Authenticating: {address}");

        RestRequest request = new("/token", Method.Post);
        request.AddJsonBody(new { address, password });

        RestResponse<TokenResponse> response = await Client.ExecuteAsync<TokenResponse>(request);

        if (!response.IsSuccessful || IsNullOrEmpty(response.Data?.Token))
            throw new MailTmException($"Authentication failed: {response.StatusCode}");

        AuthToken = response.Data.Token;
        Console.Debug("Authentication successful");
    }

    public async Task<List<MailTmMessage>> GetInboxAsync()
    {
        if (IsNullOrEmpty(AuthToken))
            throw new MailTmException("Not authenticated. Call CreateAccountAsync first.");

        Console.Starting("Fetching inbox");

        return await Resilience.ExecuteAsync(
            operation: "MailTm.GetInbox",
            action: async () =>
            {
                RestRequest request = new("/messages", Method.Get);
                request.AddHeader("Authorization", $"Bearer {AuthToken}");

                RestResponse response = await Client.ExecuteAsync(request);

                if (!response.IsSuccessful || IsNullOrEmpty(response.Content))
                    throw new MailTmException($"Failed to fetch inbox: {response.StatusCode}");

                using JsonDocument doc = JsonDocument.Parse(response.Content);
                JsonElement root = doc.RootElement;
                List<MailTmMessage> messages = [];

                JsonElement messageArray =
                    root.ValueKind == JsonValueKind.Array ? root
                    : root.TryGetProperty("hydra:member", out JsonElement members) ? members
                    : throw new MailTmException("Unexpected inbox response format");

                foreach (JsonElement elem in messageArray.EnumerateArray())
                {
                    messages.Add(ParseMessage(elem));
                }

                Console.Complete($"Found {messages.Count} messages");
                return messages;
            }
        );
    }

    public async Task<MailTmMessage> ReadMessageAsync(string messageId)
    {
        if (IsNullOrEmpty(AuthToken))
            throw new MailTmException("Not authenticated. Call CreateAccountAsync first.");

        Console.Starting($"Reading message: {messageId}");

        return await Resilience.ExecuteAsync(
            operation: "MailTm.ReadMessage",
            action: async () =>
            {
                RestRequest request = new($"/messages/{messageId}", Method.Get);
                request.AddHeader("Authorization", $"Bearer {AuthToken}");

                RestResponse response = await Client.ExecuteAsync(request);

                if (!response.IsSuccessful || IsNullOrEmpty(response.Content))
                    throw new MailTmException(
                        $"Failed to read message: {response.StatusCode} - {response.ErrorMessage ?? response.Content}"
                    );

                using JsonDocument doc = JsonDocument.Parse(response.Content);
                JsonElement root = doc.RootElement;

                MailTmMessage message = new()
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

                Console.Complete("Message loaded");
                return message;
            }
        );
    }

    public async Task<bool> DeleteAccountAsync()
    {
        if (IsNullOrEmpty(AuthToken) || IsNullOrEmpty(CurrentAccountId))
            throw new MailTmException("Not authenticated. Call CreateAccountAsync first.");

        Console.Starting("Deleting account");

        return await Resilience.ExecuteAsync(
            operation: "MailTm.DeleteAccount",
            action: async () =>
            {
                RestRequest request = new($"/accounts/{CurrentAccountId}", Method.Delete);
                request.AddHeader("Authorization", $"Bearer {AuthToken}");

                RestResponse response = await Client.ExecuteAsync(request);

                if (!response.IsSuccessful)
                    throw new MailTmException($"Failed to delete account: {response.StatusCode}");

                AuthToken = null;
                CurrentAccountId = null;

                Console.Complete("Account deleted");
                return true;
            }
        );
    }

    static MailTmMessage ParseMessage(JsonElement elem) =>
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

    static string GenerateSecurePassword(int length = 20) =>
        new([
            .. Enumerable
                .Range(0, length)
                .Select(_ => PASSWORD_CHARS[Random.Shared.Next(PASSWORD_CHARS.Length)]),
        ]);
}

public record MailTmAccount
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

public record TokenResponse
{
    [JsonPropertyName("token")]
    public required string Token { get; init; }

    [JsonPropertyName("id")]
    public required string Id { get; init; }
}

public record MailTmAddress
{
    [JsonPropertyName("address")]
    public required string Address { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }
}

public record MailTmMessage
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
