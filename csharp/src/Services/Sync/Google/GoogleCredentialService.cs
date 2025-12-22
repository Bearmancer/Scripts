namespace CSharpScripts.Services.Sync.Google;

public static class GoogleCredentialService
{
    private static ICredential? cached;

    private static readonly string[] Scopes =
    [
        SheetsService.Scope.Spreadsheets,
        DriveService.Scope.Drive,
        YouTubeServiceApi.Scope.Youtube,
    ];

    internal static ICredential GetCredential() =>
        cached ??= GoogleCredential.GetApplicationDefault().CreateScoped(Scopes);

    internal static async Task<string> GetAccessTokenAsync(CancellationToken ct = default) =>
        await GetCredential().GetAccessTokenForRequestAsync(cancellationToken: ct)
        ?? throw new InvalidOperationException("ADC token unavailable");

    internal static string GetAccessToken() => GetAccessTokenAsync().GetAwaiter().GetResult();
}
