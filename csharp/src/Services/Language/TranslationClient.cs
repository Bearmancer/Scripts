using System.Net;

namespace CSharpScripts.Services.Language;

internal sealed class TranslationClient(string libreTranslateUrl)
{
	private const int RequestTimeoutSeconds = 30;
	private const int MaxRetries = 3;

	private static readonly FrozenDictionary<string, string> Iso6393To1 = new Dictionary<
		string,
		string
	>(comparer: StringComparer.OrdinalIgnoreCase)
	{
		[key: "eng"] = "en",
		[key: "deu"] = "de",
		[key: "fra"] = "fr",
		[key: "spa"] = "es",
		[key: "ita"] = "it",
		[key: "por"] = "pt",
		[key: "nld"] = "nl",
		[key: "rus"] = "ru",
		[key: "jpn"] = "ja",
		[key: "zho"] = "zh",
		[key: "kor"] = "ko",
		[key: "ara"] = "ar",
		[key: "hin"] = "hi",
		[key: "pol"] = "pl",
		[key: "tur"] = "tr",
		[key: "swe"] = "sv",
		[key: "dan"] = "da",
		[key: "nor"] = "no",
		[key: "fin"] = "fi",
		[key: "ces"] = "cs",
		[key: "hun"] = "hu",
		[key: "ron"] = "ro",
		[key: "ell"] = "el",
		[key: "heb"] = "he",
		[key: "ukr"] = "uk",
		[key: "vie"] = "vi",
		[key: "tha"] = "th",
		[key: "ind"] = "id"
	}.ToFrozenDictionary(comparer: StringComparer.OrdinalIgnoreCase);

	public static string ToIso639_1(string iso6393) =>
		Iso6393To1.GetValueOrDefault(key: iso6393, defaultValue: "auto");

	public async Task<TranslationResult?> TranslateAsync(
		string? text,
		string? sourceLanguage = null,
		CancellationToken ct = default
	)
	{
		Log.Debug(
			messageTemplate: "TranslateAsync entry {Length} chars {SourceLanguage}",
			text?.Length ?? 0,
			sourceLanguage
		);
		if (IsNullOrWhiteSpace(value: text))
		{
			Log.Debug(messageTemplate: "TranslateAsync exit null (no text)");
			return null;
		}

		var langCode = sourceLanguage is { Length: 3 }
			? ToIso639_1(iso6393: sourceLanguage)
			: sourceLanguage ?? "auto";

		using RestClient client = new(
			new RestClientOptions { Timeout = TimeSpan.FromSeconds(seconds: RequestTimeoutSeconds) }
		);

		for (var attempt = 1; attempt <= MaxRetries; attempt++)
		{
			try
			{
				using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(
					token: ct
				);
				cts.CancelAfter(TimeSpan.FromSeconds(seconds: RequestTimeoutSeconds));

				RestRequest request = new(resource: libreTranslateUrl, method: Method.Post);

				request.AddParameter(name: "q", value: text);
				request.AddParameter(name: "source", value: langCode);
				request.AddParameter(name: "target", value: "en");
				request.AddParameter(name: "format", value: "text");

				RestResponse response = await client.ExecuteAsync(
					request: request,
					cancellationToken: cts.Token
				);

				if (response.IsSuccessful && !IsNullOrWhiteSpace(value: response.Content))
				{
					LibreTranslateResponse? result =
						JsonSerializer.Deserialize<LibreTranslateResponse>(
							json: response.Content,
							options: StateManager.JsonCompact
						);

					if (result is { TranslatedText.Length: > 0 })
					{
						return new TranslationResult(
							Translation: result.TranslatedText,
							sourceLanguage ?? "auto"
						);
					}
				}

				if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
				{
					Log.Debug(messageTemplate: "LibreTranslate unavailable, attempt {0}/{1}", attempt, MaxRetries);
					await Task.Delay(1000 * attempt, cancellationToken: ct);
					continue;
				}

				Log.Debug(
					messageTemplate: "Translation failed ({0}): {1}",
					response.StatusCode,
					response.Content ?? response.ErrorMessage ?? "Unknown error"
				);
				return null;
			}
			catch (TaskCanceledException) when (!ct.IsCancellationRequested)
			{
				Log.Debug(messageTemplate: "Translation timeout, attempt {0}/{1}", attempt, MaxRetries);
				if (attempt < MaxRetries)
					await Task.Delay(500 * attempt, cancellationToken: ct);
			}
			catch (HttpRequestException ex)
			{
				Log.Error(ex: ex, messageTemplate: "Translation HTTP error: {Message}", ex.Message);
				if (attempt < MaxRetries)
					await Task.Delay(500 * attempt, cancellationToken: ct);
			}
		}

		Log.Error(messageTemplate: "TranslateAsync exit null (failed all retries)");
		return null;
	}
}
