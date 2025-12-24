namespace CSharpScripts.Services.Mail;

public sealed class MailTmException(string message, Exception? inner = null)
    : Exception(message: message, innerException: inner);

public sealed class MailTmService : IDisposableMailService
{
    private const string BASE_URL = "https://api.mail.tm";

    private const string PASSWORD_CHARS =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";

    private string? AuthToken;
    private string? CurrentAccountId;

    public MailTmService()
    {
        Client = new RestClient(baseUrl: BASE_URL);
        Console.Info(message: "MailTmService initialized with centralized resiliency");
    }

    internal RestClient Client { get; }

    public async Task<MailTmAccount> CreateAccountAsync()
    {
        Console.Starting(operation: "Creating mail.tm account");

        string domain = await GetAvailableDomainAsync();
        var username = $"test_{DateTime.UtcNow.Ticks}";
        var address = $"{username}@{domain}";
        string password = GenerateSecurePassword();

        return await Resilience.ExecuteAsync(
            operation: "MailTm.CreateAccount",
            async () =>
            {
                RestRequest request = new(resource: "/accounts", method: Method.Post);
                request.AddJsonBody(new { address, password });

                var response = await Client.ExecuteAsync<MailTmAccount>(request: request);

                if (!response.IsSuccessful || response.Data is null)
                    throw new MailTmException(
                        $"Failed to create account: {response.StatusCode} - {response.Content}"
                    );

                CurrentAccountId = response.Data.Id;

                await AuthenticateAsync(address: address, password: password);

                Console.Complete($"Account created: {address}");
                Console.KeyValue(key: "Account ID", value: response.Data.Id);

                return response.Data;
            }
        );
    }

    private async Task<string> GetAvailableDomainAsync()
    {
        RestRequest request = new(resource: "/domains");
        var response = await Client.ExecuteAsync(request: request);

        if (!response.IsSuccessful || IsNullOrEmpty(value: response.Content))
            throw new MailTmException($"Failed to get domains: {response.StatusCode}");

        using var doc = JsonDocument.Parse(json: response.Content);
        var root = doc.RootElement;

        var domains =
            root.ValueKind == JsonValueKind.Array ? root
            : root.TryGetProperty(propertyName: "hydra:member", out var members) ? members
            : root;

        if (domains.ValueKind == JsonValueKind.Array && domains.GetArrayLength() > 0)
        {
            string? domain = domains[index: 0].GetProperty(propertyName: "domain").GetString();
            if (!IsNullOrEmpty(value: domain))
                return domain;
        }

        throw new MailTmException(message: "No available domains found");
    }

    private async Task AuthenticateAsync(string address, string password)
    {
        Console.Debug($"Authenticating: {address}");

        RestRequest request = new(resource: "/token", method: Method.Post);
        request.AddJsonBody(new { address, password });

        var response = await Client.ExecuteAsync<TokenResponse>(request: request);

        if (!response.IsSuccessful || IsNullOrEmpty(value: response.Data?.Token))
            throw new MailTmException($"Authentication failed: {response.StatusCode}");

        AuthToken = response.Data.Token;
        Console.Debug(message: "Authentication successful");
    }

    public async Task<List<MailTmMessage>> GetInboxAsync()
    {
        if (IsNullOrEmpty(value: AuthToken))
            throw new MailTmException(message: "Not authenticated. Call CreateAccountAsync first.");

        Console.Starting(operation: "Fetching inbox");

        return await Resilience.ExecuteAsync(
            operation: "MailTm.GetInbox",
            async () =>
            {
                RestRequest request = new(resource: "/messages");
                request.AddHeader(name: "Authorization", $"Bearer {AuthToken}");

                var response = await Client.ExecuteAsync(request: request);

                if (!response.IsSuccessful || IsNullOrEmpty(value: response.Content))
                    throw new MailTmException($"Failed to fetch inbox: {response.StatusCode}");

                using var doc = JsonDocument.Parse(json: response.Content);
                var root = doc.RootElement;
                List<MailTmMessage> messages = [];

                var messageArray =
                    root.ValueKind == JsonValueKind.Array ? root
                    : root.TryGetProperty(propertyName: "hydra:member", out var members) ? members
                    : throw new MailTmException(message: "Unexpected inbox response format");

                foreach (var elem in messageArray.EnumerateArray())
                    messages.Add(ParseMessage(elem: elem));

                Console.Complete($"Found {messages.Count} messages");
                return messages;
            }
        );
    }

    public async Task<MailTmMessage> ReadMessageAsync(string messageId)
    {
        if (IsNullOrEmpty(value: AuthToken))
            throw new MailTmException(message: "Not authenticated. Call CreateAccountAsync first.");

        Console.Starting($"Reading message: {messageId}");

        return await Resilience.ExecuteAsync(
            operation: "MailTm.ReadMessage",
            async () =>
            {
                RestRequest request = new($"/messages/{messageId}");
                request.AddHeader(name: "Authorization", $"Bearer {AuthToken}");

                var response = await Client.ExecuteAsync(request: request);

                if (!response.IsSuccessful || IsNullOrEmpty(value: response.Content))
                    throw new MailTmException(
                        $"Failed to read message: {response.StatusCode} - {response.ErrorMessage ?? response.Content}"
                    );

                using var doc = JsonDocument.Parse(json: response.Content);
                var root = doc.RootElement;

                MailTmMessage message = new()
                {
                    Id = root.GetProperty(propertyName: "id").GetString() ?? "",
                    AccountId = root.TryGetProperty(propertyName: "accountId", out var aid)
                        ? aid.GetString() ?? ""
                        : "",
                    Subject = root.TryGetProperty(propertyName: "subject", out var subj)
                        ? subj.GetString() ?? ""
                        : "",
                    From = root.TryGetProperty(propertyName: "from", out var from)
                        ? new MailTmAddress
                        {
                            Address = from.GetProperty(propertyName: "address").GetString() ?? "",
                            Name = from.TryGetProperty(propertyName: "name", out var n)
                                ? n.GetString()
                                : null,
                        }
                        : null,
                    Text = root.TryGetProperty(propertyName: "text", out var txt)
                        ? txt.GetString()
                        : null,
                    Html =
                        root.TryGetProperty(propertyName: "html", out var htm)
                        && htm.ValueKind != JsonValueKind.Null
                            ? htm.EnumerateArray().FirstOrDefault().GetString()
                            : null,
                    CreatedAt =
                        root.TryGetProperty(propertyName: "createdAt", out var ca)
                        && DateTime.TryParse(ca.GetString(), out var dt)
                            ? dt
                            : DateTime.MinValue,
                };

                Console.Complete(operation: "Message loaded");
                return message;
            }
        );
    }

    public async Task<bool> DeleteAccountAsync()
    {
        if (IsNullOrEmpty(value: AuthToken) || IsNullOrEmpty(value: CurrentAccountId))
            throw new MailTmException(message: "Not authenticated. Call CreateAccountAsync first.");

        Console.Starting(operation: "Deleting account");

        return await Resilience.ExecuteAsync(
            operation: "MailTm.DeleteAccount",
            async () =>
            {
                RestRequest request = new($"/accounts/{CurrentAccountId}", method: Method.Delete);
                request.AddHeader(name: "Authorization", $"Bearer {AuthToken}");

                var response = await Client.ExecuteAsync(request: request);

                if (!response.IsSuccessful)
                    throw new MailTmException($"Failed to delete account: {response.StatusCode}");

                AuthToken = null;
                CurrentAccountId = null;

                Console.Complete(operation: "Account deleted");
                return true;
            }
        );
    }

    private static MailTmMessage ParseMessage(JsonElement elem) =>
        new()
        {
            Id = elem.GetProperty(propertyName: "id").GetString() ?? "",
            AccountId = elem.TryGetProperty(propertyName: "accountId", out var aid)
                ? aid.GetString() ?? ""
                : "",
            Subject = elem.TryGetProperty(propertyName: "subject", out var subj)
                ? subj.GetString() ?? ""
                : "",
            From = elem.TryGetProperty(propertyName: "from", out var from)
                ? new MailTmAddress
                {
                    Address = from.GetProperty(propertyName: "address").GetString() ?? "",
                    Name = from.TryGetProperty(propertyName: "name", out var n)
                        ? n.GetString()
                        : null,
                }
                : null,
            CreatedAt =
                elem.TryGetProperty(propertyName: "createdAt", out var ca)
                && DateTime.TryParse(ca.GetString(), out var dt)
                    ? dt
                    : DateTime.MinValue,
        };

    private static string GenerateSecurePassword(int length = 20) =>
        new([
            .. Enumerable
                .Range(start: 0, count: length)
                .Select(_ => PASSWORD_CHARS[Random.Shared.Next(maxValue: PASSWORD_CHARS.Length)]),
        ]);

    async Task<MailAccount> IDisposableMailService.CreateAccountAsync()
    {
        var account = await CreateAccountAsync();
        return new MailAccount(Address: account.Address, CreatedAt: DateTime.UtcNow);
    }

    async Task<List<MailMessage>> IDisposableMailService.GetInboxAsync()
    {
        var messages = await GetInboxAsync();
        return
        [
            .. messages.Select(m => new MailMessage(
                Id: m.Id,
                m.From?.Address ?? "unknown",
                Subject: m.Subject,
                m.Text ?? m.Html ?? "",
                m.CreatedAt.ToUniversalTime(),
                IsRead: m.IsRead
            )),
        ];
    }

    async Task<MailMessage> IDisposableMailService.ReadMessageAsync(string messageId)
    {
        var m = await ReadMessageAsync(messageId: messageId);
        return new MailMessage(
            Id: m.Id,
            m.From?.Address ?? "unknown",
            Subject: m.Subject,
            m.Text ?? m.Html ?? "",
            m.CreatedAt.ToUniversalTime(),
            IsRead: m.IsRead
        );
    }

    async Task IDisposableMailService.ForgetSessionAsync() => await DeleteAccountAsync();
}

public record MailTmAccount
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

public record TokenResponse
{
    [JsonPropertyName(name: "token")]
    public required string Token { get; init; }

    [JsonPropertyName(name: "id")]
    public required string Id { get; init; }
}

public record MailTmAddress
{
    [JsonPropertyName(name: "address")]
    public required string Address { get; init; }

    [JsonPropertyName(name: "name")]
    public string? Name { get; init; }
}

public record MailTmMessage
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
