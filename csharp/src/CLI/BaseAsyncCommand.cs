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
			UI.Exception(ex: ex);
			if (ex.StackTrace is { } st)
			{
				var nl = st.AsSpan().IndexOf('\n');
				Log.Debug(
					"HttpRequestException_Stack {FirstLine}",
					(nl >= 0 ? st[..nl] : st).Trim()
				);
			}
			Log.Error(
				ex: ex,
				messageTemplate: "HttpRequestException {Summary}",
				FormatException(ex)
			);
			return 1;
		}
		catch (InvalidOperationException ex)
		{
			UI.Exception(ex: ex);
			if (ex.StackTrace is { } st)
			{
				var nl = st.AsSpan().IndexOf('\n');
				Log.Debug(
					"InvalidOperationException_Stack {FirstLine}",
					(nl >= 0 ? st[..nl] : st).Trim()
				);
			}
			Log.Error(
				ex: ex,
				messageTemplate: "InvalidOperationException {Summary}",
				FormatException(ex)
			);
			return 1;
		}
		catch (IOException ex)
		{
			UI.Exception(ex: ex);
			if (ex.StackTrace is { } st)
			{
				var nl = st.AsSpan().IndexOf('\n');
				Log.Debug("IOException_Stack {FirstLine}", (nl >= 0 ? st[..nl] : st).Trim());
			}
			Log.Error(ex: ex, messageTemplate: "IOException {Summary}", FormatException(ex));
			return 1;
		}
#pragma warning disable CA1031
		catch (Exception ex)
		{
			Log.Fatal(ex: ex, messageTemplate: "Command failed with unhandled exception");
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
