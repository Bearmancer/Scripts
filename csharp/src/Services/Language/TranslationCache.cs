using System.Security.Cryptography;
using System.Text;

namespace CSharpScripts.Services.Language;

internal static class TranslationCache
{
	private static readonly string CachePath = Path.Combine(
		path1: Paths.StateDirectory,
		path2: "translation-cache.json"
	);

	private static volatile Dictionary<string, string>? _memoryCache;

	private static readonly SemaphoreSlim FileLock = new(initialCount: 1, maxCount: 1);

	private static readonly JsonSerializerOptions SerializerOptions = new()
	{
		WriteIndented = false,
	};

	internal static async Task<string?> GetCachedAsync(
		string text,
		string targetLang,
		CancellationToken ct = default
	)
	{
		var key = ComputeKey(text: text, targetLang: targetLang);
		Dictionary<string, string> cache = await GetCacheAsync(ct);
		return CollectionExtensions.GetValueOrDefault(cache, key: key);
	}

	internal static async Task SetCachedAsync(
		string text,
		string targetLang,
		string translation,
		CancellationToken ct = default
	)
	{
		await FileLock.WaitAsync(ct);
		try
		{
			Dictionary<string, string> cache = await GetCacheAsync(ct);
			var key = ComputeKey(text: text, targetLang: targetLang);
			cache[key: key] = translation;
			await SaveAsync(cache: cache, ct);
			_memoryCache = cache;
		}
		finally
		{
			FileLock.Release();
		}
	}

	internal static async Task SetBatchCachedAsync(
		IEnumerable<(string Text, string TargetLang, string Translation)> entries,
		CancellationToken ct = default
	)
	{
		await FileLock.WaitAsync(ct);
		try
		{
			Dictionary<string, string> cache = await GetCacheAsync(ct);
			foreach ((var text, var lang, var translation) in entries)
			{
				var key = ComputeKey(text: text, targetLang: lang);
				cache[key: key] = translation;
			}

			await SaveAsync(cache: cache, ct);
			_memoryCache = cache;
		}
		finally
		{
			FileLock.Release();
		}
	}

	private static async Task<Dictionary<string, string>> GetCacheAsync(CancellationToken ct)
	{
		if (_memoryCache is not null)
			return _memoryCache;
		Dictionary<string, string> cache = await LoadAsync(ct);
		_memoryCache = cache;
		return cache;
	}

	private static async Task<Dictionary<string, string>> LoadAsync(CancellationToken ct)
	{
		if (!File.Exists(path: CachePath))
			return [];

		await using FileStream stream = File.OpenRead(path: CachePath);
		return await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(
				utf8Json: stream,
				options: SerializerOptions,
				ct
			) ?? [];
	}

	private static async Task SaveAsync(Dictionary<string, string> cache, CancellationToken ct)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(path: CachePath)!);
		await using FileStream stream = File.Create(path: CachePath);
		await JsonSerializer.SerializeAsync(
			utf8Json: stream,
			value: cache,
			options: SerializerOptions,
			ct
		);
	}

	private static string ComputeKey(string text, string targetLang)
	{
		var raw = Concat(text.AsSpan().Trim(), "::", targetLang.ToLowerInvariant());
		var hash = SHA256.HashData(Encoding.UTF8.GetBytes(s: raw));
		return Convert.ToHexString(inArray: hash)[..16];
	}
}



