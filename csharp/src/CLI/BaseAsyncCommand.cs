namespace CSharpScripts.CLI;

internal abstract class BaseAsyncCommand<TSettings> : AsyncCommand<TSettings>
	where TSettings : CommandSettings
{
	protected static async Task<int> ExecuteWithErrorHandlingAsync(
		ServiceType service,
		Func<Task> action
	)
	{
		using IDisposable session = Log.BeginSession(service);

		try
		{
			await action();
			return 0;
		}
		catch (DailyQuotaExceededException ex)
		{
			UI.Exception(ex);
			UI.Error("Try again tomorrow or request quota increase from Google Cloud Console.");
			Log.Error(ex, "DailyQuotaExceeded {Message}", ex.Message);
			return 1;
		}
		catch (OperationCanceledException)
		{
			UI.Warn("Operation cancelled by user");
			Log.Warning("OperationCancelled {Reason}", "Ctrl+C");
			return 130;
		}
		catch (HttpRequestException ex)
		{
			UI.Exception(ex);
			if (ex.StackTrace?.Split('\n') is [var firstLine, ..])
				Log.Debug("HttpRequestException_Stack {FirstLine}", firstLine.Trim());

			var summary = ex.InnerException is { } inner
				? $"{ex.GetType().Name}: {ex.Message} (Inner: {inner.Message})"
				: $"{ex.GetType().Name}: {ex.Message}";

			Log.Error(ex, "HttpRequestException {Summary}", summary);
			return 1;
		}
		catch (InvalidOperationException ex)
		{
			UI.Exception(ex);
			if (ex.StackTrace?.Split('\n') is [var firstLine, ..])
				Log.Debug("InvalidOperationException_Stack {FirstLine}", firstLine.Trim());

			var summary = ex.InnerException is { } inner
				? $"{ex.GetType().Name}: {ex.Message} (Inner: {inner.Message})"
				: $"{ex.GetType().Name}: {ex.Message}";

			Log.Error(ex, "InvalidOperationException {Summary}", summary);
			return 1;
		}
		catch (IOException ex)
		{
			UI.Exception(ex);
			if (ex.StackTrace?.Split('\n') is [var firstLine, ..])
				Log.Debug("IOException_Stack {FirstLine}", firstLine.Trim());

			var summary = ex.InnerException is { } inner
				? $"{ex.GetType().Name}: {ex.Message} (Inner: {inner.Message})"
				: $"{ex.GetType().Name}: {ex.Message}";

			Log.Error(ex, "IOException {Summary}", summary);
			return 1;
		}
#pragma warning disable CA1031
		catch (Exception ex)
		{
			Log.Fatal(ex, "Command failed with unhandled exception");
			UI.Exception(ex);
			return 1;
		}
#pragma warning restore CA1031
	}
}
