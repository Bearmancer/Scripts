using Google.Apis.Auth.OAuth2;

namespace CSharpScripts.Core.Auth;

internal static class GoogleAuth
{
	private static readonly string[] Scopes =
	[
		SheetsService.Scope.Spreadsheets,
		DriveService.Scope.Drive,
		YouTubeServiceApi.Scope.YoutubeReadonly,
	];

	private static readonly SemaphoreSlim AuthLock = new(1, 1);

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
#pragma warning disable CA1508 // False positive - CachedInitializer can be set by another thread
			CachedInitializer ??= new BaseClientService.Initializer
			{
				HttpClientInitializer = await GetCredentialAsync(ct),
				ApplicationName = "CSharpScripts",
			};
#pragma warning restore CA1508
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
			if (CachedCredential is not null && !IsCredentialStale(CachedCredential))
				return CachedCredential;

			if (CachedCredential is { } cached && IsCredentialStale(cached))
			{
				Log.Information("Google token expired, refreshing...");
				if (await TryRefreshTokenAsync(cached, ct))
					return CachedCredential;

				Log.Warning("Token refresh failed, re-authenticating...");
				CachedCredential = null;
				CachedInitializer = null;
			}

			CachedCredential = await AcquireBrowserCredentialAsync(ct);
			return CachedCredential;
		}
		finally
		{
			AuthLock.Release();
		}
	}

	private static bool IsCredentialStale(UserCredential credential) => credential.Token.IsStale;

	private static async Task<bool> TryRefreshTokenAsync(
		UserCredential credential,
		CancellationToken ct
	)
	{
		if (await RefreshTokenAsync(credential, ct))
			return true;

		CachedCredential = null;
		CachedInitializer = null;
		return false;
	}

	private static async Task<UserCredential> AcquireBrowserCredentialAsync(CancellationToken ct)
	{
		var tokenPath = Path.Combine(
			GetFolderPath(SpecialFolder.ApplicationData),
			"Google.Apis.Auth",
			"Google.Apis.Auth.OAuth2.Responses.TokenResponse-csharpscripts_user"
		);
		var needsBrowserAuth = !File.Exists(tokenPath);
		if (needsBrowserAuth)
			Log.Information("Opening browser for Google authentication...");

		return await GoogleWebAuthorizationBroker.AuthorizeAsync(
			new ClientSecrets
			{
				ClientId = Secrets.GoogleClientId,
				ClientSecret = Secrets.GoogleClientSecret,
			},
			Scopes,
			"csharpscripts_user",
			ct
		);
	}

	private static async Task<bool> RefreshTokenAsync(
		UserCredential credential,
		CancellationToken ct
	)
	{
		try
		{
			return await credential.RefreshTokenAsync(ct);
		}
		catch (Google.Apis.Auth.OAuth2.Responses.TokenResponseException ex)
		{
			Log.Error("Failed to refresh Google token: {Message}", ex.Message);
			return false;
		}
		catch (HttpRequestException ex)
		{
			Log.Error("Failed to refresh Google token: {Message}", ex.Message);
			return false;
		}
	}

	public static void ForceReauthentication()
	{
		CachedCredential = null;
		CachedInitializer = null;
		Log.Information("Credentials cleared, next API call will trigger re-authentication");
	}
}
