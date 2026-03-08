using System.Text.Encodings.Web;

namespace CSharpScripts.Core;

internal static class StateManager
{
	public const string LastFmSyncFile = "lastfm/sync.json";
	public const string LastFmScrobblesFile = "lastfm/scrobbles.json";
	public const string YoutubeSyncFile = "youtube/sync.json";

	internal static string RootDirectory = Paths.StateDirectory;

	private static readonly System.Threading.Lock StateLock = new();

	public static readonly JsonSerializerOptions JsonIndented = new()
	{
		WriteIndented = true,
		Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
	};

	public static readonly JsonSerializerOptions JsonCompact = new()
	{
		Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
	};

	public static async Task<T> LoadStateAsync<T>(string fileName, CancellationToken ct = default)
		where T : class, new()
	{
		ct.ThrowIfCancellationRequested();

		lock (StateLock)
		{
			Directory.CreateDirectory(RootDirectory);
		}

		var path = GetPath(fileName);

		if (File.Exists(path))
		{
			try
			{
				var json = await File.ReadAllTextAsync(path, ct);
				return JsonSerializer.Deserialize<T>(json, JsonCompact) ?? new T();
			}
			catch (JsonException ex)
			{
				Log.Warning("JSON corruption detected in {Path}: {Message}", path, ex.Message);
				var corruptedPath = path + ".corrupted";
				lock (StateLock)
				{
					File.Move(path, corruptedPath, overwrite: true);
				}
				Log.Debug("Backed up corrupted file to {Path}", corruptedPath);
				return new T();
			}
		}

		var legacyPath = GetLegacyPath(fileName);
		if (legacyPath is not null && File.Exists(legacyPath))
		{
			return await TryMigrateLegacyFileAsync<T>(fileName, legacyPath, ct);
		}

		return new T();
	}

	private static async Task<T> TryMigrateLegacyFileAsync<T>(
		string fileName,
		string legacyPath,
		CancellationToken ct
	)
		where T : class, new()
	{
		var newPath = GetPath(fileName);
		Log.Debug("Migrating state file: {0} → {1}", legacyPath, newPath);
		try
		{
			var json = await File.ReadAllTextAsync(legacyPath, ct);
			T? data = JsonSerializer.Deserialize<T>(json, JsonCompact) ?? new T();

			await SaveStateAsync(fileName, data, ct);

			lock (StateLock)
			{
				File.Delete(legacyPath);
			}

			return data;
		}
		catch (JsonException ex)
		{
			Log.Warning("JSON corruption in legacy file {0}: {1}", legacyPath, ex.Message);
			var corruptedPath = legacyPath + ".corrupted";
			lock (StateLock)
			{
				File.Move(legacyPath, corruptedPath, overwrite: true);
			}
			return new T();
		}
	}

	public static T Load<T>(string fileName)
		where T : class, new() => LoadStateAsync<T>(fileName).GetAwaiter().GetResult();

	private static string? GetLegacyPath(string fileName)
	{
		Dictionary<string, string> migrations = new()
		{
			["lastfm/fetch-state.json"] = "lastfm/sync.json",
			["youtube/fetch-state.json"] = "youtube/sync.json",
			["lastfm/scrobbles-cache.json"] = "lastfm/scrobbles.json",
		};

		return migrations.TryGetValue(fileName, out var legacy) ? GetPath(legacy) : null;
	}

	public static async Task SaveStateAsync<T>(
		string fileName,
		T state,
		CancellationToken ct = default
	)
	{
		ct.ThrowIfCancellationRequested();

		lock (StateLock)
		{
			var path = GetPath(fileName);
			var tempPath = path + $".{Guid.NewGuid()}.tmp";

			var json = JsonSerializer.Serialize(state, JsonIndented);
#pragma warning disable CA1849
			File.WriteAllText(tempPath, json);
			File.Move(tempPath, path, overwrite: true);
#pragma warning restore CA1849
		}

		await Task.CompletedTask;
	}

	public static void Save<T>(string fileName, T state) =>
		SaveStateAsync(fileName, state).GetAwaiter().GetResult();

	public static void Delete(string fileName)
	{
		var path = GetPath(fileName);
		if (File.Exists(path))
			File.Delete(path);
	}

	public static void DeleteAllStates()
	{
		if (Directory.Exists(RootDirectory))
		{
			Directory.Delete(RootDirectory, recursive: true);
			Log.Debug("Deleted all state files");
		}
	}

	public static void DeleteLastFmStates()
	{
		Delete(LastFmSyncFile);
		Delete(LastFmScrobblesFile);
		Log.Debug("Deleted Last.fm state files");
	}

	private static string GetPath(string fileName)
	{
		var fullPath = Path.Combine(
			RootDirectory,
			fileName.EndsWith(".json") ? fileName : $"{fileName}.json"
		);
		var directory = Path.GetDirectoryName(fullPath);
		if (!IsNullOrEmpty(directory))
			Directory.CreateDirectory(directory);
		return fullPath;
	}

	private const string YoutubePlaylistsSubdirectory = "youtube/playlists";
	private const string YoutubeDeletedSubdirectory = "youtube/deleted";

	private static string YouTubePlaylistsDirectory =>
		Path.Combine(RootDirectory, YoutubePlaylistsSubdirectory);

	private static string YouTubeDeletedDirectory =>
		Path.Combine(RootDirectory, YoutubeDeletedSubdirectory);

	internal static List<YouTubeVideo> LoadPlaylistCache(string playlistTitle)
	{
		Directory.CreateDirectory(YouTubePlaylistsDirectory);
		var path = GetPlaylistPath(playlistTitle);
		if (!File.Exists(path))
			return [];

		var json = File.ReadAllText(path);
		return JsonSerializer.Deserialize<List<YouTubeVideo>>(json, JsonCompact) ?? [];
	}

	public static void SavePlaylistCache(string playlistTitle, List<YouTubeVideo> videos)
	{
		Directory.CreateDirectory(YouTubePlaylistsDirectory);
		File.WriteAllText(
			GetPlaylistPath(playlistTitle),
			JsonSerializer.Serialize(videos, JsonIndented)
		);
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
		Directory.CreateDirectory(YouTubeDeletedDirectory);
		var sourcePath = GetPlaylistPath(playlistTitle);
		var destPath = Path.Combine(
			YouTubeDeletedDirectory,
			$"{SanitizeFileName(playlistTitle)}.json"
		);

		if (File.Exists(sourcePath))
			File.Move(sourcePath, destPath);

		return destPath;
	}

	public static void DeleteAllYouTubeStates()
	{
		Delete(YoutubeSyncFile);

		if (Directory.Exists(YouTubePlaylistsDirectory))
			Directory.Delete(YouTubePlaylistsDirectory, recursive: true);

		Log.Debug("Deleted YouTube state files");
	}

	public static void MigratePlaylistFiles(Dictionary<string, PlaylistSnapshot> snapshots)
	{
		List<string> oldFiles = [.. Directory.GetFiles(Paths.StateDirectory, "playlist_*.json")];

		var oldPlaylistsDir = Path.Combine(Paths.StateDirectory, "playlists");
		if (Directory.Exists(oldPlaylistsDir))
			oldFiles.AddRange(Directory.GetFiles(oldPlaylistsDir, "*.json"));

		if (oldFiles.Count == 0)
			return;

		Directory.CreateDirectory(YouTubePlaylistsDirectory);
		var migrated = 0;

		foreach (var oldFile in oldFiles)
		{
			var fileName = Path.GetFileName(oldFile);
			var playlistId = fileName.Replace("playlist_", "").Replace(".json", "");

			if (!snapshots.TryGetValue(playlistId, out PlaylistSnapshot? snapshot))
			{
				File.Delete(oldFile);
				Log.Debug("Deleted orphan playlist cache: {0}", fileName);
				continue;
			}

			var newPath = GetPlaylistPath(snapshot.Title);

			if (!File.Exists(newPath))
			{
				File.Move(oldFile, newPath);
				migrated++;
				Log.Debug("Migrated: {0} → {1}", fileName, Path.GetFileName(newPath));
			}
			else
			{
				File.Delete(oldFile);
			}
		}

		if (
			Directory.Exists(oldPlaylistsDir)
			&& Directory.GetFiles(oldPlaylistsDir, "*").Length == 0
		)
			Directory.Delete(oldPlaylistsDir, recursive: true);

		if (migrated > 0)
			Log.Information("Migrated {0} playlist cache files to new format", migrated);
	}

	private static string GetPlaylistPath(string playlistTitle) =>
		Path.Combine(YouTubePlaylistsDirectory, $"{SanitizeFileName(playlistTitle)}.json");

	private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();

	private static string SanitizeFileName(string name)
	{
		if (IsNullOrWhiteSpace(name))
			return "unnamed";

		foreach (var c in InvalidFileNameChars)
			name = name.Replace(c, '_');

		return name.Trim().TrimEnd('.');
	}

	private static string ReleaseCachePath => Path.Combine(Paths.StateDirectory, "releases");

	public static T? LoadReleaseCache<T>(string releaseId)
		where T : class
	{
		Directory.CreateDirectory(ReleaseCachePath);
		var path = GetReleasePath(releaseId);
		if (!File.Exists(path))
			return null;

		var json = File.ReadAllText(path);
		return JsonSerializer.Deserialize<T>(json, JsonCompact);
	}

	public static void SaveReleaseCache<T>(string releaseId, T data)
	{
		Directory.CreateDirectory(ReleaseCachePath);
		File.WriteAllText(GetReleasePath(releaseId), JsonSerializer.Serialize(data, JsonIndented));
		Log.Debug("Saved release cache: {0}", releaseId);
	}

	public static bool ReleaseCacheExists(string releaseId) =>
		File.Exists(GetReleasePath(releaseId));

	public static DateTime? GetReleaseCacheAge(string releaseId)
	{
		var path = GetReleasePath(releaseId);
		return File.Exists(path) ? File.GetLastWriteTimeUtc(path) : null;
	}

	public static void DeleteReleaseCache(string releaseId)
	{
		var path = GetReleasePath(releaseId);
		if (File.Exists(path))
		{
			File.Delete(path);
			Log.Debug("Deleted release cache: {0}", releaseId);
		}
	}

	public static void DeleteAllReleaseCaches()
	{
		if (Directory.Exists(ReleaseCachePath))
		{
			Directory.Delete(ReleaseCachePath, recursive: true);
			Log.Debug("Deleted all release caches");
		}
	}

	public static IEnumerable<string> ListReleaseCaches()
	{
		if (!Directory.Exists(ReleaseCachePath))
			yield break;

		foreach (var file in Directory.GetFiles(ReleaseCachePath, "*.json"))
			yield return Path.GetFileNameWithoutExtension(file);
	}

	private static string GetReleasePath(string releaseId) =>
		Path.Combine(ReleaseCachePath, $"{SanitizeFileName(releaseId)}.json");
}
