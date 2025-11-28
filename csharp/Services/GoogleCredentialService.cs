namespace CSharpScripts.Services;

internal static class GoogleCredentialService
{
    static UserCredential? cachedCredential;

    static readonly string[] AllScopes =
    [
        SheetsService.Scope.Spreadsheets,
        DriveService.Scope.DriveFile,
        Google.Apis.YouTube.v3.YouTubeService.Scope.YoutubeReadonly,
    ];

    internal static UserCredential GetCredential(string clientId, string clientSecret)
    {
        if (cachedCredential != null)
            return cachedCredential;

        Logger.Debug("Authorizing Google APIs...");

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
            var detail = inner.Message;

            if (detail.Contains("invalid_client", StringComparison.OrdinalIgnoreCase))
                detail =
                    "Invalid OAuth credentials. Verify GOOGLE_CLIENT_ID and GOOGLE_CLIENT_SECRET are correct.";
            else if (detail.Contains("invalid_grant", StringComparison.OrdinalIgnoreCase))
                detail =
                    "OAuth token expired. Delete the token file in your home directory and re-authenticate.";
            else if (detail.Contains("access_denied", StringComparison.OrdinalIgnoreCase))
                detail = "Access denied. You may have declined the authorization prompt.";

            throw new InvalidOperationException($"Google authentication failed: {detail}", inner);
        }

        Logger.Debug("Google APIs authorized");
        return cachedCredential;
    }

    internal static string GetAccessToken(UserCredential? credential)
    {
        if (credential == null)
            throw new InvalidOperationException("No credential available for access token");

        if (credential.Token.IsStale)
            credential.RefreshTokenAsync(CancellationToken.None).Wait();

        return credential.Token.AccessToken;
    }
}
