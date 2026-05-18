namespace CSharpScripts.CLI;

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
			UI.Exception(ex: ex);
			UI.Error(
				message: "Try again tomorrow or request quota increase from Google Cloud Console."
			);
			Log.Error(ex: ex, messageTemplate: "DailyQuotaExceeded {Message}", ex.Message);
			return 1;
		}
		catch (OperationCanceledException)
		{
			UI.Warn(message: "Operation cancelled by user");
			Log.Warning("OperationCancelled {Reason}", "Ctrl+C");
			return 130;
		}
		catch (HttpRequestException ex)
		{
			Log.Error(ex, "HttpRequestException {Summary}", FormatException(ex));
			UI.Exception(ex: ex);
			return 1;
		}
		catch (InvalidOperationException ex)
		{
			Log.Error(ex, "InvalidOperationException {Summary}", FormatException(ex));
			UI.Exception(ex: ex);
			return 1;
		}
		catch (IOException ex)
		{
			Log.Error(ex, "IOException {Summary}", FormatException(ex));
			UI.Exception(ex: ex);
			return 1;
		}
#pragma warning disable CA1031
		catch (Exception ex)
		{
			Log.Fatal(ex, "Command failed with unhandled exception");
			UI.Exception(ex: ex);
			return 1;
		}
#pragma warning restore CA1031
	}

	private static string FormatException(Exception ex) =>
		ex.InnerException is { } inner
			? $"{ex.GetType().Name}: {ex.Message} (Inner: {inner.Message})"
			: $"{ex.GetType().Name}: {ex.Message}";
}

