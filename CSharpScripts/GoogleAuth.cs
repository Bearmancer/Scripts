using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;

namespace CSharpScripts;

public static class GoogleAuth
{
    const string CREDENTIAL_STORE_YOUTUBE = "YouTube.Auth.Store";
    const string CREDENTIAL_STORE_SHEETS = "Sheets.Auth.Store";
    const string USER_IDENTIFIER = "user";

    private static readonly string clientId =
        Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID")
        ?? throw new InvalidOperationException("GOOGLE_CLIENT_ID environment variable not set");
    private static readonly string clientSecret =
        Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET")
        ?? throw new InvalidOperationException("GOOGLE_CLIENT_SECRET environment variable not set");

    public static async Task<YouTubeService> CreateYouTubeServiceAsync(string appName)
    {
        var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            clientSecrets: new ClientSecrets { ClientId = clientId, ClientSecret = clientSecret },
            scopes: [YouTubeService.Scope.YoutubeReadonly],
            user: USER_IDENTIFIER,
            taskCancellationToken: CancellationToken.None,
            dataStore: new FileDataStore(folder: CREDENTIAL_STORE_YOUTUBE, fullPath: true)
        );

        return new YouTubeService(
            new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = appName,
            }
        );
    }

    public static async Task<SheetsService> CreateSheetsServiceAsync(string appName)
    {
        var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            clientSecrets: new ClientSecrets { ClientId = clientId, ClientSecret = clientSecret },
            scopes: [SheetsService.Scope.Spreadsheets],
            user: USER_IDENTIFIER,
            taskCancellationToken: CancellationToken.None,
            dataStore: new FileDataStore(folder: CREDENTIAL_STORE_SHEETS, fullPath: true)
        );

        return new SheetsService(
            new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = appName,
            }
        );
    }
}
