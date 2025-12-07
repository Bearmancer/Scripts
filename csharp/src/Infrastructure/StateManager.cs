namespace CSharpScripts.Infrastructure;

/// <summary>
/// Generic state persistence manager using JSON serialization.
/// Service-specific state logic should be in respective feature directories.
/// </summary>
public static class StateManager
{
    public const string LastFmSyncFile = "lastfm/sync.json";
    public const string LastFmScrobblesFile = "lastfm/scrobbles.json";
    public const string YouTubeSyncFile = "youtube/sync.json";

    /// <summary>Shared JSON options with relaxed escaping and indentation for state files.</summary>
    public static readonly JsonSerializerOptions JsonIndented = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>Shared JSON options with relaxed escaping, compact for JSONL logs.</summary>
    public static readonly JsonSerializerOptions JsonCompact = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static T Load<T>(string fileName)
        where T : class, new()
    {
        CreateDirectory(Paths.StateDirectory);
        var path = GetPath(fileName);
        if (!File.Exists(path))
            return new T();

        var json = ReadAllText(path);
        return JsonSerializer.Deserialize<T>(json, StateManager.JsonCompact) ?? new T();
    }

    public static void Save<T>(string fileName, T state)
    {
        CreateDirectory(Paths.StateDirectory);
        WriteAllText(GetPath(fileName), JsonSerializer.Serialize(state, StateManager.JsonCompact));
    }

    public static void Delete(string fileName)
    {
        var path = GetPath(fileName);
        if (File.Exists(path))
            File.Delete(path);
    }

    public static void DeleteLastFmStates()
    {
        Delete(LastFmSyncFile);
        Delete(LastFmScrobblesFile);
        Console.Debug("Deleted Last.fm state files");
    }

    #region YouTube State Management

    private const string YouTubePlaylistsSubdirectory = "youtube/playlists";
    private const string YouTubeDeletedSubdirectory = "youtube/deleted";

    private static string YouTubePlaylistsDirectory =>
        Combine(Paths.StateDirectory, YouTubePlaylistsSubdirectory);

    private static string YouTubeDeletedDirectory =>
        Combine(Paths.StateDirectory, YouTubeDeletedSubdirectory);

    internal static List<YouTubeVideo> LoadPlaylistCache(string playlistTitle)
    {
        CreateDirectory(YouTubePlaylistsDirectory);
        var path = GetPlaylistPath(playlistTitle);
        if (!File.Exists(path))
            return [];

        var json = ReadAllText(path);
        return JsonSerializer.Deserialize<List<YouTubeVideo>>(json, JsonCompact) ?? [];
    }

    public static void SavePlaylistCache(string playlistTitle, List<YouTubeVideo> videos)
    {
        CreateDirectory(YouTubePlaylistsDirectory);
        WriteAllText(GetPlaylistPath(playlistTitle), JsonSerializer.Serialize(videos, JsonCompact));
    }

    public static void DeletePlaylistCache(string playlistTitle)
    {
        var path = GetPlaylistPath(playlistTitle);
        if (File.Exists(path))
            File.Delete(path);
    }

    public static void RenamePlaylistCache(string oldTitle, string newTitle)
    {
        var oldPath = GetPlaylistPath(oldTitle);
        var newPath = GetPlaylistPath(newTitle);

        if (File.Exists(oldPath) && !File.Exists(newPath))
            File.Move(oldPath, newPath);
    }

    public static bool PlaylistCacheExists(string playlistTitle) =>
        File.Exists(GetPlaylistPath(playlistTitle));

    public static string ArchivePlaylistCache(string playlistTitle)
    {
        CreateDirectory(YouTubeDeletedDirectory);
        var sourcePath = GetPlaylistPath(playlistTitle);
        var destPath = Combine(YouTubeDeletedDirectory, $"{SanitizeFileName(playlistTitle)}.json");

        if (File.Exists(sourcePath))
            File.Move(sourcePath, destPath);

        return destPath;
    }

    public static void DeleteAllYouTubeStates()
    {
        Delete(YouTubeSyncFile);

        if (Directory.Exists(YouTubePlaylistsDirectory))
            Directory.Delete(YouTubePlaylistsDirectory, true);

        Console.Debug("Deleted YouTube state files");
    }

    public static void MigratePlaylistFiles(Dictionary<string, PlaylistSnapshot> snapshots)
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
                Console.Debug("Deleted orphan playlist cache: {0}", fileName);
                continue;
            }

            var newPath = GetPlaylistPath(snapshot.Title);

            if (!File.Exists(newPath))
            {
                File.Move(oldFile, newPath);
                migrated++;
                Console.Debug("Migrated: {0} → {1}", fileName, GetFileName(newPath));
            }
            else
            {
                File.Delete(oldFile);
            }
        }

        if (Directory.Exists(oldPlaylistsDir) && !GetFiles(oldPlaylistsDir, "*").Any())
            Directory.Delete(oldPlaylistsDir, true);

        if (migrated > 0)
            Console.Info("Migrated {0} playlist cache files to new format", migrated);
    }

    private static string GetPlaylistPath(string playlistTitle) =>
        Combine(YouTubePlaylistsDirectory, $"{SanitizeFileName(playlistTitle)}.json");

    private static string SanitizeFileName(string name) =>
        name.Replace(":", " -")
            .Replace("/", "-")
            .Replace("\\", "-")
            .Replace("?", "")
            .Replace("*", "")
            .Replace("<", "")
            .Replace(">", "")
            .Replace("|", "-")
            .Replace("\"", "'");

    #endregion

    private static string GetPath(string fileName)
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
}
