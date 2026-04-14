namespace CSharpScripts.CLI.Music;

internal sealed class MusicNotesCommand : BaseAsyncCommand<MusicNotesCommand.Settings>
{
	protected override async Task<int> ExecuteAsync(
		CommandContext context,
		Settings settings,
		CancellationToken cancellationToken
	)
	{
		return await ExecuteWithErrorHandlingAsync(
			ServiceType.Music,
			async () =>
			{
				var discogsToken = Secrets.DiscogsToken;
				if (IsNullOrEmpty(discogsToken))
				{
					UI.Error("DISCOGS_USER_TOKEN not set");
					return;
				}

				using DiscogsService discogs = new(discogsToken);
				UI.Info("Fetching Discogs release {0}...", settings.Id);

				ReleaseData release = await discogs.GetReleaseAsync(
					settings.Id,
					ct: cancellationToken
				);

				var notes = release.Info.Notes;
				if (IsNullOrWhiteSpace(notes))
				{
					UI.Warn("Release '{0}' has no notes field.", release.Info.Title);
					return;
				}

				ParsedNotes parsed = NotesParserService.Parse(notes);
				DisplayParsedNotes(settings.Id, release.Info.Title, parsed);
			}
		);
	}

	private static void DisplayParsedNotes(string id, string title, ParsedNotes parsed)
	{
		UI.NewLine();
		UI.Rule($"Discogs Release Notes \u2014 ID: {id}");
		UI.NewLine();
		UI.Field("Title:", title);
		UI.NewLine();

		if (parsed.Composers.Count > 0)
			UI.Field("Composers:", Join(", ", parsed.Composers));
		if (parsed.Conductors.Count > 0)
			UI.Field("Conductor:", Join(", ", parsed.Conductors));
		if (parsed.Orchestras.Count > 0)
			UI.Field("Orchestra:", Join(", ", parsed.Orchestras));
		if (parsed.Venues.Count > 0)
			UI.Field("Venue:", Join(", ", parsed.Venues));

		if (parsed.RecordingDates.Count > 0)
		{
			UI.NewLine();
			UI.Rule("Recording Dates");
			foreach (RecordingDate rd in parsed.RecordingDates)
			{
				var dateDisplay = rd.Date.HasValue
					? DateFormatter.FormatForCli(rd.Date.Value)
					: "(unparsed)";
				UI.LabelValue(dateDisplay, rd.Description);
			}
		}

		if (parsed.TrackAnnotations.Count > 0)
		{
			UI.NewLine();
			UI.Rule("Track Annotations");
			SpectreTable table = new();
			HasTableBorderExtensions.Border(table, TableBorder.Simple);
			TableExtensions.AddColumn(table, "Track");
			TableExtensions.AddColumn(table, "Annotation");
			foreach (TrackAnnotation ta in parsed.TrackAnnotations)
			{
				TableExtensions.AddRow(
					table,
					Markup.Escape(ta.TrackReference),
					Markup.Escape(ta.Annotation)
				);
			}
			AnsiConsole.Write(table);
		}

		UI.NewLine();
		UI.Rule("Raw Notes");
		UI.NewLine();
		AnsiConsole.WriteLine(Markup.Escape(parsed.RawNotes));
	}

	internal sealed class Settings : CommandSettings
	{
		[CommandArgument(0, "<id>")]
		[Description("Discogs release ID (numeric)")]
		public string Id { get; init; } = "";

		public override ValidationResult Validate()
		{
			if (IsNullOrEmpty(Id))
				return ValidationResult.Error("<id> is required");
			if (!int.TryParse(Id, out _))
				return ValidationResult.Error("<id> must be a numeric Discogs release ID");

			return ValidationResult.Success();
		}
	}
}
