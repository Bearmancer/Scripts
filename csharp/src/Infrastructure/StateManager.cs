namespace CSharpScripts.Infrastructure;

public static class StateManager
{
    public const string LastFmSyncFile = "lastfm/sync.json";
    public const string LastFmScrobblesFile = "lastfm/scrobbles.json";
    public const string YouTubeSyncFile = "youtube/sync.json";

    internal static string RootDirectory = Paths.StateDirectory;

    public static readonly JsonSerializerOptions JsonIndented = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static readonly JsonSerializerOptions JsonCompact = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static T Load<T>(string fileName)
        where T : class, new()
    {
        CreateDirectory(RootDirectory);
        var path = GetPath(fileName);
        if (!File.Exists(path))
            return new T();

        var json = ReadAllText(path);
        return JsonSerializer.Deserialize<T>(json, JsonCompact) ?? new T();
    }

    public static void Save<T>(string fileName, T state)
    {
        CreateDirectory(RootDirectory);
        WriteAllText(GetPath(fileName), JsonSerializer.Serialize(state, JsonCompact));
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
        Combine(RootDirectory, YouTubePlaylistsSubdirectory);

    private static string YouTubeDeletedDirectory =>
        Combine(RootDirectory, YouTubeDeletedSubdirectory);

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

        if (Directory.Exists(oldPlaylistsDir) && GetFiles(oldPlaylistsDir, "*").Length == 0)
            Directory.Delete(oldPlaylistsDir, true);

        if (migrated > 0)
            Console.Info("Migrated {0} playlist cache files to new format", migrated);
    }

    private static string GetPlaylistPath(string playlistTitle) =>
        Combine(YouTubePlaylistsDirectory, $"{SanitizeFileName(playlistTitle)}.json");

    static readonly char[] InvalidFileNameChars = GetInvalidFileNameChars();

    static string SanitizeFileName(string name)
    {
        if (IsNullOrWhiteSpace(name))
            return "unnamed";

        foreach (char c in InvalidFileNameChars)
            name = name.Replace(c, '_');

        return name.Trim().TrimEnd('.');
    }

    #endregion

    #region Release Cache Management

    private static string ReleaseCachePath => Combine(Paths.StateDirectory, "releases");

    public static T? LoadReleaseCache<T>(string releaseId)
        where T : class
    {
        CreateDirectory(ReleaseCachePath);
        string path = GetReleasePath(releaseId);
        if (!File.Exists(path))
            return null;

        string json = ReadAllText(path);
        return JsonSerializer.Deserialize<T>(json, JsonCompact);
    }

    public static void SaveReleaseCache<T>(string releaseId, T data)
    {
        CreateDirectory(ReleaseCachePath);
        WriteAllText(GetReleasePath(releaseId), JsonSerializer.Serialize(data, JsonIndented));
        Console.Debug("Saved release cache: {0}", releaseId);
    }

    public static bool ReleaseCacheExists(string releaseId) =>
        File.Exists(GetReleasePath(releaseId));

    public static DateTime? GetReleaseCacheAge(string releaseId)
    {
        string path = GetReleasePath(releaseId);
        return File.Exists(path) ? File.GetLastWriteTimeUtc(path) : null;
    }

    public static void DeleteReleaseCache(string releaseId)
    {
        string path = GetReleasePath(releaseId);
        if (File.Exists(path))
        {
            File.Delete(path);
            Console.Debug("Deleted release cache: {0}", releaseId);
        }
    }

    public static void DeleteAllReleaseCaches()
    {
        if (Directory.Exists(ReleaseCachePath))
        {
            Directory.Delete(ReleaseCachePath, true);
            Console.Debug("Deleted all release caches");
        }
    }

    public static IEnumerable<string> ListReleaseCaches()
    {
        if (!Directory.Exists(ReleaseCachePath))
            yield break;

        foreach (string file in GetFiles(ReleaseCachePath, "*.json"))
            yield return GetFileNameWithoutExtension(file);
    }

    private static string GetReleasePath(string releaseId) =>
        Combine(ReleaseCachePath, $"{SanitizeFileName(releaseId)}.json");

    #endregion

    private static string GetPath(string fileName)
    {
        var fullPath = Combine(
            RootDirectory,
            fileName.EndsWith(".json", StringComparison.Ordinal) ? fileName : $"{fileName}.json"
        );
        var directory = GetDirectoryName(fullPath);
        if (!IsNullOrEmpty(directory))
            CreateDirectory(directory);
        return fullPath;
    }
}
