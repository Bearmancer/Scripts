namespace Scripts.CLI;

#region Sync All Command

public sealed class SyncAllCommand : AsyncCommand<SyncAllCommand.Settings>
{
	public override async Task<int> ExecuteAsync(
		CommandContext context,
		Settings settings,
		CancellationToken cancellationToken
	)
	{
		if (settings.Verbose)
		{
			Console.Level = LogLevel.Debug;
			Logger.FileLevel = LogLevel.Debug;
		}

		if (settings.Reset)
		{
			Console.Info("Clearing local cache...");
			StateManager.DeleteLastFmStates();
			StateManager.DeleteAllYouTubeStates();
			Console.Success("Cache cleared");
		}

		Console.Rule("YouTube Sync");
		var ytResult = await RunYouTubeSyncAsync();

		Console.NewLine();
		Console.Rule("Last.fm Sync");
		var lfResult = await RunLastFmSyncAsync();

		Console.NewLine();
		if (ytResult == 0 && lfResult == 0)
			Console.Success("All syncs complete!");
		else
			Console.Warning(
				"Completed with errors (YouTube: {0}, Last.fm: {1})",
				ytResult,
				lfResult
			);

		return ytResult != 0 ? ytResult : lfResult;
	}

	private static async Task<int> RunYouTubeSyncAsync()
	{
		Logger.Start(ServiceType.YouTube);
		return await SyncYouTubeCommand.ExecuteWithErrorHandlingAsync(async () =>
			await new YouTubePlaylistOrchestrator(Program.cts.Token).ExecuteAsync()
		);
	}

	private static async Task<int> RunLastFmSyncAsync()
	{
		Logger.Start(ServiceType.LastFm);
		return await SyncYouTubeCommand.ExecuteWithErrorHandlingAsync(async () =>
			await new ScrobbleSyncOrchestrator(null, Program.cts.Token).ExecuteAsync()
		);
	}

	public sealed class Settings : CommandSettings
	{
		[CommandOption("-v|--verbose")]
		[Description("Debug logging")]
		public bool Verbose { get; init; }

		[CommandOption("-r|--reset")]
		[Description("Clear cache first")]
		public bool Reset { get; init; }
	}
}

#endregion

#region Sync YouTube Command

public sealed class SyncYouTubeCommand : AsyncCommand<SyncYouTubeCommand.Settings>
{
	public override async Task<int> ExecuteAsync(
		CommandContext context,
		Settings settings,
		CancellationToken cancellationToken
	)
	{
		if (settings.Verbose)
		{
			Console.Level = LogLevel.Debug;
			Logger.FileLevel = LogLevel.Debug;
		}

		Logger.Start(ServiceType.YouTube);

		return await ExecuteWithErrorHandlingAsync(async () =>
		{
			if (settings.Reset)
			{
				Console.Info("Clearing YouTube cache...");
				StateManager.DeleteAllYouTubeStates();
				Console.Success("Cache cleared");
			}

			if (settings.ShowSessionId)
				Console.Info("Session ID: {0}", Logger.CurrentSessionId);

			await new YouTubePlaylistOrchestrator(Program.cts.Token).ExecuteAsync();
		});
	}

	internal static int ExecuteWithErrorHandling(Action action)
	{
		try
		{
			action();
			return 0;
		}
		catch (DailyQuotaExceededException ex)
		{
			Console.Error("{0}: {1}", ex.GetType().Name, ex.Message);
			Console.Error(
				"Try again tomorrow or request quota increase from Google Cloud Console."
			);
			if (ex.InnerException != null)
				Console.Error("Inner: {0}", ex.InnerException.Message);
			Logger.End(false, $"DailyQuotaExceededException: {ex.Message}", ex);
			return 1;
		}
		catch (RetryExhaustedException ex)
		{
			Console.Error("{0}: {1}", ex.GetType().Name, ex.Message);
			Console.Error("Wait 15-30 minutes and try again. Progress has been saved.");
			if (ex.InnerException != null)
				Console.Error(
					"Inner: {0}: {1}",
					ex.InnerException.GetType().Name,
					ex.InnerException.Message
				);
			Logger.End(false, $"RetryExhaustedException: {ex.Message}", ex);
			return 1;
		}
		catch (AggregateException aex)
		{
			foreach (Exception ex in aex.InnerExceptions)
			{
				Console.Error("{0}: {1}", ex.GetType().Name, ex.Message);
				if (ex.InnerException != null)
					Console.Error(
						"  Inner: {0}: {1}",
						ex.InnerException.GetType().Name,
						ex.InnerException.Message
					);
			}
			Exception firstError = aex.InnerExceptions[0];
			var summary =
				$"AggregateException ({aex.InnerExceptions.Count} errors): {firstError.GetType().Name}: {firstError.Message}";
			Logger.End(false, summary, aex);
			return 1;
		}
		catch (OperationCanceledException)
		{
			Console.Warning("Operation cancelled by user");
			Logger.Interrupted("Cancelled by Ctrl+C");
			return 130;
		}
		catch (Exception ex)
		{
			Console.Error("{0}: {1}", ex.GetType().Name, ex.Message);
			if (ex.InnerException != null)
				Console.Error(
					"Inner: {0}: {1}",
					ex.InnerException.GetType().Name,
					ex.InnerException.Message
				);
			if (ex.StackTrace != null)
			{
				var firstStackLine = ex.StackTrace.Split('\n')[0].Trim();
				Console.Dim($"Stack: {firstStackLine}");
			}

			var summary =
				ex.InnerException != null
					? $"{ex.GetType().Name}: {ex.Message} (Inner: {ex.InnerException.Message})"
					: $"{ex.GetType().Name}: {ex.Message}";

			Logger.End(false, summary, ex);
			return 1;
		}
	}

	internal static async Task<int> ExecuteWithErrorHandlingAsync(Func<Task> action)
	{
		try
		{
			await action();
			return 0;
		}
		catch (DailyQuotaExceededException ex)
		{
			Console.Error("{0}: {1}", ex.GetType().Name, ex.Message);
			Console.Error(
				"Try again tomorrow or request quota increase from Google Cloud Console."
			);
			if (ex.InnerException != null)
				Console.Error("Inner: {0}", ex.InnerException.Message);
			Logger.End(false, $"DailyQuotaExceededException: {ex.Message}", ex);
			return 1;
		}
		catch (RetryExhaustedException ex)
		{
			Console.Error("{0}: {1}", ex.GetType().Name, ex.Message);
			Console.Error("Wait 15-30 minutes and try again. Progress has been saved.");
			if (ex.InnerException != null)
				Console.Error(
					"Inner: {0}: {1}",
					ex.InnerException.GetType().Name,
					ex.InnerException.Message
				);
			Logger.End(false, $"RetryExhaustedException: {ex.Message}", ex);
			return 1;
		}
		catch (OperationCanceledException)
		{
			Console.Warning("Operation cancelled by user");
			Logger.Interrupted("Cancelled by Ctrl+C");
			return 130;
		}
		catch (Exception ex)
		{
			Console.Error("{0}: {1}", ex.GetType().Name, ex.Message);
			if (ex.InnerException != null)
				Console.Error(
					"Inner: {0}: {1}",
					ex.InnerException.GetType().Name,
					ex.InnerException.Message
				);
			if (ex.StackTrace != null)
			{
				var firstStackLine = ex.StackTrace.Split('\n')[0].Trim();
				Console.Dim($"Stack: {firstStackLine}");
			}

			var summary =
				ex.InnerException != null
					? $"{ex.GetType().Name}: {ex.Message} (Inner: {ex.InnerException.Message})"
					: $"{ex.GetType().Name}: {ex.Message}";

			Logger.End(false, summary, ex);
			return 1;
		}
	}

	public sealed class Settings : CommandSettings
	{
		[CommandOption("-v|--verbose")]
		[Description("Debug logging")]
		[DefaultValue(false)]
		public bool Verbose { get; init; }

		[CommandOption("-r|--reset")]
		[Description("Clear cache first")]
		[DefaultValue(false)]
		public bool Reset { get; init; }

		[CommandOption("-i|--session-id")]
		[Description("Show session ID")]
		[DefaultValue(false)]
		public bool ShowSessionId { get; init; }
	}
}

#endregion

#region Sync LastFm Command

public sealed class SyncLastFmCommand : AsyncCommand<SyncLastFmCommand.Settings>
{
	public override async Task<int> ExecuteAsync(
		CommandContext context,
		Settings settings,
		CancellationToken cancellationToken
	)
	{
		if (settings.Verbose)
		{
			Console.Level = LogLevel.Debug;
			Logger.FileLevel = LogLevel.Debug;
		}

		if (settings.Reset)
		{
			Console.Info("Clearing Last.fm local cache...");
			StateManager.DeleteLastFmStates();
			Console.Success("Cache cleared");
		}

		DateTime? sinceDate = null;
		if (!IsNullOrEmpty(settings.Since))
		{
			if (
				!DateTime.TryParseExact(
					settings.Since,
					"yyyy/MM/dd",
					null,
					DateTimeStyles.None,
					out DateTime parsed
				)
			)
			{
				Console.Error("Invalid date format. Use yyyy/MM/dd (e.g. 2024/01/01)");
				return 1;
			}

			sinceDate = parsed;
			Console.Warning(
				"Will delete existing data on/after {0} and re-sync",
				sinceDate.Value.ToString("yyyy/MM/dd")
			);
		}

		Logger.Start(ServiceType.LastFm);

		return await SyncYouTubeCommand.ExecuteWithErrorHandlingAsync(async () =>
			await new ScrobbleSyncOrchestrator(sinceDate, Program.cts.Token).ExecuteAsync()
		);
	}

	public sealed class Settings : CommandSettings
	{
		[CommandOption("-v|--verbose")]
		[Description("Debug logging")]
		public bool Verbose { get; init; }

		[CommandOption("-r|--reset")]
		[Description("Clear cache first")]
		public bool Reset { get; init; }

		[CommandOption("--since")]
		[Description("Sync from date (yyyy/MM/dd)")]
		public string? Since { get; init; }
	}
}

#endregion

#region Status Command

public sealed class StatusCommand : Command<StatusCommand.Settings>
{
	public override int Execute(
		CommandContext context,
		Settings settings,
		CancellationToken cancellationToken
	)
	{
		var checkLastFm = IsNullOrEmpty(settings.Service) || settings.Service.Equals("lastfm");
		var checkYouTube =
			IsNullOrEmpty(settings.Service)
			|| settings.Service.Equals("yt")
			|| settings.Service.Equals("youtube");

		if (checkLastFm)
			ShowLastFmStatus();

		if (checkYouTube)
			ShowYouTubeStatus();

		return 0;
	}

	private static void ShowLastFmStatus()
	{
		Console.Info("=== Last.fm ===");
		var stateFile = Combine(Paths.StateDirectory, StateManager.LastFmSyncFile);
		var hasState = File.Exists(stateFile);
		var spreadsheetUrl = $"https://docs.google.com/spreadsheets/d/{Secrets.LastFmSpreadsheetId}";

		if (hasState)
		{
			var json = ReadAllText(stateFile);
			FetchState state =
				JsonSerializer.Deserialize<FetchState>(json, StateManager.JsonIndented)
				?? new FetchState();
			Console.Info("Scrobbles: {0}", state.TotalFetched);
			Console.Info("Cached: Yes");
			Console.Info("Last sync: {0}", state.LastUpdated.ToString("yyyy/MM/dd HH:mm:ss"));
			Console.Link(spreadsheetUrl, "Spreadsheet");
		}
		else
		{
			Console.Info("Cached: No");
			Console.Link(spreadsheetUrl, "Spreadsheet");
		}

		Console.NewLine();
	}

	private static void ShowYouTubeStatus()
	{
		Console.Info("=== YouTube ===");
		var stateFile = Combine(Paths.StateDirectory, StateManager.YoutubeSyncFile);
		var cached = File.Exists(stateFile);

		if (cached)
		{
			var json = ReadAllText(stateFile);
			YouTubeFetchState state =
				JsonSerializer.Deserialize<YouTubeFetchState>(json, StateManager.JsonIndented)
				?? new YouTubeFetchState();
			var totalVideos = state.PlaylistSnapshots.Values.Sum(s => s.VideoIds.Count);
			var spreadsheetUrl = $"https://docs.google.com/spreadsheets/d/{state.SpreadsheetId}";

			if (!state.FetchComplete)
				Console.Warning("Fetch incomplete - run sync to resume");

			Console.Info(
				"Playlists: {0} | Videos: {1}",
				state.PlaylistSnapshots.Count,
				totalVideos
			);
			Console.Info("Cached: Yes");
			Console.Info("Last sync: {0}", state.LastUpdated.ToString("yyyy/MM/dd HH:mm:ss"));
			Console.Link(spreadsheetUrl, "Spreadsheet");
		}
		else
		{
			Console.Info("Cached: No");
		}

		Console.NewLine();
	}

	public sealed class Settings : CommandSettings
	{
		[CommandArgument(0, "[service]")]
		[Description("yt, lastfm (omit for all)")]
		[AllowedValues("yt", "youtube", "lastfm", "all")]
		public string Service { get; init; } = "all";
	}
}

#endregion
