using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Util.Store;

namespace CSharpScripts.Core.Auth;

internal static class GoogleAuth
{
	private static readonly string[] Scopes =
	[
		SheetsService.Scope.Spreadsheets,
		DriveService.Scope.Drive,
		YouTubeServiceApi.Scope.YoutubeReadonly,
	];

	private static readonly SemaphoreSlim AuthLock = new(initialCount: 1, maxCount: 1);

	private static UserCredential? CachedCredential { get; set; }
	private static BaseClientService.Initializer? CachedInitializer { get; set; }

	public static async Task<BaseClientService.Initializer> GetInitializerAsync(
		CancellationToken ct = default
	)
	{
		if (CachedInitializer is not null)
			return CachedInitializer;

		await AuthLock.WaitAsync(ct);
		try
		{
#pragma warning disable CA1508 // double-checked locking pattern
			if (CachedInitializer is not null)
				return CachedInitializer;
#pragma warning restore CA1508

			UserCredential credential = await AcquireOrRefreshCredentialAsync(ct);
			CachedInitializer = new BaseClientService.Initializer
			{
				HttpClientInitializer = credential,
				ApplicationName = "CSharpScripts",
			};
			return CachedInitializer;
		}
		finally
		{
			AuthLock.Release();
		}
	}

	public static async Task<UserCredential> GetCredentialAsync(CancellationToken ct = default)
	{
		await AuthLock.WaitAsync(ct);
		try
		{
			return await AcquireOrRefreshCredentialAsync(ct);
		}
		finally
		{
			AuthLock.Release();
		}
	}

	private static async Task<UserCredential> AcquireOrRefreshCredentialAsync(CancellationToken ct)
	{
		if (CachedCredential is { } cached)
		{
			if (!cached.Token.IsStale)
				return cached;

			Log.Information("Google token expired, refreshing...");
			try
			{
				if (await cached.RefreshTokenAsync(taskCancellationToken: ct))
					return CachedCredential;
			}
			catch (TokenResponseException ex)
			{
				Log.Error(ex, "Failed to refresh Google token: {Message}", ex.Message);
			}
			catch (HttpRequestException ex)
			{
				Log.Error(ex, "Failed to refresh Google token: {Message}", ex.Message);
			}

			Log.Warning("Token refresh failed, re-authenticating...");
			CachedCredential = null;
			CachedInitializer = null;
		}

		CachedCredential = await AcquireBrowserCredentialAsync(ct);
		return CachedCredential;
	}

	private static async Task<UserCredential> AcquireBrowserCredentialAsync(CancellationToken ct)
	{
		var authDir = Path.Combine(Paths.StateDirectory, "google-auth");
		var tokenPath = Path.Combine(
			authDir,
			"Google.Apis.Auth.OAuth2.Responses.TokenResponse-csharpscripts_user"
		);

		if (!File.Exists(tokenPath))
			Log.Information("Opening browser for Google authentication...");

		return await GoogleWebAuthorizationBroker.AuthorizeAsync(
			new ClientSecrets
			{
				ClientId = Secrets.GoogleClientId,
				ClientSecret = Secrets.GoogleClientSecret,
			},
			Scopes,
			"csharpscripts_user",
			ct,
			new FileDataStore(authDir, fullPath: true)
		);
	}

	public static void ForceReauthentication()
	{
		CachedCredential = null;
		CachedInitializer = null;
		Log.Information("Credentials cleared, next API call will trigger re-authentication");
	}
}
