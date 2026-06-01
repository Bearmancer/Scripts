namespace Scripts.CLI;

internal abstract class BaseAsyncCommand<TSettings> : AsyncCommand<TSettings>
	where TSettings : CommandSettings
{
	protected static async Task<int> ExecuteWithErrorHandlingAsync(
		ServiceType service,
		Func<Task> action
	)
	{
		using IDisposable session = Log.BeginSession(service: service);

		try
		{
			await action();
			return 0;
		}
		catch (DailyQuotaExceededException ex)
		{
			Ui.Exception(ex: ex);
			Ui.Error(
				message: "Try again tomorrow or request quota increase from Google Cloud Console."
			);
			Log.Error(ex: ex, messageTemplate: "DailyQuotaExceeded {Message}", ex.Message);
			return 1;
		}
		catch (OperationCanceledException)
		{
			Ui.Warn(message: "Operation cancelled by user");
			Log.Warning(messageTemplate: "OperationCancelled {Reason}", "Ctrl+C");
			return 130;
		}
		catch (HttpRequestException ex)
		{
			Log.Error(
				ex: ex,
				messageTemplate: "HttpRequestException {Summary}",
				FormatException(ex: ex)
			);
			Ui.Exception(ex: ex);
			return 1;
		}
		catch (InvalidOperationException ex)
		{
			Log.Error(
				ex: ex,
				messageTemplate: "InvalidOperationException {Summary}",
				FormatException(ex: ex)
			);
			Ui.Exception(ex: ex);
			return 1;
		}
		catch (IOException ex)
		{
			Log.Error(ex: ex, messageTemplate: "IOException {Summary}", FormatException(ex: ex));
			Ui.Exception(ex: ex);
			return 1;
		}
#pragma warning disable CA1031
		catch (Exception ex)
		{
			Log.Fatal(ex: ex, messageTemplate: "Command failed with unhandled exception");
			Ui.Exception(ex: ex);
			return 1;
		}
#pragma warning restore CA1031
	}

	private static string FormatException(Exception ex) =>
		ex.InnerException is { } inner
			? $"{ex.GetType().Name}: {ex.Message} (Inner: {inner.Message})"
			: $"{ex.GetType().Name}: {ex.Message}";
}
