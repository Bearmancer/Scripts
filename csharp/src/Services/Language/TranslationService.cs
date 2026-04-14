namespace CSharpScripts.Services.Language;

internal static class TranslationService
{
	private static readonly string LibreTranslateUrl =
		Secrets.LibreTranslateUrl ?? LibreTranslateHostManager.GetDefaultUrl();

	private static readonly TranslationClient Client = new(libreTranslateUrl: LibreTranslateUrl);

	public static string ToIso639_1(string iso639_3) =>
		TranslationClient.ToIso639_1(iso639_3: iso639_3);

	public static Task<TranslationResult?> TranslateAsync(
		string? text,
		string? sourceLanguage = null,
		CancellationToken ct = default
	) => Client.TranslateAsync(text: text, sourceLanguage: sourceLanguage, ct);

	public static Task<bool> EnsureContainerRunningAsync(
		bool offline = true,
		CancellationToken ct = default
	) => new LibreTranslateHostManager().EnsureRunningAsync(offline: offline, ct);

	public static async Task<T> WithContainerAsync<T>(
		Func<CancellationToken, Task<T>> operation,
		bool stopAfterwards = false,
		bool offline = true,
		CancellationToken ct = default
	)
	{
		var wasRunning = LibreTranslateHostManager.IsRunning();

		if (
			!wasRunning
			&& !await new LibreTranslateHostManager().EnsureRunningAsync(offline: offline, ct)
		)
			throw new InvalidOperationException(
				message: "Failed to start LibreTranslate container"
			);

		try
		{
			return await operation(arg: ct);
		}
		finally
		{
			if (stopAfterwards && !wasRunning)
			{
				Log.Debug("Stopping LibreTranslate container after operation...");
				LibreTranslateHostManager.Stop();
			}
		}
	}

	public static bool IsContainerRunning() => LibreTranslateHostManager.IsRunning();

	public static bool StartContainer(bool offline = true) =>
		new LibreTranslateHostManager().Start(offline: offline);

	public static bool StopContainer() => LibreTranslateHostManager.Stop();

	public static bool RemoveContainer() => LibreTranslateHostManager.Remove();

	public static bool PullImage() => LibreTranslateHostManager.PullImage();

	public static bool ImageExists() => LibreTranslateHostManager.ImageExists();

	public static void ShowStatus() => LibreTranslateHostManager.ShowStatus(url: LibreTranslateUrl);
}

internal record LibreTranslateResponse(
	[property: JsonPropertyName(name: "translatedText")] string TranslatedText
);

internal record TranslationResult(string Translation, string DetectedLanguage);
