namespace CSharpScripts.Infrastructure;

internal static class StateManager
{
    internal const string LastFmSyncFile = "lastfm/sync.json";
    internal const string LastFmScrobblesFile = "lastfm/scrobbles.json";
    internal const string YouTubeSyncFile = "youtube/sync.json";

    const string YouTubePlaylistsSubdirectory = "youtube/playlists";
    const string YouTubeDeletedSubdirectory = "youtube/deleted";

    internal static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    static string YouTubePlaylistsDirectory =>
        Combine(Paths.StateDirectory, YouTubePlaylistsSubdirectory);
    static string YouTubeDeletedDirectory =>
        Combine(Paths.StateDirectory, YouTubeDeletedSubdirectory);

    internal static T Load<T>(string fileName)
        where T : class, new()
    {
        CreateDirectory(Paths.StateDirectory);
        var path = GetPath(fileName);
        if (!File.Exists(path))
            return new T();

        var json = ReadAllText(path);
        return JsonSerializer.Deserialize<T>(json, JsonOptions) ?? new T();
    }

    internal static void Save<T>(string fileName, T state)
    {
        CreateDirectory(Paths.StateDirectory);
        WriteAllText(GetPath(fileName), JsonSerializer.Serialize(state, JsonOptions));
    }

    internal static void Delete(string fileName)
    {
        var path = GetPath(fileName);
        if (File.Exists(path))
            File.Delete(path);
    }

    internal static List<YouTubeVideo> LoadPlaylistCache(string playlistTitle)
    {
        CreateDirectory(YouTubePlaylistsDirectory);
        var path = GetPlaylistPath(playlistTitle);
        if (!File.Exists(path))
            return [];

        var json = ReadAllText(path);
        return JsonSerializer.Deserialize<List<YouTubeVideo>>(json, JsonOptions) ?? [];
    }

    internal static void SavePlaylistCache(string playlistTitle, List<YouTubeVideo> videos)
    {
        CreateDirectory(YouTubePlaylistsDirectory);
        WriteAllText(GetPlaylistPath(playlistTitle), JsonSerializer.Serialize(videos, JsonOptions));
    }

    internal static void DeletePlaylistCache(string playlistTitle)
    {
        var path = GetPlaylistPath(playlistTitle);
        if (File.Exists(path))
            File.Delete(path);
    }

    internal static void RenamePlaylistCache(string oldTitle, string newTitle)
    {
        var oldPath = GetPlaylistPath(oldTitle);
        var newPath = GetPlaylistPath(newTitle);

        if (File.Exists(oldPath) && !File.Exists(newPath))
            File.Move(oldPath, newPath);
    }

    internal static void DeleteLastFmStates()
    {
        Delete(LastFmSyncFile);
        Delete(LastFmScrobblesFile);
        Logger.Debug("Deleted Last.fm state files");
    }

    internal static void DeleteYouTubeStates()
    {
        Delete(YouTubeSyncFile);

        if (Directory.Exists(YouTubePlaylistsDirectory))
            Directory.Delete(YouTubePlaylistsDirectory, recursive: true);

        Logger.Debug("Deleted YouTube state files");
    }

    internal static void MigratePlaylistFiles(Dictionary<string, PlaylistSnapshot> snapshots)
    {
        var oldFiles = GetFiles(Paths.StateDirectory, "playlist_*.json").ToList();

        var oldPlaylistsDir = Combine(Paths.StateDirectory, "playlists");
        if (Directory.Exists(oldPlaylistsDir))
            oldFiles.AddRange(GetFiles(oldPlaylistsDir, "*.json"));

        if (oldFiles.Count == 0)
            return;

        CreateDirectory(YouTubePlaylistsDirectory);
        var migrated = 0;

        foreach (var oldFile in oldFiles)
        {
            var fileName = GetFileName(oldFile);
            var playlistId = fileName.Replace("playlist_", "").Replace(".json", "");

            if (!snapshots.TryGetValue(playlistId, out var snapshot))
            {
                File.Delete(oldFile);
                Logger.Debug("Deleted orphan playlist cache: {0}", fileName);
                continue;
            }

            var newPath = GetPlaylistPath(snapshot.Title);

            if (!File.Exists(newPath))
            {
                File.Move(oldFile, newPath);
                migrated++;
                Logger.Debug("Migrated: {0} → {1}", fileName, GetFileName(newPath));
            }
            else
            {
                File.Delete(oldFile);
            }
        }

        if (Directory.Exists(oldPlaylistsDir) && !GetFiles(oldPlaylistsDir, "*").Any())
            Directory.Delete(oldPlaylistsDir, recursive: true);

        if (migrated > 0)
            Logger.Info("Migrated {0} playlist cache files to new format", migrated);
    }

    internal static bool PlaylistCacheExists(string playlistTitle) =>
        File.Exists(GetPlaylistPath(playlistTitle));

    internal static string ArchivePlaylistCache(string playlistTitle)
    {
        CreateDirectory(YouTubeDeletedDirectory);
        var sourcePath = GetPlaylistPath(playlistTitle);
        var destPath = Combine(YouTubeDeletedDirectory, $"{SanitizeFileName(playlistTitle)}.json");

        if (File.Exists(sourcePath))
            File.Move(sourcePath, destPath);

        return destPath;
    }

    static string GetPath(string fileName)
    {
        var fullPath = Combine(
            Paths.StateDirectory,
            fileName.EndsWith(".json") ? fileName : $"{fileName}.json"
        );
        var directory = GetDirectoryName(fullPath);
        if (!IsNullOrEmpty(directory))
            CreateDirectory(directory);
        return fullPath;
    }

    static string GetPlaylistPath(string playlistTitle) =>
        Combine(YouTubePlaylistsDirectory, $"{SanitizeFileName(playlistTitle)}.json");

    static string SanitizeFileName(string name) =>
        name.Replace(":", " -")
            .Replace("/", "-")
            .Replace("\\", "-")
            .Replace("?", "")
            .Replace("*", "")
            .Replace("<", "")
            .Replace(">", "")
            .Replace("|", "-")
            .Replace("\"", "'");
}
