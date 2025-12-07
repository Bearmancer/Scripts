using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using CSharpScripts.Models;
using CSharpScripts.Services.Music;

namespace CSharpScripts.CLI;

/// <summary>
/// NAME
///   music - Search Discogs and MusicBrainz
///
/// DESCRIPTION
///   Commands to search and lookup music metadata from Discogs and
///   MusicBrainz databases. Search by artist, album, or track, and
///   lookup detailed release information by ID.
///
/// COMMANDS
///   search    Search for releases across Discogs and MusicBrainz
///   lookup    Get detailed release information by ID
///
/// ENVIRONMENT VARIABLES
///   DISCOGS_TOKEN    Required for Discogs searches (get from discogs.com)
///
/// EXAMPLES
///   cli music search --artist "David Bowie"
///   cli music search --album "Heroes" --year 1977
///   cli music lookup abc123 --source musicbrainz
/// </summary>
[Command("music", Description = "Search Discogs and MusicBrainz")]
public sealed class MusicGroupCommand : ICommand
{
    public ValueTask ExecuteAsync(IConsole console)
    {
        Console.Rule("Music Commands");
        Console.NewLine();

        Console.MarkupLine("[blue bold]COMMANDS[/]");
        Console.MarkupLine("  [cyan]search[/]    Search for releases by artist, album, or track");
        Console.MarkupLine("  [cyan]lookup[/]    Get detailed release information by ID");
        Console.NewLine();

        Console.MarkupLine("[blue bold]SEARCH OPTIONS[/]");
        Console.MarkupLine("  [cyan]-a, --artist[/]    Artist name");
        Console.MarkupLine("  [cyan]-l, --album[/]     Album/release title");
        Console.MarkupLine("  [cyan]-t, --track[/]     Track title [grey](Discogs only)[/]");
        Console.MarkupLine("  [cyan]-y, --year[/]      Release year");
        Console.MarkupLine(
            "  [cyan]-s, --source[/]    musicbrainz, discogs, both [grey](default: both)[/]"
        );
        Console.MarkupLine(
            "  [cyan]--limit[/]         Max results per source [grey](default: 10)[/]"
        );
        Console.NewLine();

        Console.MarkupLine("[blue bold]EXAMPLES[/]");
        Console.MarkupLine("  [dim]$[/] cli music search --artist \"Radiohead\"");
        Console.MarkupLine("  [dim]$[/] cli music search --album \"OK Computer\" --year 1997");
        Console.MarkupLine("  [dim]$[/] cli music lookup 12345 --source discogs");

        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// NAME
///   music search - Search Discogs and MusicBrainz
///
/// DESCRIPTION
///   Searches for releases across Discogs and/or MusicBrainz. At least one
///   of --artist, --album, or --track must be specified. Results include
///   release ID, title, year, and source for further lookup.
///
/// USAGE
///   cli music search [options]
///
/// OPTIONS
///   -a, --artist      Artist name to search for
///   -l, --album       Album/release title to search for
///   -t, --track       Track title to search for (Discogs only)
///   -y, --year        Filter by release year
///   -s, --source      Source to search: musicbrainz, discogs, both (default: both)
///   --limit           Maximum results per source (default: 10)
///
/// ENVIRONMENT VARIABLES
///   DISCOGS_TOKEN     Required for Discogs searches
///
/// EXAMPLES
///   cli music search --artist "David Bowie"
///   cli music search --artist "Radiohead" --album "OK Computer"
///   cli music search --track "Paranoid Android" --source discogs
///   cli music search --artist "Beatles" --year 1967 --limit 5
/// </summary>
[Command("music search", Description = "Search Discogs and MusicBrainz")]
public sealed class MusicSearchCommand : ICommand
{
    [CommandOption("artist", 'a', Description = "Artist name")]
    public string? Artist { get; init; }

    [CommandOption("album", 'l', Description = "Album/release title")]
    public string? Album { get; init; }

    [CommandOption("track", 't', Description = "Track title (Discogs only)")]
    public string? Track { get; init; }

    [CommandOption("year", 'y', Description = "Release year")]
    public int? Year { get; init; }

    [CommandOption("source", 's', Description = "musicbrainz, discogs, both")]
    public string Source { get; init; } = "both";

    [CommandOption("limit", Description = "Max results per source")]
    public int Limit { get; init; } = 10;

    public async ValueTask ExecuteAsync(IConsole console)
    {
        if (IsNullOrWhiteSpace(Artist) && IsNullOrWhiteSpace(Album) && IsNullOrWhiteSpace(Track))
        {
            Console.Error("Specify at least one of: --artist, --album, --track");
            return;
        }

        MusicSearchQuery query = new(
            Artist: Artist,
            Album: Album,
            Track: Track,
            Year: Year,
            MaxResults: Limit
        );

        string? discogsToken = GetEnvironmentVariable("DISCOGS_TOKEN");
        MusicMetadataService service = new(discogsToken);

        Console.Info("Searching {0}...", Source);
        Console.NewLine();

        (List<DiscogsSearchResult>? discogs, List<MusicBrainzSearchResult> musicbrainz) =
            await service.SearchBothAsync(query, Source.ToLowerInvariant());

        if (musicbrainz.Count > 0)
        {
            Console.Rule("MusicBrainz Results");
            foreach (MusicBrainzSearchResult result in musicbrainz.Take(Limit))
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
            foreach (DiscogsSearchResult result in discogs.Take(Limit))
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
    }
}

/// <summary>
/// NAME
///   music lookup - Get release details by ID
///
/// DESCRIPTION
///   Retrieves detailed information for a release by its ID. Specify the
///   source (MusicBrainz or Discogs) using the --source option. Returns
///   full metadata including tracks, credits, and identifiers.
///
/// USAGE
///   cli music lookup <id> [options]
///
/// ARGUMENTS
///   id          Release ID (GUID for MusicBrainz, number for Discogs)
///
/// OPTIONS
///   -s, --source      Source database: musicbrainz, discogs (default: musicbrainz)
///   -c, --credits     Include credits/personnel information
///
/// ENVIRONMENT VARIABLES
///   DISCOGS_TOKEN     Required for Discogs lookups
///
/// EXAMPLES
///   cli music lookup 12345 --source discogs
///   cli music lookup a1b2c3d4-e5f6-7890-abcd-ef1234567890 --source musicbrainz
///   cli music lookup 12345 --source discogs --credits
/// </summary>
[Command("music lookup", Description = "Get release details by ID")]
public sealed class MusicLookupCommand : ICommand
{
    [CommandParameter(0, Name = "id", Description = "Release ID")]
    public required string Id { get; init; }

    [CommandOption("source", 's', Description = "musicbrainz, discogs")]
    public string Source { get; init; } = "musicbrainz";

    [CommandOption("credits", 'c', Description = "Include credits/personnel")]
    public bool IncludeCredits { get; init; }

    public async ValueTask ExecuteAsync(IConsole console)
    {
        string? discogsToken = GetEnvironmentVariable("DISCOGS_TOKEN");
        MusicMetadataService service = new(discogsToken);

        Console.Info("Looking up {0} from {1}...", Id, Source);
        Console.NewLine();

        if (Source.Equals("musicbrainz", StringComparison.OrdinalIgnoreCase))
        {
            if (!Guid.TryParse(Id, out Guid mbId))
            {
                Console.Error("Invalid MusicBrainz ID (must be GUID)");
                return;
            }

            MusicBrainzRelease? release = await service.MusicBrainz.GetReleaseAsync(mbId);

            if (release is null)
            {
                Console.Warning("Release not found");
                return;
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

            if (IncludeCredits && release.Credits.Count > 0)
            {
                Console.NewLine();
                Console.Info("Credits ({0}):", release.Credits.Count);
                foreach (MusicBrainzCredit credit in release.Credits)
                {
                    Console.Dim($"  {credit.Name} - {credit.Role}");
                }
            }
        }
        else if (Source.Equals("discogs", StringComparison.OrdinalIgnoreCase))
        {
            if (!int.TryParse(Id, out int discogsId))
            {
                Console.Error("Invalid Discogs ID (must be number)");
                return;
            }

            if (service.Discogs is null)
            {
                Console.CriticalFailure("Discogs", "DISCOGS_TOKEN environment variable not set");
                return;
            }

            DiscogsRelease? release;
            try
            {
                release = await service.Discogs.GetReleaseAsync(discogsId);
            }
            catch (Exception ex)
            {
                Console.CriticalFailure("Discogs", ex.Message);
                return;
            }

            if (release is null)
            {
                Console.Warning("Release not found");
                return;
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

            if (IncludeCredits && release.Credits.Count > 0)
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
            Console.Error("Invalid source: {0}. Use: musicbrainz, discogs", Source);
        }
    }
}
