using System.ComponentModel;
using CSharpScripts.Services.Music;

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
			service: ServiceType.Music,
			async () =>
			{
				var discogsToken = Secrets.DiscogsToken;
				if (IsNullOrEmpty(value: discogsToken))
				{
					Ui.Error(message: "DISCOGS_USER_TOKEN not set");
					return;
				}

				using DiscogsService discogs = new(token: discogsToken);
				Ui.Info(message: "Fetching Discogs release {0}...", settings.Id);

				ReleaseData release = await discogs.GetReleaseAsync(
					releaseId: settings.Id,
					ct: cancellationToken
				);

				var notes = release.Info.Notes;
				if (IsNullOrWhiteSpace(value: notes))
				{
					Ui.Warn(message: "Release '{0}' has no notes field.", release.Info.Title);
					return;
				}

				ParsedNotes parsed = NotesParserService.Parse(notes: notes);
				DisplayParsedNotes(id: settings.Id, title: release.Info.Title, parsed: parsed);
			}
		);
	}

	private static void DisplayParsedNotes(string id, string title, ParsedNotes parsed)
	{
		Ui.NewLine();
		Ui.Rule($"Discogs Release Notes \u2014 ID: {id}");
		Ui.NewLine();
		Ui.Field(label: "Title:", value: title);
		Ui.NewLine();

		if (parsed.Composers.Count > 0)
			Ui.Field(label: "Composers:", Join(separator: ", ", values: parsed.Composers));
		if (parsed.Conductors.Count > 0)
			Ui.Field(label: "Conductor:", Join(separator: ", ", values: parsed.Conductors));
		if (parsed.Orchestras.Count > 0)
			Ui.Field(label: "Orchestra:", Join(separator: ", ", values: parsed.Orchestras));
		if (parsed.Venues.Count > 0)
			Ui.Field(label: "Venue:", Join(separator: ", ", values: parsed.Venues));

		if (parsed.RecordingDates.Count > 0)
		{
			Ui.NewLine();
			Ui.Rule(text: "Recording Dates");
			foreach (RecordingDate rd in parsed.RecordingDates)
			{
				var dateDisplay = rd.Date.HasValue
					? DateFormatter.FormatForCli(dt: rd.Date.Value)
					: "(unparsed)";
				Ui.LabelValue(label: dateDisplay, value: rd.Description);
			}
		}

		if (parsed.TrackAnnotations.Count > 0)
		{
			Ui.NewLine();
			Ui.Rule(text: "Track Annotations");
			SpectreTable table = new();
			table.Border(border: TableBorder.Simple);
			table.AddColumn(column: "Track");
			table.AddColumn(column: "Annotation");
			foreach (TrackAnnotation ta in parsed.TrackAnnotations)
			{
				table.AddRow(
					Markup.Escape(text: ta.TrackReference),
					Markup.Escape(text: ta.Annotation)
				);
			}
			AnsiConsole.Write(renderable: table);
		}

		Ui.NewLine();
		Ui.Rule(text: "Raw Notes");
		Ui.NewLine();
		AnsiConsole.WriteLine(Markup.Escape(text: parsed.RawNotes));
	}

	internal sealed class Settings : CommandSettings
	{
		[CommandArgument(position: 0, template: "<id>")]
		[Description(description: "Discogs release ID (numeric)")]
		public string Id { get; init; } = "";

		public override ValidationResult Validate()
		{
			if (IsNullOrEmpty(value: Id))
				return ValidationResult.Error(message: "<id> is required");
			if (!int.TryParse(s: Id, result: out _))
				return ValidationResult.Error(message: "<id> must be a numeric Discogs release ID");

			return ValidationResult.Success();
		}
	}
}
