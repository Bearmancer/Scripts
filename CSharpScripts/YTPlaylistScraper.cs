using System.Globalization;
using System.Text.Json;
using CsvHelper;
using CsvHelper.Configuration;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;

namespace CSharpScripts;

internal class Program
{
    const string OUTPUT_DIR_NAME = "YT_Playlists_Scraped";
    const string MANIFEST_SUBDIR = ".manifest";
    const string CHANGES_SUBDIR = "changes";

    static readonly string OutputDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
        OUTPUT_DIR_NAME
    );

    static readonly string ManifestDir = Path.Combine(OutputDir, MANIFEST_SUBDIR);
    static readonly string ChangesDir = Path.Combine(OutputDir, CHANGES_SUBDIR);

    static async Task Main()
    {
        Utilities.Info("YouTube Playlist Exporter started");

        var yt = await GoogleAuth.CreateYouTubeServiceAsync(appName: "YouTubePlaylistExporter");
        Utilities.Success("Authentication successful");

        await RunInitialImportAsync();

        var changes = await SyncAllAsync(yt);
        Utilities.Info($"Changes detected: {changes.Count}");

        if (changes.Count == 0)
            return;

        // var spreadsheetId = Environment.GetEnvironmentVariable("YOUTUBE_CHANGES_SHEET_ID");
        // if (string.IsNullOrWhiteSpace(spreadsheetId))
        //     return;

        var sheets = await GoogleAuth.CreateSheetsServiceAsync(appName: "YouTubePlaylistExporter");
        var rows = changes
            .Select(c =>
                (IList<object>)
                    [
                        c.When,
                        c.PlaylistId,
                        c.PlaylistTitle,
                        c.Type,
                        c.VideoId,
                        c.Title,
                        c.ChannelTitle,
                    ]
            )
            .ToList();

        await GoogleSheets.AppendAsync(
            service: sheets,
            spreadsheetId: spreadsheetId,
            rangeA1: "Changes!A:G",
            rows: rows
        );

        Utilities.Success($"Committed {rows.Count} change rows to Sheets");
    }

    static async Task RunInitialImportAsync()
    {
        var root = Environment.GetEnvironmentVariable("CSV_IMPORT_DIR") ?? OutputDir;
        var spreadsheetId =
            Environment.GetEnvironmentVariable("YOUTUBE_CHANGES_SHEET_ID")
            ?? throw new InvalidOperationException(
                "YOUTUBE_CHANGES_SHEET_ID environment variable not set"
            );

        Utilities.Info($"Scanning CSVs in {root}");

        var csvFiles = Directory
            .EnumerateFiles(
                root,
                searchPattern: "*.csv",
                searchOption: SearchOption.TopDirectoryOnly
            )
            .Where(p =>
                !string.Equals(
                    Path.GetFileName(Path.GetDirectoryName(p)),
                    "changes",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            .ToList();

        if (csvFiles.Count == 0)
        {
            Utilities.Warning("No CSV files found for import");
            return;
        }

        var sheets = await GoogleAuth.CreateSheetsServiceAsync(appName: "YouTubePlaylistExporter");

        foreach (var file in csvFiles)
        {
            var sheetName = Path.GetFileNameWithoutExtension(file);
            Utilities.Info($"Importing {sheetName}");

            var rows = ReadCsvAsRows(path: file);
            await GoogleSheets.EnsureSheetExistsAsync(
                service: sheets,
                spreadsheetId: spreadsheetId,
                sheetName: sheetName
            );
            await GoogleSheets.AppendAsync(
                service: sheets,
                spreadsheetId: spreadsheetId,
                rangeA1: $"{sheetName}!A:Z",
                rows: rows
            );

            Utilities.Success($"Imported {rows.Count} rows to {sheetName}");
        }
    }

    static List<IList<object>> ReadCsvAsRows(string path)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true };
        using var reader = new StreamReader(path);
        using var csv = new CsvReader(reader, config);

        var rows = new List<IList<object>>();
        csv.Read();
        csv.ReadHeader();

        var headers = csv.HeaderRecord?.ToList() ?? [];
        rows.Add(headers.Cast<object>().ToList());

        while (csv.Read())
        {
            var record = headers.Select(h => (object)(csv.GetField(h) ?? string.Empty)).ToList();
            rows.Add(record);
        }

        return rows;
    }

    static async Task<List<Change>> SyncAllAsync(YouTubeService yt)
    {
        Directory.CreateDirectory(ManifestDir);
        Directory.CreateDirectory(ChangesDir);

        var playlists = await GetMyPlaylistsAsync(service: yt);
        var allChanges = new List<Change>();

        foreach (var playlist in playlists)
        {
            var playlistId = playlist.Id ?? string.Empty;
            var playlistTitle = playlist.Snippet?.Title ?? "Unknown";
            var items = await FetchMinimalItemsAsync(service: yt, playlistId: playlistId);

            var isAlphabetical = IsAlphabeticalByTitle(items);
            var manifest =
                await LoadManifestAsync(playlistId: playlistId)
                ?? new PlaylistManifest(
                    PlaylistId: playlistId,
                    Title: playlistTitle,
                    ItemCount: 0,
                    IsAlphabetical: isAlphabetical,
                    Items: []
                );

            var (added, removed) = Diff(oldItems: manifest.Items, newItems: items);

            if (added.Count == 0 && removed.Count == 0)
                continue;

            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

            allChanges.AddRange(
                added.Select(item => new Change(
                    PlaylistId: playlistId,
                    PlaylistTitle: playlistTitle,
                    Type: "Added",
                    VideoId: item.VideoId,
                    Title: item.Title,
                    ChannelTitle: item.ChannelTitle,
                    When: timestamp
                ))
            );

            allChanges.AddRange(
                removed.Select(item => new Change(
                    PlaylistId: playlistId,
                    PlaylistTitle: playlistTitle,
                    Type: "Removed",
                    VideoId: item.VideoId,
                    Title: item.Title,
                    ChannelTitle: item.ChannelTitle,
                    When: timestamp
                ))
            );

            var merged = MergeItems(current: manifest.Items, added: added, removed: removed);
            if (isAlphabetical)
                merged = [.. merged.OrderBy(i => i.Title, StringComparer.OrdinalIgnoreCase)];

            await SaveManifestAsync(
                manifest: new PlaylistManifest(
                    PlaylistId: playlistId,
                    Title: playlistTitle,
                    ItemCount: items.Count,
                    IsAlphabetical: isAlphabetical,
                    Items: merged
                )
            );

            await ExportPlaylistCsvAsync(title: playlistTitle, items: merged);
        }

        if (allChanges.Count > 0)
        {
            var changeCsvPath = await ExportChangesCsvAsync(changes: allChanges);
            Utilities.Info($"Changes exported: {Path.GetFileName(changeCsvPath)}");
        }

        return allChanges;
    }

    static async Task<List<Playlist>> GetMyPlaylistsAsync(YouTubeService service)
    {
        var request = service.Playlists.List(part: "snippet");
        request.Mine = true;
        request.MaxResults = 50;

        var result = new List<Playlist>();
        string? pageToken = null;

        do
        {
            request.PageToken = pageToken;
            var response = await request.ExecuteAsync();
            result.AddRange(response.Items ?? []);
            pageToken = response.NextPageToken;
        } while (!string.IsNullOrEmpty(pageToken));

        return result;
    }

    static async Task<List<MinimalItem>> FetchMinimalItemsAsync(
        YouTubeService service,
        string playlistId
    )
    {
        var request = service.PlaylistItems.List(part: "snippet,contentDetails");
        request.PlaylistId = playlistId;
        request.MaxResults = 50;

        var items = new List<MinimalItem>();
        string? pageToken = null;

        do
        {
            request.PageToken = pageToken;
            var response = await request.ExecuteAsync();

            foreach (var item in response.Items ?? [])
            {
                var videoId =
                    item.ContentDetails?.VideoId
                    ?? item.Snippet?.ResourceId?.VideoId
                    ?? string.Empty;
                if (string.IsNullOrEmpty(videoId))
                    continue;

                items.Add(
                    new MinimalItem(
                        VideoId: videoId,
                        Title: item.Snippet?.Title?.Trim() ?? string.Empty,
                        ChannelId: item.Snippet?.ChannelId ?? string.Empty,
                        ChannelTitle: item.Snippet?.ChannelTitle ?? string.Empty
                    )
                );
            }

            pageToken = response.NextPageToken;
        } while (!string.IsNullOrEmpty(pageToken));

        return items;
    }

    static bool IsAlphabeticalByTitle(List<MinimalItem> items)
    {
        for (var i = 1; i < items.Count; i++)
        {
            if (
                string.Compare(
                    items[i - 1].Title,
                    items[i].Title,
                    StringComparison.OrdinalIgnoreCase
                ) > 0
            )
                return false;
        }
        return true;
    }

    static (List<MinimalItem> added, List<MinimalItem> removed) Diff(
        List<MinimalItem> oldItems,
        List<MinimalItem> newItems
    )
    {
        var oldSet = oldItems.Select(i => i.VideoId).ToHashSet();
        var newSet = newItems.Select(i => i.VideoId).ToHashSet();
        var added = newItems.Where(i => !oldSet.Contains(i.VideoId)).ToList();
        var removed = oldItems.Where(i => !newSet.Contains(i.VideoId)).ToList();
        return (added, removed);
    }

    static List<MinimalItem> MergeItems(
        List<MinimalItem> current,
        List<MinimalItem> added,
        List<MinimalItem> removed
    )
    {
        var map = current.ToDictionary(i => i.VideoId, i => i);

        foreach (var item in removed)
            map.Remove(item.VideoId);

        foreach (var item in added)
            map[item.VideoId] = item;

        return [.. map.Values];
    }

    static async Task<PlaylistManifest?> LoadManifestAsync(string playlistId)
    {
        var path = Path.Combine(ManifestDir, playlistId + ".json");
        if (!File.Exists(path))
            return null;

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<PlaylistManifest>(stream);
    }

    static async Task SaveManifestAsync(PlaylistManifest manifest)
    {
        var path = Path.Combine(ManifestDir, manifest.PlaylistId + ".json");
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(
            utf8Json: stream,
            value: manifest,
            options: new JsonSerializerOptions { WriteIndented = true }
        );
    }

    static async Task<string> ExportPlaylistCsvAsync(string title, List<MinimalItem> items)
    {
        var safeTitle = MakeSafeTitle(title);
        Directory.CreateDirectory(OutputDir);

        var path = UniquePath(
            directory: OutputDir,
            filenameNoExtension: safeTitle,
            extension: ".csv"
        );
        await using var writer = new StreamWriter(path);
        await writer.WriteLineAsync("Title,Channel,VideoLink,ChannelLink");

        foreach (var item in items)
        {
            var line = string.Join(
                ',',
                EscapeCsv(item.Title),
                EscapeCsv(item.ChannelTitle),
                EscapeCsv($"https://www.youtube.com/watch?v={item.VideoId}"),
                EscapeCsv($"https://www.youtube.com/channel/{item.ChannelId}")
            );
            await writer.WriteLineAsync(line);
        }

        return path;
    }

    static async Task<string> ExportChangesCsvAsync(List<Change> changes)
    {
        var filename = $"changes_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
        var path = Path.Combine(ChangesDir, filename);
        await using var writer = new StreamWriter(path);
        await writer.WriteLineAsync(
            "When,PlaylistId,PlaylistTitle,Type,VideoId,Title,ChannelTitle"
        );

        foreach (var change in changes)
        {
            await writer.WriteLineAsync(
                string.Join(
                    ',',
                    EscapeCsv(change.When),
                    EscapeCsv(change.PlaylistId),
                    EscapeCsv(change.PlaylistTitle),
                    EscapeCsv(change.Type),
                    EscapeCsv(change.VideoId),
                    EscapeCsv(change.Title),
                    EscapeCsv(change.ChannelTitle)
                )
            );
        }

        return path;
    }

    static string EscapeCsv(string value)
    {
        if (
            value.Contains('"')
            || value.Contains(',')
            || value.Contains('\n')
            || value.Contains('\r')
        )
            return '"' + value.Replace("\"", "\"\"") + '"';
        return value;
    }

    static string MakeSafeTitle(string title)
    {
        var baseName = string.Concat(
                (title ?? "Playlist").Where(c => !Path.GetInvalidFileNameChars().Contains(c))
            )
            .Trim();

        if (string.IsNullOrWhiteSpace(baseName))
            baseName = "Playlist";

        HashSet<string> reserved =
        [
            "CON",
            "PRN",
            "AUX",
            "NUL",
            "COM1",
            "COM2",
            "COM3",
            "COM4",
            "COM5",
            "COM6",
            "COM7",
            "COM8",
            "COM9",
            "LPT1",
            "LPT2",
            "LPT3",
            "LPT4",
            "LPT5",
            "LPT6",
            "LPT7",
            "LPT8",
            "LPT9",
        ];

        if (reserved.Contains(baseName))
            baseName = "_" + baseName;

        return baseName;
    }

    static string UniquePath(string directory, string filenameNoExtension, string extension)
    {
        var firstPath = Path.Combine(directory, filenameNoExtension + extension);
        if (!File.Exists(firstPath))
            return firstPath;

        var counter = 2;
        while (true)
        {
            var candidate = Path.Combine(
                directory,
                $"{filenameNoExtension} ({counter}){extension}"
            );
            if (!File.Exists(candidate))
                return candidate;
            counter++;
        }
    }

    sealed record MinimalItem(string VideoId, string Title, string ChannelId, string ChannelTitle);

    sealed record PlaylistManifest(
        string PlaylistId,
        string Title,
        int ItemCount,
        bool IsAlphabetical,
        List<MinimalItem> Items
    );

    sealed record Change(
        string PlaylistId,
        string PlaylistTitle,
        string Type,
        string VideoId,
        string Title,
        string ChannelTitle,
        string When
    );
}
