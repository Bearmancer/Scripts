namespace CSharpScripts.Commands;

internal static class ExportHandler
{
    internal static void YouTubeCsv(CancellationToken ct) =>
        YouTubePlaylistOrchestrator.ExportSheetsAsCSVs(ct: ct);
}
