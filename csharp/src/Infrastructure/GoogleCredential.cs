namespace CSharpScripts.Infrastructure;
using Google.Apis.YouTube.v3;

public static class GoogleCredentials
{
	private static readonly string[] Scopes =
	[
		SheetsService.Scope.Spreadsheets,
		DriveService.Scope.Drive,
		YouTubeService.Scope.YoutubeReadonly,
	];

	private static UserCredential? CachedCredential { get; set; }
	private static BaseClientService.Initializer? CachedInitializer { get; set; }

	public static BaseClientService.Initializer Initializer =>
		CachedInitializer ??= new BaseClientService.Initializer
		{
			HttpClientInitializer = GetCredential(),
			ApplicationName = "CSharpScripts",
		};

	public static UserCredential GetCredential()
	{
		if (CachedCredential is not null)
		{
			// Check if token is expired or about to expire
			if (CachedCredential.Token.IsStale)
			{
				Console.Info("Google token expired, refreshing...");
				if (!RefreshTokenAsync(CachedCredential).GetAwaiter().GetResult())
				{
					Console.Warning("Token refresh failed, re-authenticating...");
					CachedCredential = null;
					CachedInitializer = null;
				}
				else
				{
					return CachedCredential;
				}
			}
			else
			{
				return CachedCredential;
			}
		}

		Console.Info("Opening browser for Google authentication...");
		CachedCredential = GoogleWebAuthorizationBroker
			.AuthorizeAsync(
				new ClientSecrets
				{
					ClientId = Config.GoogleClientId,
					ClientSecret = Config.GoogleClientSecret,
				},
				Scopes,
				"csharpscripts_user",
				CancellationToken.None
			)
			.Result;

		return CachedCredential;
	}

	private static async Task<bool> RefreshTokenAsync(UserCredential credential)
	{
		try
		{
			return await credential.RefreshTokenAsync(CancellationToken.None);
		}
		catch (Exception ex)
		{
			Console.Error($"Failed to refresh Google token: {ex.Message}");
			return false;
		}
	}

	/// <summary>
	/// Forces re-authentication by clearing cached credentials.
	/// Call this when API calls fail due to expired/revoked tokens.
	/// </summary>
	public static void ForceReauthentication()
	{
		CachedCredential = null;
		CachedInitializer = null;
		Console.Info("Credentials cleared, next API call will trigger re-authentication");
	}
}
