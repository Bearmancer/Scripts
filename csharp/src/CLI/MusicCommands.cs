namespace CSharpScripts.CLI.Commands;

public sealed class MusicSearchCommand : AsyncCommand<MusicSearchCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandOption("-a|--artist")]
        [Description("Artist name")]
        public string? Artist { get; init; }

        [CommandOption("-l|--album")]
        [Description("Album/release title")]
        public string? Album { get; init; }

        [CommandOption("-t|--track")]
        [Description("Track title (Discogs only)")]
        public string? Track { get; init; }

        [CommandOption("-y|--year")]
        [Description("Release year")]
        public int? Year { get; init; }

        [CommandOption("-s|--source")]
        [Description("musicbrainz, discogs, both")]
        [DefaultValue("both")]
        public string Source { get; init; } = "both";

        [CommandOption("-n|--limit")]
        [Description("Max results")]
        [DefaultValue(10)]
        public int Limit { get; init; } = 10;
    }

    public override async Task<int> ExecuteAsync(
        CommandContext context,
        Settings settings,
        CancellationToken cancellationToken
    )
    {
        if (
            IsNullOrWhiteSpace(settings.Artist)
            && IsNullOrWhiteSpace(settings.Album)
            && IsNullOrWhiteSpace(settings.Track)
        )
        {
            Console.Error("Specify at least one of: --artist, --album, --track");
            return 1;
        }

        MusicSearchQuery query = new(
            Artist: settings.Artist,
            Album: settings.Album,
            Track: settings.Track,
            Year: settings.Year,
            MaxResults: settings.Limit
        );

        string? discogsToken = GetEnvironmentVariable("DISCOGS_USER_TOKEN");
        MusicMetadataService service = new(discogsToken);

        Console.Info("Searching {0}...", settings.Source);
        Console.NewLine();

        (List<DiscogsSearchResult>? discogs, List<MusicBrainzSearchResult> musicbrainz) =
            await service.SearchBothAsync(query, settings.Source.ToLowerInvariant());

        if (musicbrainz.Count > 0)
        {
            Console.Rule("MusicBrainz Results");
            foreach (MusicBrainzSearchResult result in musicbrainz.Take(settings.Limit))
            {
                Console.Info(
                    "{0} - {1} ({2})",
                    result.Artist ?? "Unknown",
                    result.Title,
                    result.Year?.ToString() ?? "?"
                );
                Console.Dim($"  ID: {result.Id}");
            }
            Console.NewLine();
        }

        if (discogs?.Count > 0)
        {
            Console.Rule("Discogs Results");
            foreach (DiscogsSearchResult result in discogs.Take(settings.Limit))
            {
                Console.Info(
                    "{0} - {1} ({2})",
                    result.Artist ?? "Unknown",
                    result.Title ?? "Unknown",
                    result.Year?.ToString() ?? "?"
                );
                Console.Dim($"  ID: {result.ReleaseId} | Label: {result.Label ?? "?"}");
            }
            Console.NewLine();
        }

        if (musicbrainz.Count == 0 && (discogs?.Count ?? 0) == 0)
        {
            Console.Warning("No results found.");
        }
        else
        {
            Console.Success(
                "Found {0} MusicBrainz + {1} Discogs results",
                musicbrainz.Count,
                discogs?.Count ?? 0
            );
        }

        return 0;
    }
}

public sealed class MusicLookupCommand : AsyncCommand<MusicLookupCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<id>")]
        [Description("Release ID")]
        public required string Id { get; init; }

        [CommandOption("-s|--source")]
        [Description("musicbrainz, discogs")]
        [DefaultValue("musicbrainz")]
        public string Source { get; init; } = "musicbrainz";

        [CommandOption("-c|--credits")]
        [Description("Include credits")]
        [DefaultValue(false)]
        public bool IncludeCredits { get; init; }
    }

    public override async Task<int> ExecuteAsync(
        CommandContext context,
        Settings settings,
        CancellationToken cancellationToken
    )
    {
        string? discogsToken = GetEnvironmentVariable("DISCOGS_USER_TOKEN");
        MusicMetadataService service = new(discogsToken);

        Console.Info("Looking up {0} from {1}...", settings.Id, settings.Source);
        Console.NewLine();

        if (settings.Source.Equals("musicbrainz", StringComparison.OrdinalIgnoreCase))
        {
            if (!Guid.TryParse(settings.Id, out Guid mbId))
            {
                Console.Error("Invalid MusicBrainz ID (must be GUID)");
                return 1;
            }

            MusicBrainzRelease? release = await service.MusicBrainz.GetReleaseAsync(mbId);

            if (release is null)
            {
                Console.Warning("Release not found");
                return 1;
            }

            Console.Rule(release.Title);
            Console.KeyValue("Artist", release.Artist ?? release.ArtistCredit ?? "Unknown");
            Console.KeyValue("Date", release.Date?.ToString() ?? "Unknown");
            Console.KeyValue("Country", release.Country ?? "Unknown");
            Console.KeyValue("Status", release.Status ?? "Unknown");
            if (!IsNullOrEmpty(release.Barcode))
                Console.KeyValue("Barcode", release.Barcode);

            if (release.Tracks.Count > 0)
            {
                Console.NewLine();
                Console.Info("Tracks ({0}):", release.Tracks.Count);
                foreach (MusicBrainzTrack track in release.Tracks)
                {
                    string duration = track.Length?.ToString(@"m\:ss") ?? "?:??";
                    Console.Dim($"  {track.Position}. {track.Title} ({duration})");
                }
            }

            if (settings.IncludeCredits && release.Credits.Count > 0)
            {
                Console.NewLine();
                Console.Info("Credits ({0}):", release.Credits.Count);
                foreach (MusicBrainzCredit credit in release.Credits)
                {
                    Console.Dim($"  {credit.Name} - {credit.Role}");
                }
            }
        }
        else if (settings.Source.Equals("discogs", StringComparison.OrdinalIgnoreCase))
        {
            if (!int.TryParse(settings.Id, out int discogsId))
            {
                Console.Error("Invalid Discogs ID (must be number)");
                return 1;
            }

            if (service.Discogs is null)
            {
                Console.CriticalFailure(
                    "Discogs",
                    "DISCOGS_USER_TOKEN environment variable not set"
                );
                return 1;
            }

            DiscogsRelease? release;
            try
            {
                release = await service.Discogs.GetReleaseAsync(discogsId);
            }
            catch (Exception ex)
            {
                Console.CriticalFailure("Discogs", ex.Message);
                return 1;
            }

            if (release is null)
            {
                Console.Warning("Release not found");
                return 1;
            }

            Console.Rule(release.Title);
            Console.KeyValue("Artist", Join(", ", release.Artists.Select(a => a.Name)));
            Console.KeyValue("Year", release.Year.ToString());
            Console.KeyValue("Country", release.Country ?? "Unknown");
            Console.KeyValue("Labels", Join(", ", release.Labels.Select(l => l.Name)));
            Console.KeyValue("Genres", Join(", ", release.Genres));
            Console.KeyValue("Styles", Join(", ", release.Styles));

            if (release.Tracks.Count > 0)
            {
                Console.NewLine();
                Console.Info("Tracks ({0}):", release.Tracks.Count);
                foreach (DiscogsTrack track in release.Tracks)
                {
                    Console.Dim($"  {track.Position}. {track.Title} ({track.Duration ?? "?:??"})");
                }
            }

            if (settings.IncludeCredits && release.Credits.Count > 0)
            {
                Console.NewLine();
                Console.Info("Credits ({0}):", release.Credits.Count);
                foreach (DiscogsCredit credit in release.Credits)
                {
                    Console.Dim($"  {credit.Name} - {credit.Role}");
                }
            }
        }
        else
        {
            Console.Error("Invalid source: {0}. Use: musicbrainz, discogs", settings.Source);
            return 1;
        }

        return 0;
    }
}
