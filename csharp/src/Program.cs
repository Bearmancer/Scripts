

namespace Scripts;

internal static class Program
{
	private static volatile bool Cancelled;
	public static CancellationTokenSource Cts { get; } = new();

	public static async Task Main(string[] args)
	{
		Console.WriteLine("[TRACE] Program.Main entered");
		try
		{
			Console.CancelKeyPress += (_, e) =>
			{
				e.Cancel = true;
				if (!Cancelled)
				{
					Cancelled = true;
					Cts.Cancel();
					Ui.Warn(message: "Cancellation requested, stopping gracefully...");
				}
			};

			Console.WriteLine("[TRACE] Calling GoogleAuth.GetCredentialAsync...");
			var sw = System.Diagnostics.Stopwatch.StartNew();
			var credential = await GoogleAuth.GetCredentialAsync(Cts.Token);
			sw.Stop();
			Console.WriteLine($"[TRACE] GetCredentialAsync returned in {sw.ElapsedMilliseconds}ms");
			Console.WriteLine($"[TRACE] Credential: UserId={credential.UserId}, Token.IsStale={credential.Token.IsStale}, HasAccessToken={!string.IsNullOrEmpty(credential.Token.AccessToken)}, HasRefreshToken={!string.IsNullOrEmpty(credential.Token.RefreshToken)}");

			// Verify token works by querying YouTube API
			Console.WriteLine("[TRACE] Verifying token by querying YouTube API...");
			try
			{
				var ytService = new Google.Apis.YouTube.v3.YouTubeService(new BaseClientService.Initializer
				{
					HttpClientInitializer = credential,
					ApplicationName = "Scripts"
				});

				var channelsRequest = ytService.Channels.List("snippet");
				channelsRequest.Mine = true;
				var channelsResponse = await channelsRequest.ExecuteAsync();

				Console.WriteLine($"[TRACE] YouTube API call SUCCESS");
				Console.WriteLine($"[TRACE] Channels found: {channelsResponse.Items?.Count ?? 0}");
				if (channelsResponse.Items?.Count > 0)
				{
					Console.WriteLine($"[TRACE] First channel: {channelsResponse.Items[0].Snippet.Title}");
				}
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"[TRACE] YouTube API call FAILED: {ex.GetType().Name}: {ex.Message}");
				if (ex.InnerException is { } inner)
				{
					Console.Error.WriteLine($"[TRACE] Inner: {inner.GetType().Name}: {inner.Message}");
				}
			}
			Console.WriteLine("YouTube auth complete.");
		}
		catch (OperationCanceledException ex)
		{
			Console.Error.WriteLine($"[TRACE] OperationCanceledException: {ex.Message}");
			Console.Error.WriteLine(value: "Fatal: Operation canceled.");
		}
		catch (Exception ex) when (ex is FileNotFoundException or UnauthorizedAccessException or InvalidOperationException or IOException or HttpRequestException)
		{
			Console.Error.WriteLine($"[TRACE] {ex.GetType().Name}: {ex.Message}");
			Console.Error.WriteLine($"[TRACE] StackTrace: {ex.StackTrace}");
			Console.Error.WriteLine($"Fatal: {ex.Message}");
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"[TRACE] UNHANDLED {ex.GetType().FullName}: {ex.Message}");
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
			Console.WriteLine("[TRACE] Program.Main exiting");
		}
	}
}
