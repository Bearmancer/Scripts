using CSharpScripts.Tests;

namespace CSharpScripts.CLI.Commands;

public sealed class TestYouTubeChangesCommand : Command<TestYouTubeChangesCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandOption("-p|--playlist")]
        [Description("Playlist ID to test (default: Bad playlist)")]
        public string? PlaylistId { get; init; }
    }

    public override int Execute(
        CommandContext context,
        Settings settings,
        CancellationToken cancellationToken
    )
    {
        Console.Level = LogLevel.Debug;

        try
        {
            YouTubeChangeDetectionTest test = new(Program.Cts.Token);
            test.RunAllTests();
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error("Test failed: {0}", ex.Message);
            if (ex.InnerException != null)
                Console.Error("Inner: {0}", ex.InnerException.Message);
            return 1;
        }
    }
}
