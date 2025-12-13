namespace CSharpScripts.Services.Sync.Google;

public static class GoogleCredentialService
{
    private static UserCredential? cachedCredential;

    private static readonly string[] AllScopes =
    [
        SheetsService.Scope.Spreadsheets,
        DriveService.Scope.DriveFile,
        YouTubeServiceApi.Scope.YoutubeReadonly,
    ];

    internal static UserCredential GetCredential(string clientId, string clientSecret)
    {
        if (cachedCredential != null)
            return cachedCredential;

        Console.Debug("Authorizing Google APIs...");

        if (IsNullOrWhiteSpace(clientId))
            throw new InvalidOperationException(
                "Google Client ID is empty. Set GOOGLE_CLIENT_ID environment variable."
            );

        if (IsNullOrWhiteSpace(clientSecret))
            throw new InvalidOperationException(
                "Google Client Secret is empty. Set GOOGLE_CLIENT_SECRET environment variable."
            );

        try
        {
            cachedCredential = GoogleWebAuthorizationBroker
                .AuthorizeAsync(
                    clientSecrets: new() { ClientId = clientId, ClientSecret = clientSecret },
                    scopes: AllScopes,
                    user: "simplercs_user",
                    taskCancellationToken: CancellationToken.None
                )
                .Result;
        }
        catch (AggregateException aex) when (aex.InnerException is not null)
        {
            var inner = aex.InnerException;
            string detail = inner.Message switch
            {
                var m when m.Contains("invalid_client", StringComparison.OrdinalIgnoreCase) =>
                    "Invalid OAuth credentials. Verify GOOGLE_CLIENT_ID and GOOGLE_CLIENT_SECRET are correct.",
                var m when m.Contains("invalid_grant", StringComparison.OrdinalIgnoreCase) =>
                    "OAuth token expired. Delete the token file in your home directory and re-authenticate.",
                var m when m.Contains("access_denied", StringComparison.OrdinalIgnoreCase) =>
                    "Access denied. You may have declined the authorization prompt.",
                _ => inner.Message,
            };

            throw new InvalidOperationException($"Google authentication failed: {detail}", inner);
        }

        Console.Debug("Google APIs authorized");
        return cachedCredential;
    }

    public static string GetAccessToken(UserCredential? credential)
    {
        if (credential == null)
            throw new InvalidOperationException("No credential available for access token");

        if (credential.Token.IsStale)
            credential.RefreshTokenAsync(CancellationToken.None).Wait();

        return credential.Token.AccessToken;
    }
}
