namespace CSharpScripts.CLI.Music;

internal static class MusicOutputFormatter
{
	internal static void DisplayFillResults(
		List<RecordingWithSuggestions> results,
		MusicGenreCategory genre = MusicGenreCategory.Unknown
	)
	{
		UI.NewLine();
		UI.Rule(text: "Search Results");
		UI.NewLine();

		var suggestionsFound = 0;
		foreach (RecordingWithSuggestions item in results)
		{
			if (!item.Suggestions.HasAny())
				continue;

			suggestionsFound++;

			Action displayAction = genre switch
			{
				MusicGenreCategory.Classical => () => DisplayClassicalItem(item: item),
				MusicGenreCategory.Pop => () => DisplayPopItem(item: item),
				MusicGenreCategory.Jazz => () => DisplayJazzItem(item: item),
				MusicGenreCategory.Unknown => () => { },
				_ => () => DisplayClassicalItem(item: item),
			};

			displayAction();

			UI.NewLine();
		}

		UI.Info(
			message: "Found suggestions for {0} of {1} recordings",
			suggestionsFound,
			results.Count
		);
	}

	private static void DisplayClassicalItem(RecordingWithSuggestions item)
	{
		var work = item.Original.Work ?? "(Unknown Work)";
		var composer = item.Original.Composer ?? "(none)";

		UI.TitleWithSubtitle(title: work, subtitle: composer);
		UI.InputHeader();

		if (!IsNullOrEmpty(value: item.Original.Conductor))
			UI.LabelValue(label: "Conductor", value: item.Original.Conductor);
		if (!IsNullOrEmpty(value: item.Original.Orchestra))
			UI.LabelValue(label: "Orchestra", value: item.Original.Orchestra);

		DisplayCommonInputFields(record: item.Original);
		DisplaySuggestions(suggestions: item.Suggestions);
	}

	private static void DisplayPopItem(RecordingWithSuggestions item)
	{
		var title = item.Original.Work ?? "(Unknown Title)";
		var artist = item.Original.Performers ?? "(Unknown Artist)";

		UI.TitleWithSubtitle(title: title, subtitle: artist);
		UI.InputHeader();
		DisplayCommonInputFields(record: item.Original);
		DisplaySuggestions(suggestions: item.Suggestions);
	}

	private static void DisplayJazzItem(RecordingWithSuggestions item)
	{
		var title = item.Original.Work ?? "(Unknown Title)";
		var artist = item.Original.Performers ?? "(Unknown Artist)";

		UI.TitleWithSubtitle(title: title, subtitle: artist);
		UI.InputHeader();

		if (!IsNullOrEmpty(value: item.Original.Orchestra))
			UI.LabelValue(label: "Ensemble", value: item.Original.Orchestra);
		if (!IsNullOrEmpty(value: item.Original.Performers))
			UI.LabelValue(label: "Personnel", value: item.Original.Performers);

		DisplayCommonInputFields(record: item.Original);
		DisplaySuggestions(suggestions: item.Suggestions);
	}

	private static void DisplayCommonInputFields(RecordingInput record)
	{
		if (!IsNullOrEmpty(value: record.Label))
			UI.LabelValue(label: "Label", value: record.Label);
		else
			UI.MissingField(label: "Label");

		if (!IsNullOrEmpty(value: record.CatalogNumber))
			UI.LabelValue(label: "Catalog #", value: record.CatalogNumber);
		else
			UI.MissingField(label: "Catalog #");

		if (!IsNullOrEmpty(value: record.Year))
			UI.LabelValue(label: "Year", value: record.Year);
		else
			UI.MissingField(label: "Year");
	}

	private static void DisplaySuggestions(SuggestionSet suggestions)
	{
		List<SuggestionBundle> items = suggestions.Items;

		if (items.Count == 0)
			return;

		UI.FoundHeader();
		foreach (SuggestionBundle bundle in items)
		{
			UI.ConfidenceResult(
				confidence: bundle.Confidence,
				label: bundle.Label,
				catalogNumber: bundle.CatalogNumber,
				year: bundle.Year,
				source: bundle.Source
			);
		}
	}

	internal static void DisplaySearchResults(List<SearchResult> results)
	{
		SpectreTable table = new();
		HasTableBorderExtensions.Border(table, border: TableBorder.Rounded);
		TableExtensions.AddColumn(table, column: "Artist");
		TableExtensions.AddColumn(table, column: "Title");
		TableExtensions.AddColumn(table, column: "Year");
		TableExtensions.AddColumn(table, column: "Type");
		TableExtensions.AddColumn(table, column: "ID");
		TableExtensions.AddColumn(table, column: "Source");

		foreach (SearchResult r in results)
		{
			var source =
				r.Source == MusicSource.Discogs
					? UI.Yellow(text: "Discogs")
					: UI.Cyan(text: "MusicBrainz");
			var id =
				r.Source == MusicSource.Discogs
					? UI.LinkText($"https://www.discogs.com/release/{r.Id}", text: r.Id)
					: UI.LinkText($"https://musicbrainz.org/release/{r.Id}", text: r.Id);

			TableExtensions.AddRow(
				table,
				Markup.Escape(r.Artist ?? ""),
				Markup.Escape(text: r.Title),
				r.Year?.ToString(provider: CultureInfo.InvariantCulture) ?? "",
				Markup.Escape(r.ReleaseType ?? ""),
				id,
				source
			);
		}

		AnsiConsole.Write(renderable: table);
		UI.Info(message: "{0} results", results.Count);
	}

	internal static void DisplayReleaseData(ReleaseData release)
	{
		ReleaseInfo info = release.Info;

		UI.NewLine();
		UI.Rule(text: "Release Info");
		UI.NewLine();
		UI.Field(label: "Release:", value: info.Title);
		UI.Field(label: "Artist:", value: info.Artist);
		UI.Field(label: "Year:", info.Year?.ToString());
		UI.Field(label: "Label:", value: info.Label);
		UI.Field(label: "Catalog:", value: info.CatalogNumber);
		UI.Field(label: "Discs:", info.DiscCount.ToString());
		UI.Field(label: "Tracks:", info.TrackCount.ToString());

		if (info.TotalDuration.HasValue && info.TotalDuration.Value > TimeSpan.Zero)
		{
			TimeSpan td = info.TotalDuration.Value;
			var durationText =
				td.Days > 0 ? $"{td.Days}d {td.Hours}h {td.Minutes}m"
				: td.Hours > 0 ? $"{td.Hours}h {td.Minutes}m"
				: $"{td.Minutes}m {td.Seconds}s";
			UI.Field(label: "Duration:", value: durationText);
		}

		UI.NewLine();

		SpectreTable table = new();
		HasTableBorderExtensions.Border(table, border: TableBorder.Simple);
		TableExtensions.AddColumn(table, column: "Disc");
		TableExtensions.AddColumn(table, column: "Track");
		TableExtensions.AddColumn(table, column: "Title");
		TableExtensions.AddColumn(table, column: "Duration");

		foreach (TrackInfo track in release.Tracks)
		{
			var duration =
				track.Duration is { } d && d > TimeSpan.Zero ? d.ToString(format: @"m\:ss") : "";
			TableExtensions.AddRow(
				table,
				track.DiscNumber.ToString(),
				track.TrackNumber.ToString(),
				Markup.Escape(text: track.Title),
				duration
			);
		}

		AnsiConsole.Write(renderable: table);
	}
}
