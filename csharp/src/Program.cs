using CliFx;

namespace CSharpScripts;

public static class Program
{
    public static readonly CancellationTokenSource Cts = new();

    private static async Task<int> Main(string[] args)
    {
        System.Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            Cts.Cancel();
            Console.Error("Shutdown requested...");
        };

        return await new CliApplicationBuilder()
            .AddCommandsFromThisAssembly()
            .SetTitle("Lance's Utilities")
            .SetDescription("CLI toolkit for automation and data sync")
            .SetExecutableName("cli")
            .Build()
            .RunAsync(args);
    }
}
