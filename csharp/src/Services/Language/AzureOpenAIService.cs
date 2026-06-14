using System.ClientModel;
using Azure.AI.OpenAI;
using OpenAI.Audio;
using OpenAI.Chat;

namespace Scripts.Services.Language;

internal static class AzureOpenAIService
{
	private static readonly AzureOpenAIClient? Client = string.IsNullOrWhiteSpace(
		Secrets.AzureOpenAIEndpoint
	)
		? null
		: new AzureOpenAIClient(
			new Uri(Secrets.AzureOpenAIEndpoint),
			Core.Auth.AzureAuth.Credential
		);

	internal static bool IsConfigured => !string.IsNullOrWhiteSpace(Secrets.AzureOpenAIEndpoint);

	internal static async Task<string?> TranscribeAudioAsync(
		byte[] audioBytes,
		string? audioFilename = null,
		CancellationToken ct = default
	)
	{
		_ = audioBytes ?? throw new ArgumentNullException(nameof(audioBytes));
		using var track = Log.Track(new { audioBytesLength = audioBytes.Length, audioFilename });

		if (audioBytes.Length == 0)
			throw new ArgumentException("Audio bytes cannot be empty.", nameof(audioBytes));

		if (Client is null)
			return null;

		var filename = audioFilename ?? "audio.wav";

		try
		{
			AudioClient audioClient = Client.GetAudioClient(
				Secrets.AzureOpenAIWhisperDeploymentName
			);
			using MemoryStream stream = new(audioBytes);
			ClientResult<AudioTranscription> response = await audioClient
				.TranscribeAudioAsync(
					audio: stream,
					audioFilename: filename,
					options: new AudioTranscriptionOptions(),
					cancellationToken: ct
				)
				.ConfigureAwait(continueOnCapturedContext: false);

			return response.Value?.Text;
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			Log.Warning("Azure Whisper transcription failed: {Error}", ex.Message);
			return null;
		}
	}

	internal static async Task<string?> TranscribeAudioSrtAsync(
		byte[] audioBytes,
		string? audioFilename = null,
		CancellationToken ct = default
	)
	{
		_ = audioBytes ?? throw new ArgumentNullException(nameof(audioBytes));
		using var track = Log.Track(new { audioBytesLength = audioBytes.Length, audioFilename });

		if (audioBytes.Length == 0)
			throw new ArgumentException("Audio bytes cannot be empty.", nameof(audioBytes));

		if (Client is null)
			return null;

		var filename = audioFilename ?? "audio.wav";

		try
		{
			AudioClient audioClient = Client.GetAudioClient(
				Secrets.AzureOpenAIWhisperDeploymentName
			);
			using MemoryStream stream = new(audioBytes);
			ClientResult<AudioTranscription> response = await audioClient
				.TranscribeAudioAsync(
					audio: stream,
					audioFilename: filename,
					options: new AudioTranscriptionOptions
					{
						ResponseFormat = AudioTranscriptionFormat.Srt
					},
					cancellationToken: ct
				)
				.ConfigureAwait(continueOnCapturedContext: false);

			return response.Value?.Text;
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			Log.Warning("Azure Whisper SRT transcription failed: {Error}", ex.Message);
			return null;
		}
	}

	internal static async Task<string?> TranslateWithLlmAsync(
		string text,
		string targetLanguage = "en",
		string? sourceLanguage = null,
		CancellationToken ct = default
	)
	{
		_ = text ?? throw new ArgumentNullException(nameof(text));
		using var track = Log.Track(new { textLength = text.Length, targetLanguage, sourceLanguage });

		if (string.IsNullOrWhiteSpace(text))
			throw new ArgumentException("Text cannot be empty.", nameof(text));

		if (Client is null)
			return null;

		try
		{
			ChatClient chatClient = Client.GetChatClient(Secrets.AzureOpenAIDeploymentName);
			List<ChatMessage> messages = new(capacity: 2)
			{
				new SystemChatMessage(BuildSystemPrompt(targetLanguage, sourceLanguage)),
				new UserChatMessage(text),
			};
			ClientResult<ChatCompletion> response = await chatClient
				.CompleteChatAsync(messages, cancellationToken: ct)
				.ConfigureAwait(continueOnCapturedContext: false);

			return ExtractText(response.Value);
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			Log.Warning("Azure OpenAI LLM translation failed: {Error}", ex.Message);
			return null;
		}
	}

	private static string BuildSystemPrompt(string targetLanguage, string? sourceLanguage) =>
		sourceLanguage is null
			? $"You are a translator. Translate the user's text to {targetLanguage}. Respond with ONLY the translated text, no commentary, no quotes, no explanation."
			: $"You are a translator. Translate the user's text from {sourceLanguage} to {targetLanguage}. Respond with ONLY the translated text, no commentary, no quotes, no explanation.";

	private static string? ExtractText(ChatCompletion completion)
	{
		if (completion?.Content is not { Count: > 0 } parts)
			return null;
		foreach (ChatMessageContentPart part in parts)
		{
			if (part.Text is { Length: > 0 } text)
				return text;
		}
		return null;
	}
}
