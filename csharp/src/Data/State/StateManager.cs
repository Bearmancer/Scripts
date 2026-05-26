using System.Text.Encodings.Web;

namespace CSharpScripts.Data.State;

internal static class StateManager
{
	public const string LastFmSyncFile = "lastfm/sync.json";
	public const string LastFmScrobblesFile = "lastfm/scrobbles.json";
	public const string YoutubeSyncFile = "youtube/sync.json";

	private const string YoutubePlaylistsSubdirectory = "youtube/playlists";
	private const string YoutubeDeletedSubdirectory = "youtube/deleted";

	internal static string RootDirectory = Paths.StateDirectory;

	private static readonly Lock StateLock = new();

	public static readonly JsonSerializerOptions JsonIndented = new()
	{
		WriteIndented = true,
		Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
	};

	public static readonly JsonSerializerOptions JsonCompact = new()
	{
		Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
	};

	private static readonly FrozenDictionary<string, string> LegacyMigrations = new Dictionary<
		string,
		string
	>
	{
		[key: "lastfm/fetch-state.json"] = "lastfm/sync.json",
		[key: "youtube/fetch-state.json"] = "youtube/sync.json",
		[key: "lastfm/scrobbles-cache.json"] = "lastfm/scrobbles.json",
	}.ToFrozenDictionary();

	private static string YouTubePlaylistsDirectory =>
		Path.Combine(path1: RootDirectory, path2: YoutubePlaylistsSubdirectory);

	private static string YouTubeDeletedDirectory =>
		Path.Combine(path1: RootDirectory, path2: YoutubeDeletedSubdirectory);

	private static string ReleaseCachePath =>
		Path.Combine(path1: Paths.StateDirectory, path2: "releases");

	public static async Task<T> LoadStateAsync<T>(string fileName, CancellationToken ct = default)
		where T : class, new()
	{
		ct.ThrowIfCancellationRequested();

		lock (StateLock)
			Directory.CreateDirectory(path: RootDirectory);

		var path = GetPath(fileName: fileName);

		if (File.Exists(path: path))
		{
			try
			{
				var json = await File.ReadAllTextAsync(path: path, cancellationToken: ct);
				return JsonSerializer.Deserialize<T>(json: json, options: JsonCompact) ?? new T();
			}
			catch (JsonException)
			{
				Log.Warning(messageTemplate: "JSON corruption detected in {Path}", path);
				var corruptedPath = path + ".corrupted";
				lock (StateLock)
					File.Move(sourceFileName: path, destFileName: corruptedPath, overwrite: true);
				Log.Debug(messageTemplate: "Backed up corrupted file to {Path}", corruptedPath);
				return new T();
			}
		}

		var legacyPath = GetLegacyPath(fileName: fileName);
		if (legacyPath is { } && File.Exists(path: legacyPath))
		{
			return await TryMigrateLegacyFileAsync<T>(
				fileName: fileName,
				legacyPath: legacyPath,
				ct: ct
			);
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
		var newPath = GetPath(fileName: fileName);
		Log.Debug(
			messageTemplate: "Migrating state file: {LegacyPath} → {NewPath}",
			legacyPath,
			newPath
		);
		try
		{
			var json = await File.ReadAllTextAsync(path: legacyPath, cancellationToken: ct);
			T? data = JsonSerializer.Deserialize<T>(json: json, options: JsonCompact) ?? new T();

			await SaveStateAsync(fileName: fileName, state: data, ct: ct);

			lock (StateLock)
				File.Delete(path: legacyPath);

			return data;
		}
		catch (JsonException)
		{
			Log.Warning(messageTemplate: "JSON corruption in legacy file {LegacyPath}", legacyPath);
			var corruptedPath = legacyPath + ".corrupted";
			lock (StateLock)
				File.Move(sourceFileName: legacyPath, destFileName: corruptedPath, overwrite: true);
			return new T();
		}
	}

	private static string? GetLegacyPath(string fileName) =>
		LegacyMigrations.TryGetValue(key: fileName, out var legacy)
			? GetPath(fileName: legacy)
			: null;

	public static async Task SaveStateAsync<T>(
		string fileName,
		T state,
		CancellationToken ct = default
	)
	{
		ct.ThrowIfCancellationRequested();

		lock (StateLock)
		{
			var path = GetPath(fileName: fileName);
			var tempPath = path + $".{Guid.NewGuid()}.tmp";

			var json = JsonSerializer.Serialize(value: state, options: JsonIndented);
#pragma warning disable CA1849
			File.WriteAllText(path: tempPath, contents: json);
			File.Move(sourceFileName: tempPath, destFileName: path, overwrite: true);
#pragma warning restore CA1849
		}

		await Task.CompletedTask;
	}

	public static void Delete(string fileName)
	{
		var path = GetPath(fileName: fileName);
		if (File.Exists(path: path))
			File.Delete(path: path);
	}

	public static void DeleteAllStates()
	{
		if (!Directory.Exists(path: RootDirectory))
			return;

		Directory.Delete(path: RootDirectory, recursive: true);
		Log.Debug(messageTemplate: "Deleted all state files");
	}

	public static void DeleteLastFmStates()
	{
		Delete(fileName: LastFmSyncFile);
		Delete(fileName: LastFmScrobblesFile);
		Log.Debug(messageTemplate: "Deleted Last.fm state files");
	}

	private static string GetPath(string fileName)
	{
		var fullPath = Path.Combine(
			path1: RootDirectory,
			fileName.EndsWith(value: ".json") ? fileName : $"{fileName}.json"
		);
		var directory = Path.GetDirectoryName(path: fullPath);
		if (!IsNullOrEmpty(value: directory))
			Directory.CreateDirectory(path: directory);
		return fullPath;
	}

	internal static List<YouTubeVideo> LoadPlaylistCache(string playlistTitle)
	{
		Directory.CreateDirectory(path: YouTubePlaylistsDirectory);
		var path = GetPlaylistPath(playlistTitle: playlistTitle);
		if (!File.Exists(path: path))
			return [];

		var json = File.ReadAllText(path: path);
		return JsonSerializer.Deserialize<List<YouTubeVideo>>(json: json, options: JsonCompact)
			?? [];
	}

	public static void SavePlaylistCache(string playlistTitle, List<YouTubeVideo> videos)
	{
		Directory.CreateDirectory(path: YouTubePlaylistsDirectory);
		File.WriteAllText(
			GetPlaylistPath(playlistTitle: playlistTitle),
			JsonSerializer.Serialize(value: videos, options: JsonIndented)
		);
	}

	public static void DeletePlaylistCache(string playlistTitle)
	{
		var path = GetPlaylistPath(playlistTitle: playlistTitle);
		if (File.Exists(path: path))
			File.Delete(path: path);
	}

	public static void RenamePlaylistCache(string oldTitle, string newTitle)
	{
		var oldPath = GetPlaylistPath(playlistTitle: oldTitle);
		var newPath = GetPlaylistPath(playlistTitle: newTitle);

		if (File.Exists(path: oldPath) && !File.Exists(path: newPath))
			File.Move(sourceFileName: oldPath, destFileName: newPath);
	}

	public static bool PlaylistCacheExists(string playlistTitle) =>
		File.Exists(GetPlaylistPath(playlistTitle: playlistTitle));

	public static string ArchivePlaylistCache(string playlistTitle)
	{
		Directory.CreateDirectory(path: YouTubeDeletedDirectory);
		var sourcePath = GetPlaylistPath(playlistTitle: playlistTitle);
		var destPath = Path.Combine(
			path1: YouTubeDeletedDirectory,
			$"{playlistTitle.SanitizeFileName()}.json"
		);

		if (File.Exists(path: sourcePath))
			File.Move(sourceFileName: sourcePath, destFileName: destPath);

		return destPath;
	}

	public static void DeleteAllYouTubeStates()
	{
		Delete(fileName: YoutubeSyncFile);

		if (Directory.Exists(path: YouTubePlaylistsDirectory))
			Directory.Delete(path: YouTubePlaylistsDirectory, recursive: true);

		Log.Debug(messageTemplate: "Deleted YouTube state files");
	}

	public static void MigratePlaylistFiles(Dictionary<string, PlaylistSnapshot> snapshots)
	{
		List<string> oldFiles =
		[
			.. Directory.GetFiles(path: Paths.StateDirectory, searchPattern: "playlist_*.json"),
		];

		var oldPlaylistsDir = Path.Combine(path1: Paths.StateDirectory, path2: "playlists");
		if (Directory.Exists(path: oldPlaylistsDir))
			oldFiles.AddRange(Directory.GetFiles(path: oldPlaylistsDir, searchPattern: "*.json"));

		if (oldFiles.Count == 0)
			return;

		Directory.CreateDirectory(path: YouTubePlaylistsDirectory);
		var migrated = 0;

		foreach (var oldFile in oldFiles)
		{
			var fileName = Path.GetFileName(path: oldFile);
			var withoutExt = Path.GetFileNameWithoutExtension(path: fileName);
			var playlistId = withoutExt.StartsWith(value: "playlist_")
				? withoutExt[9..]
				: withoutExt;

			if (!snapshots.TryGetValue(key: playlistId, out PlaylistSnapshot? snapshot))
			{
				File.Delete(path: oldFile);
				Log.Debug(messageTemplate: "Deleted orphan playlist cache: {0}", fileName);
				continue;
			}

			var newPath = GetPlaylistPath(playlistTitle: snapshot.Title);

			if (!File.Exists(path: newPath))
			{
				File.Move(sourceFileName: oldFile, destFileName: newPath);
				migrated++;
				Log.Debug(
					messageTemplate: "Migrated: {0} → {1}",
					fileName,
					Path.GetFileName(path: newPath)
				);
			}
			else
				File.Delete(path: oldFile);
		}

		if (
			Directory.Exists(path: oldPlaylistsDir)
			&& Directory.GetFiles(path: oldPlaylistsDir, searchPattern: "*").Length == 0
		)
			Directory.Delete(path: oldPlaylistsDir, recursive: true);

		if (migrated > 0)
			Log.Information(
				messageTemplate: "Migrated {0} playlist cache files to new format",
				migrated
			);
	}

	private static string GetPlaylistPath(string playlistTitle) =>
		Path.Combine(path1: YouTubePlaylistsDirectory, $"{playlistTitle.SanitizeFileName()}.json");

	public static T? LoadReleaseCache<T>(string releaseId)
		where T : class
	{
		Directory.CreateDirectory(path: ReleaseCachePath);
		var path = GetReleasePath(releaseId: releaseId);
		if (!File.Exists(path: path))
			return null;

		var json = File.ReadAllText(path: path);
		return JsonSerializer.Deserialize<T>(json: json, options: JsonCompact);
	}

	public static void SaveReleaseCache<T>(string releaseId, T data)
	{
		Directory.CreateDirectory(path: ReleaseCachePath);
		File.WriteAllText(
			GetReleasePath(releaseId: releaseId),
			JsonSerializer.Serialize(value: data, options: JsonIndented)
		);
		Log.Debug(messageTemplate: "Saved release cache: {0}", releaseId);
	}

	public static bool ReleaseCacheExists(string releaseId) =>
		File.Exists(GetReleasePath(releaseId: releaseId));

	public static DateTimeOffset? GetReleaseCacheAge(string releaseId)
	{
		var path = GetReleasePath(releaseId: releaseId);
		return File.Exists(path: path)
			? new DateTimeOffset(File.GetLastWriteTimeUtc(path: path))
			: null;
	}

	public static void DeleteReleaseCache(string releaseId)
	{
		var path = GetReleasePath(releaseId: releaseId);
		if (File.Exists(path: path))
		{
			File.Delete(path: path);
			Log.Debug(messageTemplate: "Deleted release cache: {0}", releaseId);
		}
	}

	public static void DeleteAllReleaseCaches()
	{
		if (Directory.Exists(path: ReleaseCachePath))
		{
			Directory.Delete(path: ReleaseCachePath, recursive: true);
			Log.Debug(messageTemplate: "Deleted all release caches");
		}
	}

	public static IEnumerable<string> ListReleaseCaches()
	{
		if (!Directory.Exists(path: ReleaseCachePath))
			yield break;

		foreach (var file in Directory.GetFiles(path: ReleaseCachePath, searchPattern: "*.json"))
			yield return Path.GetFileNameWithoutExtension(path: file);
	}

	private static string GetReleasePath(string releaseId) =>
		Path.Combine(path1: ReleaseCachePath, $"{releaseId.SanitizeFileName()}.json");
}
