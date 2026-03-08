namespace CSharpScripts.Services.Language;

using System.Security.Cryptography;
using System.Text;

internal static class TranslationCache
{
	private static readonly string CachePath = Path.Combine(
		Paths.StateDirectory,
		"translation-cache.json"
	);

	private static readonly SemaphoreSlim FileLock = new(1, 1);

	private static readonly JsonSerializerOptions SerializerOptions = new()
	{
		WriteIndented = false,
	};

	/// <summary>
	/// Retrieves a cached translation for the given text and target language, or null if not cached.
	/// </summary>
	internal static async Task<string?> GetCachedAsync(
		string text,
		string targetLang,
		CancellationToken ct = default
	)
	{
		var key = ComputeKey(text, targetLang);
		Dictionary<string, string> cache = await LoadAsync(ct);
		return cache.GetValueOrDefault(key);
	}

	/// <summary>
	/// Stores a translation result in the cache.
	/// </summary>
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
			Dictionary<string, string> cache = await LoadAsync(ct);
			var key = ComputeKey(text, targetLang);
			cache[key] = translation;
			await SaveAsync(cache, ct);
		}
		finally
		{
			FileLock.Release();
		}
	}

	/// <summary>
	/// Stores multiple translation results in the cache in a single write.
	/// </summary>
	internal static async Task SetBatchCachedAsync(
		IEnumerable<(string Text, string TargetLang, string Translation)> entries,
		CancellationToken ct = default
	)
	{
		await FileLock.WaitAsync(ct);
		try
		{
			Dictionary<string, string> cache = await LoadAsync(ct);
			foreach ((var text, var lang, var translation) in entries)
			{
				var key = ComputeKey(text, lang);
				cache[key] = translation;
			}

			await SaveAsync(cache, ct);
		}
		finally
		{
			FileLock.Release();
		}
	}

	private static async Task<Dictionary<string, string>> LoadAsync(CancellationToken ct)
	{
		if (!File.Exists(CachePath))
			return [];

		await using FileStream stream = File.OpenRead(CachePath);
		return await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(
				stream,
				SerializerOptions,
				ct
			) ?? [];
	}

	private static async Task SaveAsync(Dictionary<string, string> cache, CancellationToken ct)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(CachePath)!);
		await using FileStream stream = File.Create(CachePath);
		await JsonSerializer.SerializeAsync(stream, cache, SerializerOptions, ct);
	}

	private static string ComputeKey(string text, string targetLang)
	{
		var raw = $"{text.Trim()}::{targetLang.ToLowerInvariant()}";
		var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
		return Convert.ToHexString(hash)[..16];
	}
}
