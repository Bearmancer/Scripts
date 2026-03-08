namespace CSharpScripts.Services.Language;

internal sealed class TranslationClient(string libreTranslateUrl)
{
	private const int RequestTimeoutSeconds = 30;
	private const int MaxRetries = 3;

	private readonly string LibreTranslateUrl = libreTranslateUrl;

	private static readonly Dictionary<string, string> Iso6393To1 = new(
		StringComparer.OrdinalIgnoreCase
	)
	{
		["eng"] = "en",
		["deu"] = "de",
		["fra"] = "fr",
		["spa"] = "es",
		["ita"] = "it",
		["por"] = "pt",
		["nld"] = "nl",
		["rus"] = "ru",
		["jpn"] = "ja",
		["zho"] = "zh",
		["kor"] = "ko",
		["ara"] = "ar",
		["hin"] = "hi",
		["pol"] = "pl",
		["tur"] = "tr",
		["swe"] = "sv",
		["dan"] = "da",
		["nor"] = "no",
		["fin"] = "fi",
		["ces"] = "cs",
		["hun"] = "hu",
		["ron"] = "ro",
		["ell"] = "el",
		["heb"] = "he",
		["ukr"] = "uk",
		["vie"] = "vi",
		["tha"] = "th",
		["ind"] = "id",
	};

	public static string ToIso639_1(string iso639_3) =>
		Iso6393To1.TryGetValue(iso639_3, out var code) ? code : "auto";

	public async Task<TranslationResult?> TranslateAsync(
		string? text,
		string? sourceLanguage = null,
		CancellationToken ct = default
	)
	{
		Log.Debug(
			"TranslateAsync entry {Length} chars {SourceLanguage}",
			text?.Length ?? 0,
			sourceLanguage
		);
		if (IsNullOrWhiteSpace(text))
		{
			Log.Debug("TranslateAsync exit null (no text)");
			return null;
		}

		var langCode = sourceLanguage is { Length: 3 }
			? ToIso639_1(sourceLanguage)
			: sourceLanguage ?? "auto";

		for (var attempt = 1; attempt <= MaxRetries; attempt++)
		{
			try
			{
				using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
				cts.CancelAfter(TimeSpan.FromSeconds(RequestTimeoutSeconds));

				using RestClient client = new(
					new RestClientOptions { Timeout = TimeSpan.FromSeconds(RequestTimeoutSeconds) }
				);
				RestRequest request = new(LibreTranslateUrl, Method.Post);

				request.AddParameter("q", text);
				request.AddParameter("source", langCode);
				request.AddParameter("target", "en");
				request.AddParameter("format", "text");

				RestResponse response = await client.ExecuteAsync(request, cts.Token);

				if (response.IsSuccessful && !IsNullOrWhiteSpace(response.Content))
				{
					LibreTranslateResponse? result =
						JsonSerializer.Deserialize<LibreTranslateResponse>(
							response.Content,
							StateManager.JsonCompact
						);

					if (result is { TranslatedText.Length: > 0 })
						return new TranslationResult(
							result.TranslatedText,
							sourceLanguage ?? "auto"
						);
				}

				if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
				{
					Log.Debug("LibreTranslate unavailable, attempt {0}/{1}", attempt, MaxRetries);
					await Task.Delay(1000 * attempt, ct);
					continue;
				}

				Log.Debug(
					"Translation failed ({0}): {1}",
					response.StatusCode,
					response.Content ?? response.ErrorMessage ?? "Unknown error"
				);
				return null;
			}
			catch (TaskCanceledException) when (!ct.IsCancellationRequested)
			{
				Log.Debug("Translation timeout, attempt {0}/{1}", attempt, MaxRetries);
				if (attempt < MaxRetries)
					await Task.Delay(500 * attempt, ct);
			}
			catch (HttpRequestException ex)
			{
				Log.Debug("Translation HTTP error: {0}", ex.Message);
				if (attempt < MaxRetries)
					await Task.Delay(500 * attempt, ct);
			}
		}

		Log.Error("TranslateAsync exit null (failed all retries)");
		return null;
	}
}
