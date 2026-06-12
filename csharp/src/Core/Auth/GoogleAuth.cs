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
		Console.WriteLine("[TRACE] GoogleAuth.GetCredentialAsync entered");
		await AuthLock.WaitAsync(ct);
		Console.WriteLine("[TRACE] AuthLock acquired");
		try
		{
			if (CachedCredential is { } cached)
			{
				Console.WriteLine($"[TRACE] CachedCredential exists: UserId={cached.UserId}, Token.IsStale={cached.Token.IsStale}, AccessToken={cached.Token.AccessToken?[..Math.Min(10, cached.Token.AccessToken.Length)]}..., RefreshToken={cached.Token.RefreshToken?[..Math.Min(10, cached.Token.RefreshToken.Length)]}...");
				if (!cached.Token.IsStale)
				{
					Console.WriteLine("[TRACE] Returning cached credential (not stale)");
					return cached;
				}
				Console.WriteLine("[TRACE] Cached token is stale, re-authenticating...");
			}
			else
			{
				Console.WriteLine("[TRACE] No cached credential, performing fresh auth...");
			}

			Console.WriteLine("Opening browser for Google authentication...");
			Console.WriteLine($"[TRACE] Scopes requested: {string.Join(", ", Scopes)}");

			var codeReceiver = new TcpCodeReceiver();
			Console.WriteLine($"[TRACE] TcpCodeReceiver created, redirect: {codeReceiver.RedirectUri}");

			Console.WriteLine($"[TRACE] Calling GoogleWebAuthorizationBroker.AuthorizeAsync...");
			Console.WriteLine($"[TRACE] ClientId: {Secrets.GoogleClientId?[..Math.Min(10, Secrets.GoogleClientId.Length)]}...");
			Console.WriteLine($"[TRACE] UserId: csharpscripts_user");

			var sw = System.Diagnostics.Stopwatch.StartNew();
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
			sw.Stop();

			Console.WriteLine($"[TRACE] AuthorizeAsync completed in {sw.ElapsedMilliseconds}ms");
			Console.WriteLine($"[TRACE] Credential obtained: UserId={CachedCredential.UserId}");
			Console.WriteLine($"[TRACE] Token.IsStale={CachedCredential.Token.IsStale}");
			Console.WriteLine($"[TRACE] Token.AccessToken={CachedCredential.Token.AccessToken?[..Math.Min(20, CachedCredential.Token.AccessToken.Length)]}...");
			Console.WriteLine($"[TRACE] Token.RefreshToken={CachedCredential.Token.RefreshToken?[..Math.Min(20, CachedCredential.Token.RefreshToken.Length)]}...");
			Console.WriteLine($"[TRACE] Token.IssuedUtc={CachedCredential.Token.IssuedUtc}");
			Console.WriteLine($"[TRACE] Token.ExpiresInSeconds={CachedCredential.Token.ExpiresInSeconds}");
			Console.WriteLine($"[TRACE] CurrentUtcTime={DateTime.UtcNow}");
			Console.WriteLine($"[TRACE] Token.Scope={CachedCredential.Token.Scope}");
			Console.WriteLine($"[TRACE] Token.TokenType={CachedCredential.Token.TokenType}");

			return CachedCredential;
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"[TRACE] EXCEPTION in GetCredentialAsync: {ex.GetType().FullName}: {ex.Message}");
			Console.Error.WriteLine($"[TRACE] StackTrace: {ex.StackTrace}");
			if (ex.InnerException is { } inner)
			{
				Console.Error.WriteLine($"[TRACE] InnerException: {inner.GetType().FullName}: {inner.Message}");
				Console.Error.WriteLine($"[TRACE] InnerStackTrace: {inner.StackTrace}");
			}
			throw;
		}
		finally
		{
			Console.WriteLine("[TRACE] AuthLock releasing");
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
