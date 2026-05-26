using System.ComponentModel;

namespace CSharpScripts.CLI.Clean;

internal sealed class CleanCacheCommand : Command<CleanCacheCommand.Settings>
{
	protected override int Execute(
		CommandContext context,
		Settings settings,
		CancellationToken cancellationToken
	)
	{
		var cleanAll = settings.Service.EqualsIgnoreCase(other: "all");
		var cleanLastFm = cleanAll || settings.Service.EqualsIgnoreCase(other: "lastfm");
		var cleanYouTube =
			cleanAll
			|| settings.Service.EqualsIgnoreCase(other: "youtube")
			|| settings.Service.EqualsIgnoreCase(other: "yt");

		if (!cleanLastFm && !cleanYouTube)
		{
			Ui.Warn(message: "Invalid service: {0}. Use: yt, lastfm, or all", settings.Service);
			return 1;
		}

		Ui.Rule(text: "Clean Cache");

		if (cleanLastFm)
		{
			Ui.Info(message: "Cleaning Last.fm local cache...");
			StateManager.DeleteLastFmStates();
			Ui.Ok(message: "  Cache files deleted");
			Log.Information(messageTemplate: "CleanLastFmCache");
		}

		if (cleanYouTube)
		{
			Ui.Info(message: "Cleaning YouTube local cache...");
			StateManager.DeleteAllYouTubeStates();
			Ui.Ok(message: "  Cache files deleted");
			Log.Information(messageTemplate: "CleanYouTubeCache");
		}

		Ui.NewLine();
		Ui.Ok(message: "Cache clean complete");

		return 0;
	}

	internal sealed class Settings : CommandSettings
	{
		[CommandArgument(position: 0, template: "[service]")]
		[Description(description: "yt, lastfm, all (default: all)")]
		[DefaultValue(value: "all")]
		[AllowedValues("yt", "youtube", "lastfm", "all")]
		public string Service { get; init; } = "all";
	}
}
