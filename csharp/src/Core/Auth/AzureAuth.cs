using System;
using System.Diagnostics.Tracing;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Core.Diagnostics;
using Azure.Identity;
using Microsoft.Identity.Client;

namespace Scripts.Core.Auth;

internal static class AzureAuth
{
	public static readonly TokenCredential Credential = new ChainedTokenCredential(
		new MsalCacheCredential(),
		new AzureCliCredential(new AzureCliCredentialOptions
		{
			ProcessTimeout = TimeSpan.FromSeconds(60),
		}));

	internal static readonly AzureEventSourceListener VerboseListener = new(
		WriteEvent,
		level: EventLevel.Verbose);

	private static void WriteEvent(EventWrittenEventArgs eventData, string message)
	{
		if (eventData.EventSource.Name != "Azure-Identity")
			return;
		try
		{
			Log.Verbose("[{EventSource}] {Message}", eventData.EventSource.Name, message);
		}
		catch
		{
		}
	}
}

internal sealed class MsalCacheCredential : TokenCredential
{
	private const string AzClientId = "04b07795-8ddb-461a-bbc8-06d85301bc17";
	private const string Authority = "https://login.microsoftonline.com/common";

	private static readonly string AzHome = System.IO.Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".azure");
	private static readonly string CachePath = System.IO.Path.Combine(AzHome, "msal_token_cache.bin");

	private static readonly IPublicClientApplication App = PublicClientApplicationBuilder
		.Create(AzClientId)
		.WithAuthority(Authority, validateAuthority: false)
		.WithRedirectUri("http://localhost:8400")
		.Build();

	static MsalCacheCredential()
	{
		App.UserTokenCache.SetBeforeAccess(args =>
		{
			if (!System.IO.File.Exists(CachePath))
				return;
			try
			{
				args.TokenCache.DeserializeMsalV3(
					System.IO.File.ReadAllBytes(CachePath),
					shouldClearExistingCache: false);
			}
			catch (Exception ex)
			{
				Log.Warning("MSAL cache deserialize failed: {Message}", ex.Message);
			}
		});
	}

	public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
		=> GetTokenAsync(requestContext, cancellationToken).AsTask().GetAwaiter().GetResult();

	public override async ValueTask<AccessToken> GetTokenAsync(
		TokenRequestContext requestContext,
		CancellationToken cancellationToken)
	{
		var accounts = await App.GetAccountsAsync().WaitAsync(cancellationToken);
		if (!System.Linq.Enumerable.Any(accounts))
			throw new CredentialUnavailableException(
				$"MSAL cache has no accounts. Run 'az login' to populate {CachePath}.");

		var lastError = default(Exception);
		foreach (var account in accounts)
		{
			try
			{
				var result = await App.AcquireTokenSilent(requestContext.Scopes, account)
					.ExecuteAsync(cancellationToken)
					.WaitAsync(cancellationToken);
				return new AccessToken(result.AccessToken, result.ExpiresOn);
			}
			catch (MsalUiRequiredException ex)
			{
				lastError = ex;
			}
		}

		throw new CredentialUnavailableException(
			$"No cached token for scopes [{string.Join(", ", requestContext.Scopes)}]. Last error: {lastError?.Message}");
	}
}
