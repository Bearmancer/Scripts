namespace CSharpScripts.CLI.Commands;

public sealed class MusicSearchCommand : AsyncCommand<MusicSearchCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<query>")]
        [Description("Free-text search (e.g. 'Bowie Heroes 1977')")]
        public required string Query { get; init; }

        [CommandOption("-s|--source")]
        [Description("discogs (default), musicbrainz (or mb), both")]
        [DefaultValue("discogs")]
        public string Source { get; init; } = "discogs";

        [CommandOption("-t|--type")]
        [Description("Filter: album, ep, single, compilation (normalized across APIs)")]
        public string? Type { get; init; }

        [CommandOption("-n|--limit")]
        [Description("Max results per source (default 10)")]
        [DefaultValue(10)]
        public int Limit { get; init; } = 10;

        [CommandOption("--debug")]
        [Description("Verbose output: filter stats, extra columns")]
        [DefaultValue(false)]
        public bool Debug { get; init; }
    }

    public override async Task<int> ExecuteAsync(
        CommandContext context,
        Settings settings,
        CancellationToken cancellationToken
    )
    {
        string? discogsToken = GetEnvironmentVariable("DISCOGS_USER_TOKEN");
        string source = settings.Source.ToLowerInvariant();

        bool searchMusicBrainz = source is "musicbrainz" or "mb" or "both";
        bool searchDiscogs = source is "discogs" or "both";

        if (searchDiscogs && IsNullOrEmpty(discogsToken))
        {
            Console.Warning("DISCOGS_USER_TOKEN not set, using MusicBrainz");
            searchDiscogs = false;
            searchMusicBrainz = true;
        }

        string sourceLabel =
            searchMusicBrainz && searchDiscogs ? "Discogs + MusicBrainz"
            : searchDiscogs ? "Discogs"
            : "MusicBrainz";

        Console.Info("Searching {0}...", sourceLabel);

        List<Models.SearchResult> results = [];
        int filteredCount = 0;

        if (searchMusicBrainz)
        {
            MusicBrainzService mb = new();
            List<Models.SearchResult> mbResults = await mb.SearchAsync(
                settings.Query,
                settings.Limit
            );
            results.AddRange(mbResults);
        }

        if (searchDiscogs)
        {
            DiscogsService discogs = new(discogsToken);
            List<Models.SearchResult> discogsResults = await discogs.SearchAsync(
                settings.Query,
                settings.Limit
            );
            results.AddRange(discogsResults);
        }

        // Apply normalized type filter
        if (!IsNullOrEmpty(settings.Type))
        {
            int beforeCount = results.Count;
            string normalizedFilter = NormalizeType(settings.Type);

            results = results.Where(r => MatchesType(r, normalizedFilter)).ToList();
            filteredCount = beforeCount - results.Count;

            if (settings.Debug)
            {
                Console.Dim(
                    $"[DEBUG] Filter '{settings.Type}' -> normalized '{normalizedFilter}', removed {filteredCount}"
                );
            }
        }

        if (results.Count == 0)
        {
            Console.Warning("No results found.");
            return 0;
        }

        // Tabulated output
        SpectreTable table = new();
        table.Border(TableBorder.Rounded);
        table.AddColumn("Artist");
        table.AddColumn("Title");
        table.AddColumn("Year");
        table.AddColumn("Type");
        table.AddColumn("ID");

        if (settings.Debug)
        {
            table.AddColumn("Source");
            table.AddColumn("Label");
            table.AddColumn("Format");
        }

        foreach (Models.SearchResult r in results)
        {
            string year = r.Year?.ToString() ?? "[dim]?[/]";
            string type = NormalizeTypeForDisplay(r.ReleaseType) ?? "[dim]?[/]";
            string artist = r.Artist ?? "[dim]Unknown[/]";
            string idLink = MakeIdLink(r);

            if (settings.Debug)
            {
                string src =
                    r.Source == MusicSource.Discogs ? "[yellow]Discogs[/]" : "[cyan]MusicBrainz[/]";
                table.AddRow(
                    Markup.Escape(artist),
                    Markup.Escape(r.Title),
                    year,
                    type,
                    idLink,
                    src,
                    Markup.Escape(r.Label ?? "?"),
                    Markup.Escape(r.Format ?? "?")
                );
            }
            else
            {
                table.AddRow(Markup.Escape(artist), Markup.Escape(r.Title), year, type, idLink);
            }
        }

        AnsiConsole.Write(table);

        Console.Success(
            "Found {0} results{1}",
            results.Count,
            filteredCount > 0 ? $" ({filteredCount} filtered)" : ""
        );

        return 0;
    }

    /// <summary>
    /// Normalize user input to standard type names.
    /// </summary>
    static string NormalizeType(string input) =>
        input.ToLowerInvariant() switch
        {
            "album" => "album",
            "ep" => "ep",
            "single" => "single",
            "compilation" => "compilation",
            "master" => "master", // Discogs-specific
            "release" => "release", // Discogs-specific
            _ => input.ToLowerInvariant(),
        };

    /// <summary>
    /// Check if a result matches the normalized type filter.
    /// Handles API differences: MusicBrainz uses "Album", Discogs uses "master"/"release".
    /// </summary>
    static bool MatchesType(Models.SearchResult r, string filter)
    {
        if (IsNullOrEmpty(r.ReleaseType))
            return false;

        string normalized = r.ReleaseType.ToLowerInvariant();

        return filter switch
        {
            // "album" matches MusicBrainz "Album" OR Discogs "master" (canonical album)
            "album" => normalized is "album" or "master",
            "ep" => normalized.Contains("ep"),
            "single" => normalized.Contains("single"),
            "compilation" => normalized.Contains("compilation"),
            "master" => normalized is "master",
            "release" => normalized is "release",
            _ => normalized.Contains(filter),
        };
    }

    /// <summary>
    /// Normalize type for display consistency.
    /// </summary>
    static string? NormalizeTypeForDisplay(string? type) =>
        type?.ToLowerInvariant() switch
        {
            "album" => "Album",
            "ep" => "EP",
            "single" => "Single",
            "compilation" => "Compilation",
            "master" => "Master",
            "release" => "Release",
            _ => type,
        };

    /// <summary>
    /// Create hyperlinked ID for terminal (uses ANSI escape sequences).
    /// </summary>
    static string MakeIdLink(Models.SearchResult r)
    {
        string url =
            r.Source == MusicSource.Discogs
                ? $"https://www.discogs.com/release/{r.Id}"
                : $"https://musicbrainz.org/release/{r.Id}";

        // Spectre.Console supports [link] markup
        return $"[link={url}]{r.Id}[/]";
    }
}

public sealed class MusicLookupCommand : AsyncCommand<MusicLookupCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<id>")]
        [Description("Release ID (GUID for MusicBrainz, number for Discogs)")]
        public required string Id { get; init; }

        [CommandOption("-s|--source")]
        [Description("discogs (default), musicbrainz (or mb)")]
        [DefaultValue("discogs")]
        public string Source { get; init; } = "discogs";
    }

    public override async Task<int> ExecuteAsync(
        CommandContext context,
        Settings settings,
        CancellationToken cancellationToken
    )
    {
        Console.Info("Looking up {0} from {1}...", settings.Id, settings.Source);

        IMusicService service;

        if (
            settings.Source.Equals("musicbrainz", StringComparison.OrdinalIgnoreCase)
            || settings.Source.Equals("mb", StringComparison.OrdinalIgnoreCase)
        )
        {
            if (!Guid.TryParse(settings.Id, out _))
            {
                Console.Error("Invalid MusicBrainz ID (must be GUID)");
                return 1;
            }
            service = new MusicBrainzService();
        }
        else if (settings.Source.Equals("discogs", StringComparison.OrdinalIgnoreCase))
        {
            if (!int.TryParse(settings.Id, out _))
            {
                Console.Error("Invalid Discogs ID (must be number)");
                return 1;
            }
            string? token = GetEnvironmentVariable("DISCOGS_USER_TOKEN");
            if (IsNullOrEmpty(token))
            {
                Console.CriticalFailure("Discogs", "DISCOGS_USER_TOKEN not set");
                return 1;
            }
            service = new DiscogsService(token);
        }
        else
        {
            Console.Error(
                "Invalid source: {0}. Use: discogs, musicbrainz (or mb)",
                settings.Source
            );
            return 1;
        }

        List<TrackMetadata> tracks = await service.GetReleaseTracksAsync(settings.Id);

        if (tracks.Count == 0)
        {
            Console.Warning("No tracks found.");
            return 0;
        }

        // Header
        Console.Rule(tracks[0].Album);
        Console.KeyValue("Artist", tracks[0].Artist ?? "Unknown");
        Console.KeyValue("Label", tracks[0].Label ?? "Unknown");
        Console.KeyValue("Year", tracks[0].FirstIssuedYear?.ToString() ?? "Unknown");

        if (!IsNullOrEmpty(tracks[0].Composer))
            Console.KeyValue("Composer", tracks[0].Composer!);
        if (!IsNullOrEmpty(tracks[0].Conductor))
            Console.KeyValue("Conductor", tracks[0].Conductor!);
        if (!IsNullOrEmpty(tracks[0].Orchestra))
            Console.KeyValue("Orchestra", tracks[0].Orchestra!);

        // Track table
        SpectreTable table = new();
        table.Border(TableBorder.Simple);
        table.AddColumn("#");
        table.AddColumn("Title");
        table.AddColumn("Duration");

        foreach (TrackMetadata track in tracks)
        {
            string duration = track.Duration?.ToString(@"m\:ss") ?? "?:??";
            table.AddRow(
                $"{track.DiscNumber}.{track.TrackNumber}",
                Markup.Escape(track.Title),
                duration
            );
        }

        AnsiConsole.Write(table);

        return 0;
    }
}
