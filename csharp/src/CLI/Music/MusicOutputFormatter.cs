namespace CSharpScripts.CLI.Music;

internal static class MusicOutputFormatter
{
	internal static void DisplayFillResults(
		List<RecordingWithSuggestions> results,
		MusicGenreCategory genre = MusicGenreCategory.Unknown
	)
	{
		UI.NewLine();
		UI.Rule("Search Results");
		UI.NewLine();

		var suggestionsFound = 0;
		foreach (RecordingWithSuggestions item in results)
		{
			if (!item.Suggestions.HasAny())
				continue;

			suggestionsFound++;

			switch (genre)
			{
				case MusicGenreCategory.Classical:
					DisplayClassicalItem(item);
					break;
				case MusicGenreCategory.Pop:
					DisplayPopItem(item);
					break;
				case MusicGenreCategory.Jazz:
					DisplayJazzItem(item);
					break;
				default:
					DisplayClassicalItem(item);
					break;
			}

			UI.NewLine();
		}

		UI.Info("Found suggestions for {0} of {1} recordings", suggestionsFound, results.Count);
	}

	private static void DisplayClassicalItem(RecordingWithSuggestions item)
	{
		var work = item.Original.Work ?? "(Unknown Work)";
		var composer = item.Original.Composer ?? "(none)";

		UI.TitleWithSubtitle(work, composer);
		UI.InputHeader();

		if (!IsNullOrEmpty(item.Original.Conductor))
			UI.LabelValue("Conductor", item.Original.Conductor);
		if (!IsNullOrEmpty(item.Original.Orchestra))
			UI.LabelValue("Orchestra", item.Original.Orchestra);

		DisplayCommonInputFields(item.Original);
		DisplaySuggestions(item.Suggestions);
	}

	private static void DisplayPopItem(RecordingWithSuggestions item)
	{
		var title = item.Original.Work ?? "(Unknown Title)";
		var artist = item.Original.Performers ?? "(Unknown Artist)";

		UI.TitleWithSubtitle(title, artist);
		UI.InputHeader();
		DisplayCommonInputFields(item.Original);
		DisplaySuggestions(item.Suggestions);
	}

	private static void DisplayJazzItem(RecordingWithSuggestions item)
	{
		var title = item.Original.Work ?? "(Unknown Title)";
		var artist = item.Original.Performers ?? "(Unknown Artist)";

		UI.TitleWithSubtitle(title, artist);
		UI.InputHeader();

		if (!IsNullOrEmpty(item.Original.Orchestra))
			UI.LabelValue("Ensemble", item.Original.Orchestra);
		if (!IsNullOrEmpty(item.Original.Performers))
			UI.LabelValue("Personnel", item.Original.Performers);

		DisplayCommonInputFields(item.Original);
		DisplaySuggestions(item.Suggestions);
	}

	private static void DisplayCommonInputFields(RecordingInput record)
	{
		if (!IsNullOrEmpty(record.Label))
			UI.LabelValue("Label", record.Label);
		else
			UI.MissingField("Label");

		if (!IsNullOrEmpty(record.CatalogNumber))
			UI.LabelValue("Catalog #", record.CatalogNumber);
		else
			UI.MissingField("Catalog #");

		if (!IsNullOrEmpty(record.Year))
			UI.LabelValue("Year", record.Year);
		else
			UI.MissingField("Year");
	}

	private static void DisplaySuggestions(SuggestionSet suggestions)
	{
		if (suggestions.Items.Count == 0)
			return;

		UI.FoundHeader();
		foreach (SuggestionBundle bundle in suggestions.Items)
			UI.ConfidenceResult(
				bundle.Confidence,
				bundle.Label,
				bundle.CatalogNumber,
				bundle.Year,
				bundle.Source
			);
	}

	internal static void DisplaySearchResults(List<SearchResult> results)
	{
		SpectreTable table = new();
		table.Border(TableBorder.Rounded);
		table.AddColumn("Artist");
		table.AddColumn("Title");
		table.AddColumn("Year");
		table.AddColumn("Type");
		table.AddColumn("ID");
		table.AddColumn("Source");

		foreach (SearchResult r in results)
		{
			var source =
				r.Source == MusicSource.Discogs ? UI.Yellow("Discogs") : UI.Cyan("MusicBrainz");
			var id =
				r.Source == MusicSource.Discogs
					? UI.LinkText($"https://www.discogs.com/release/{r.Id}", r.Id)
					: UI.LinkText($"https://musicbrainz.org/release/{r.Id}", r.Id);

			table.AddRow(
				Markup.Escape(r.Artist ?? ""),
				Markup.Escape(r.Title),
				r.Year?.ToString(CultureInfo.InvariantCulture) ?? "",
				Markup.Escape(r.ReleaseType ?? ""),
				id,
				source
			);
		}

		AnsiConsole.Write(table);
		UI.Info("{0} results", results.Count);
	}

	internal static void DisplayReleaseData(ReleaseData release)
	{
		ReleaseInfo info = release.Info;

		UI.NewLine();
		UI.Rule("Release Info");
		UI.NewLine();
		UI.Field("Release:", info.Title);
		UI.Field("Artist:", info.Artist);
		UI.Field("Year:", info.Year?.ToString());
		UI.Field("Label:", info.Label);
		UI.Field("Catalog:", info.CatalogNumber);
		UI.Field("Discs:", info.DiscCount.ToString());
		UI.Field("Tracks:", info.TrackCount.ToString());

		if (info.TotalDuration.HasValue && info.TotalDuration.Value > TimeSpan.Zero)
		{
			TimeSpan td = info.TotalDuration.Value;
			var durationText =
				td.Days > 0 ? $"{td.Days}d {td.Hours}h {td.Minutes}m"
				: td.Hours > 0 ? $"{td.Hours}h {td.Minutes}m"
				: $"{td.Minutes}m {td.Seconds}s";
			UI.Field("Duration:", durationText);
		}

		UI.NewLine();

		SpectreTable table = new();
		table.Border(TableBorder.Simple);
		table.AddColumn("Disc");
		table.AddColumn("Track");
		table.AddColumn("Title");
		table.AddColumn("Duration");

		foreach (TrackInfo track in release.Tracks)
		{
			var duration = track.Duration is { } d && d > TimeSpan.Zero ? d.ToString(@"m\:ss") : "";
			table.AddRow(
				track.DiscNumber.ToString(),
				track.TrackNumber.ToString(),
				Markup.Escape(track.Title),
				duration
			);
		}

		AnsiConsole.Write(table);
	}
}
