namespace CSharpScripts.CLI.Clean;

internal sealed class CleanCacheCommand : Command<CleanCacheCommand.Settings>
{
	protected override int Execute(
		CommandContext context,
		Settings settings,
		CancellationToken cancellationToken
	)
	{
		var cleanAll = settings.Service.EqualsIgnoreCase("all");
		var cleanLastFm = cleanAll || settings.Service.EqualsIgnoreCase("lastfm");
		var cleanYouTube =
			cleanAll
			|| settings.Service.EqualsIgnoreCase("youtube")
			|| settings.Service.EqualsIgnoreCase("yt");

		if (!cleanLastFm && !cleanYouTube)
		{
			UI.Warn("Invalid service: {0}. Use: yt, lastfm, or all", settings.Service);
			return 1;
		}

		UI.Rule("Clean Cache");

		if (cleanLastFm)
		{
			UI.Info("Cleaning Last.fm local cache...");
			StateManager.DeleteLastFmStates();
			UI.Ok("  Cache files deleted");
			Log.Information("CleanLastFmCache");
		}

		if (cleanYouTube)
		{
			UI.Info("Cleaning YouTube local cache...");
			StateManager.DeleteAllYouTubeStates();
			UI.Ok("  Cache files deleted");
			Log.Information("CleanYouTubeCache");
		}

		UI.NewLine();
		UI.Ok("Cache clean complete");

		return 0;
	}

	internal sealed class Settings : CommandSettings
	{
		[CommandArgument(0, "[service]")]
		[Description("yt, lastfm, all (default: all)")]
		[DefaultValue("all")]
		[AllowedValues("yt", "youtube", "lastfm", "all")]
		public string Service { get; init; } = "all";
	}
}
