namespace CSharpScripts.Commands;

internal static class SyncHandler
{
    internal static void YouTube(CancellationToken ct, bool force = false)
    {
        Logger.Start(ServiceType.YouTube);

        ExecuteWithErrorHandling(() =>
        {
            if (force)
            {
                Logger.Info("Force sync: clearing cache and re-fetching all data...");
                StateManager.DeleteYouTubeStates();
            }
            new YouTubePlaylistOrchestrator(ct).Execute();
        });
    }

    internal static void LastFm(CancellationToken ct)
    {
        Logger.Start(ServiceType.LastFm);

        ExecuteWithErrorHandling(() =>
        {
            Logger.Info("Starting Last.fm sync...");
            new ScrobbleSyncOrchestrator(ct).Execute();
        });
    }

    static void ExecuteWithErrorHandling(Action action)
    {
        try
        {
            action();
        }
        catch (DailyQuotaExceededException ex)
        {
            Logger.Error(ex.Message);
            Logger.Error("Try again tomorrow or request quota increase from Google Cloud Console.");
            Logger.End(success: false, summary: "Daily quota exceeded");
        }
        catch (RetryExhaustedException ex)
        {
            Logger.Error(ex.Message);
            Logger.Error("Wait 15-30 minutes and try again. Progress has been saved.");
            Logger.End(success: false, summary: "Retry limit reached");
        }
        catch (AggregateException aex)
        {
            foreach (var ex in aex.InnerExceptions)
            {
                Logger.Error("{0}: {1}", ex.GetType().Name, ex.Message);
                Logger.FileError(ex.Message, ex);
            }
            Logger.End(
                success: false,
                summary: $"Failed with {aex.InnerExceptions.Count} error(s)"
            );
        }
        catch (OperationCanceledException)
        {
            Logger.Warning("Operation cancelled by user");
            Logger.Interrupted("Cancelled by Ctrl+C");
        }
        catch (Exception ex)
        {
            Logger.Error("{0}: {1}", ex.GetType().Name, ex.Message);
            Logger.FileError(ex.Message, ex);
            Logger.End(success: false, summary: ex.Message);
        }
    }
}
