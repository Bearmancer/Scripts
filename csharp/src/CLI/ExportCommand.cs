using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;

namespace CSharpScripts.CLI;

[Command("export yt", Description = "Export YouTube playlists as CSV files")]
public sealed class ExportYouTubeCommand : ICommand
{
    public ValueTask ExecuteAsync(IConsole console)
    {
        YouTubePlaylistOrchestrator.ExportSheetsAsCSVs(ct: Program.Cts.Token);
        return ValueTask.CompletedTask;
    }
}
