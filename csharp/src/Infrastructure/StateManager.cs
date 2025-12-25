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
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static readonly JsonSerializerOptions JsonCompact = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static T Load<T>(string fileName)
        where T : class, new()
    {
        CreateDirectory(path: RootDirectory);
        string path = GetPath(fileName: fileName);
        if (!File.Exists(path: path))
            return new T();

        string json = ReadAllText(path: path);
        return JsonSerializer.Deserialize<T>(json: json, options: JsonCompact) ?? new T();
    }

    public static void Save<T>(string fileName, T state)
    {
        CreateDirectory(path: RootDirectory);
        WriteAllText(
            GetPath(fileName: fileName),
            JsonSerializer.Serialize(value: state, options: JsonCompact)
        );
    }

    public static void Delete(string fileName)
    {
        string path = GetPath(fileName: fileName);
        if (File.Exists(path: path))
            File.Delete(path: path);
    }

    public static void DeleteLastFmStates()
    {
        Delete(fileName: LastFmSyncFile);
        Delete(fileName: LastFmScrobblesFile);
        Console.Debug(message: "Deleted Last.fm state files");
    }

    private static string GetPath(string fileName)
    {
        string fullPath = Combine(
            path1: RootDirectory,
            fileName.EndsWith(value: ".json", comparisonType: StringComparison.Ordinal)
                ? fileName
                : $"{fileName}.json"
        );
        string? directory = GetDirectoryName(path: fullPath);
        if (!IsNullOrEmpty(value: directory))
            CreateDirectory(path: directory);
        return fullPath;
    }

    private const string YouTubePlaylistsSubdirectory = "youtube/playlists";
    private const string YouTubeDeletedSubdirectory = "youtube/deleted";

    private static string YouTubePlaylistsDirectory =>
        Combine(path1: RootDirectory, path2: YouTubePlaylistsSubdirectory);

    private static string YouTubeDeletedDirectory =>
        Combine(path1: RootDirectory, path2: YouTubeDeletedSubdirectory);

    internal static List<YouTubeVideo> LoadPlaylistCache(string playlistTitle)
    {
        CreateDirectory(path: YouTubePlaylistsDirectory);
        string path = GetPlaylistPath(playlistTitle: playlistTitle);
        if (!File.Exists(path: path))
            return [];

        string json = ReadAllText(path: path);
        return JsonSerializer.Deserialize<List<YouTubeVideo>>(json: json, options: JsonCompact)
            ?? [];
    }

    public static void SavePlaylistCache(string playlistTitle, List<YouTubeVideo> videos)
    {
        CreateDirectory(path: YouTubePlaylistsDirectory);
        WriteAllText(
            GetPlaylistPath(playlistTitle: playlistTitle),
            JsonSerializer.Serialize(value: videos, options: JsonCompact)
        );
    }

    public static void DeletePlaylistCache(string playlistTitle)
    {
        string path = GetPlaylistPath(playlistTitle: playlistTitle);
        if (File.Exists(path: path))
            File.Delete(path: path);
    }

    public static void RenamePlaylistCache(string oldTitle, string newTitle)
    {
        string oldPath = GetPlaylistPath(playlistTitle: oldTitle);
        string newPath = GetPlaylistPath(playlistTitle: newTitle);

        if (File.Exists(path: oldPath) && !File.Exists(path: newPath))
            Move(sourceFileName: oldPath, destFileName: newPath);
    }

    public static bool PlaylistCacheExists(string playlistTitle) =>
        File.Exists(GetPlaylistPath(playlistTitle: playlistTitle));

    public static string ArchivePlaylistCache(string playlistTitle)
    {
        CreateDirectory(path: YouTubeDeletedDirectory);
        string sourcePath = GetPlaylistPath(playlistTitle: playlistTitle);
        string destPath = Combine(
            path1: YouTubeDeletedDirectory,
            $"{SanitizeFileName(name: playlistTitle)}.json"
        );

        if (File.Exists(path: sourcePath))
            Move(sourceFileName: sourcePath, destFileName: destPath);

        return destPath;
    }

    public static void DeleteAllYouTubeStates()
    {
        Delete(fileName: YouTubeSyncFile);

        if (Directory.Exists(path: YouTubePlaylistsDirectory))
            Directory.Delete(path: YouTubePlaylistsDirectory, recursive: true);

        Console.Debug(message: "Deleted YouTube state files");
    }

    public static void MigratePlaylistFiles(Dictionary<string, PlaylistSnapshot> snapshots)
    {
        var oldFiles = GetFiles(path: Paths.StateDirectory, searchPattern: "playlist_*.json")
            .ToList();

        string oldPlaylistsDir = Combine(path1: Paths.StateDirectory, path2: "playlists");
        if (Directory.Exists(path: oldPlaylistsDir))
            oldFiles.AddRange(GetFiles(path: oldPlaylistsDir, searchPattern: "*.json"));

        if (oldFiles.Count == 0)
            return;

        CreateDirectory(path: YouTubePlaylistsDirectory);
        var migrated = 0;

        foreach (string oldFile in oldFiles)
        {
            string fileName = GetFileName(path: oldFile);
            string playlistId = fileName
                .Replace(oldValue: "playlist_", newValue: "")
                .Replace(oldValue: ".json", newValue: "");

            if (!snapshots.TryGetValue(key: playlistId, out var snapshot))
            {
                File.Delete(path: oldFile);
                Console.Debug(message: "Deleted orphan playlist cache: {0}", fileName);
                continue;
            }

            string newPath = GetPlaylistPath(playlistTitle: snapshot.Title);

            if (!File.Exists(path: newPath))
            {
                Move(sourceFileName: oldFile, destFileName: newPath);
                migrated++;
                Console.Debug(message: "Migrated: {0} → {1}", fileName, GetFileName(path: newPath));
            }
            else
            {
                File.Delete(path: oldFile);
            }
        }

        if (
            Directory.Exists(path: oldPlaylistsDir)
            && GetFiles(path: oldPlaylistsDir, searchPattern: "*").Length == 0
        )
            Directory.Delete(path: oldPlaylistsDir, recursive: true);

        if (migrated > 0)
            Console.Info(message: "Migrated {0} playlist cache files to new format", migrated);
    }

    private static string GetPlaylistPath(string playlistTitle) =>
        Combine(path1: YouTubePlaylistsDirectory, $"{SanitizeFileName(name: playlistTitle)}.json");

    private static readonly char[] InvalidFileNameChars = GetInvalidFileNameChars();

    private static string SanitizeFileName(string name)
    {
        if (IsNullOrWhiteSpace(value: name))
            return "unnamed";

        foreach (char c in InvalidFileNameChars)
            name = name.Replace(oldChar: c, newChar: '_');

        return name.Trim().TrimEnd(trimChar: '.');
    }

    private static string ReleaseCachePath =>
        Combine(path1: Paths.StateDirectory, path2: "releases");

    public static T? LoadReleaseCache<T>(string releaseId)
        where T : class
    {
        CreateDirectory(path: ReleaseCachePath);
        string path = GetReleasePath(releaseId: releaseId);
        if (!File.Exists(path: path))
            return null;

        string json = ReadAllText(path: path);
        return JsonSerializer.Deserialize<T>(json: json, options: JsonCompact);
    }

    public static void SaveReleaseCache<T>(string releaseId, T data)
    {
        CreateDirectory(path: ReleaseCachePath);
        WriteAllText(
            GetReleasePath(releaseId: releaseId),
            JsonSerializer.Serialize(value: data, options: JsonIndented)
        );
        Console.Debug(message: "Saved release cache: {0}", releaseId);
    }

    public static bool ReleaseCacheExists(string releaseId) =>
        File.Exists(GetReleasePath(releaseId: releaseId));

    public static DateTime? GetReleaseCacheAge(string releaseId)
    {
        string path = GetReleasePath(releaseId: releaseId);
        return File.Exists(path: path) ? File.GetLastWriteTimeUtc(path: path) : null;
    }

    public static void DeleteReleaseCache(string releaseId)
    {
        string path = GetReleasePath(releaseId: releaseId);
        if (File.Exists(path: path))
        {
            File.Delete(path: path);
            Console.Debug(message: "Deleted release cache: {0}", releaseId);
        }
    }

    public static void DeleteAllReleaseCaches()
    {
        if (Directory.Exists(path: ReleaseCachePath))
        {
            Directory.Delete(path: ReleaseCachePath, recursive: true);
            Console.Debug(message: "Deleted all release caches");
        }
    }

    public static IEnumerable<string> ListReleaseCaches()
    {
        if (!Directory.Exists(path: ReleaseCachePath))
            yield break;

        foreach (string file in GetFiles(path: ReleaseCachePath, searchPattern: "*.json"))
            yield return GetFileNameWithoutExtension(path: file);
    }

    private static string GetReleasePath(string releaseId) =>
        Combine(path1: ReleaseCachePath, $"{SanitizeFileName(name: releaseId)}.json");
}
