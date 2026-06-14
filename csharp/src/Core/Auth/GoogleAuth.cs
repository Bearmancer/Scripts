namespace Scripts.Core.Auth;

using Google.Apis.Auth.OAuth2;
using Google.Apis.YouTube.v3;

internal static class GoogleAuth
{
	private static readonly string[] Scopes =
	[
		YouTubeService.Scope.YoutubeReadonly,
		"https://www.googleapis.com/auth/spreadsheets",
		"https://www.googleapis.com/auth/drive",
	];

	private static readonly SemaphoreSlim AuthLock = new(initialCount: 1, maxCount: 1);
	private static UserCredential? CachedCredential;

	public static async Task<UserCredential> GetCredentialAsync(CancellationToken ct = default)
	{
		using var _ = Log.Track();
		await AuthLock.WaitAsync(ct);
		try
		{
			if (CachedCredential is { } cached)
			{
				if (!cached.Token.IsStale)
				{
					return cached;
				}
				Log.Debug("Cached token is stale, re-authenticating...");
			}

			Ui.Progress("Opening browser for Google authentication...");

			var codeReceiver = new TcpCodeReceiver();

			CachedCredential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
				new ClientSecrets
				{
					ClientId = Secrets.GoogleClientId,
					ClientSecret = Secrets.GoogleClientSecret,
				},
				Scopes,
				"csharpscripts_user",
				ct,
				codeReceiver: codeReceiver);

			return CachedCredential;
		}
		finally
		{
			AuthLock.Release();
		}
	}

	public static async Task<BaseClientService.Initializer> GetInitializerAsync(
		CancellationToken ct = default)
	{
		var credential = await GetCredentialAsync(ct);
		return new BaseClientService.Initializer
		{
			HttpClientInitializer = credential,
			ApplicationName = "Scripts",
		};
	}

	public static void ForceReauthentication()
	{
		CachedCredential = null;
		Console.WriteLine("Credentials cleared, next API call will trigger re-authentication");
	}
}
