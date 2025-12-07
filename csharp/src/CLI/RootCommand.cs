using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;

namespace CSharpScripts.CLI;

/// <summary>
/// NAME
///   cli - Lance's Utilities
///
/// DESCRIPTION
///   CLI toolkit for automation and data sync. Provides commands for syncing
///   YouTube playlists and Last.fm scrobbles to Google Sheets, music metadata
///   search via Discogs and MusicBrainz, and temporary email management.
///
/// USAGE
///   cli [command] [options]
///
/// COMMANDS
///   sync yt         Sync YouTube playlists to Google Sheets
///   sync lastfm     Sync Last.fm scrobbles to Google Sheets
///   export yt       Export YouTube playlists as CSV files
///   status          Show sync state and cache info
///   clean local     Delete local state and cache files
///   clean purge     Delete all state, remote data, and builds
///   music search    Search Discogs and MusicBrainz
///   music lookup    Get release details by ID
///   mail create     Create temporary email
///   mail check      Check inbox for messages
///   mail delete     Delete temporary email
///
/// EXAMPLES
///   cli sync yt --force
///   cli sync lastfm --since 2024/01/01
///   cli status yt
///   cli clean purge yt
///   cli music search --artist "Radiohead"
///   cli mail create
/// </summary>
[Command(Description = "CLI toolkit for automation and data sync")]
public sealed class RootCommand : ICommand
{
    public ValueTask ExecuteAsync(IConsole console)
    {
        Console.Rule("Lance's Utilities");
        Console.NewLine();

        Console.MarkupLine("[blue bold]COMMAND GROUPS[/]");
        Console.MarkupLine("  [cyan]sync[/]       Sync data to Google Sheets");
        Console.MarkupLine("  [cyan]export[/]     Export data as CSV files");
        Console.MarkupLine("  [cyan]status[/]     Show sync state and cache info");
        Console.MarkupLine("  [cyan]clean[/]      Delete state, cache, and remote data");
        Console.MarkupLine("  [cyan]music[/]      Search Discogs and MusicBrainz");
        Console.MarkupLine("  [cyan]mail[/]       Temporary email management");
        Console.NewLine();

        Console.MarkupLine("[blue bold]USAGE[/]");
        Console.MarkupLine("  [dim]$[/] cli [grey]<command>[/] [grey][[options]][/]");
        Console.NewLine();

        Console.MarkupLine("[blue bold]QUICK START[/]");
        Console.MarkupLine(
            "  [dim]$[/] cli sync yt [grey]--force[/]          [dim]# Sync YouTube playlists[/]"
        );
        Console.MarkupLine(
            "  [dim]$[/] cli sync lastfm                [dim]# Sync Last.fm scrobbles[/]"
        );
        Console.MarkupLine(
            "  [dim]$[/] cli status                     [dim]# Check sync status[/]"
        );
        Console.MarkupLine("  [dim]$[/] cli music search [grey]--artist \"Radiohead\"[/]");
        Console.NewLine();

        Console.MarkupLine("[dim]Run 'cli <command> --help' for more information on a command.[/]");

        return ValueTask.CompletedTask;
    }
}
