namespace CSharpScripts.Commands;

internal static class ResetHandler
{
    internal static void Execute(string service)
    {
        Logger.Info("Full reset: clearing state and rebuilding...");
        Logger.NewLine();

        ClearHandler.Execute(service: service, localOnly: false, remoteOnly: false);

        Logger.NewLine();
        CleanHandler.Execute();

        Logger.NewLine();
        Logger.Success("Reset complete.");
    }
}
