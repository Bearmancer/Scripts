namespace CSharpScripts.CLI.Commands;

public sealed class CompletionInstallCommand : Command<CompletionInstallCommand.Settings>
{
    public sealed class Settings : CommandSettings { }

    public override int Execute(
        CommandContext context,
        Settings settings,
        CancellationToken cancellationToken
    )
    {
        string profilePath = GetFolderPath(SpecialFolder.UserProfile);
        string psProfilePath = Path.Combine(
            profilePath,
            "Documents",
            "PowerShell",
            "Microsoft.PowerShell_profile.ps1"
        );

        // Get the path to this executable
        string exePath =
            ProcessPath
            ?? throw new InvalidOperationException("Could not determine executable path");

        // Build the PowerShell script
        string completionScript =
            @"
# scripts CLI tab completion (auto-generated)
Register-ArgumentCompleter -Native -CommandName scripts -ScriptBlock {
    param($wordToComplete, $commandAst, $cursorPosition)
    $words = $commandAst.ToString() -split '\s+'
    & """
            + exePath.Replace("\\", "\\\\")
            + @""" completion suggest $($words[1..($words.Length-1)] -join ' ') 2>$null | ForEach-Object {
        [System.Management.Automation.CompletionResult]::new($_, $_, 'ParameterValue', $_)
    }
}";

        string marker = "# scripts CLI tab completion";

        // Check if already installed
        if (File.Exists(psProfilePath))
        {
            string existing = File.ReadAllText(psProfilePath);
            if (existing.Contains(marker))
            {
                Console.Info("Tab completion already installed in profile");
                Console.Dim(psProfilePath);
                return 0;
            }
        }

        // Ensure directory exists
        string? profileDir = Path.GetDirectoryName(psProfilePath);
        if (profileDir is not null && !Directory.Exists(profileDir))
            Directory.CreateDirectory(profileDir);

        // Append to profile
        File.AppendAllText(psProfilePath, NewLine + completionScript + NewLine);

        Console.Success("Tab completion installed!");
        Console.Info("Profile: {0}", psProfilePath);
        Console.Warning("Restart PowerShell or run: . $PROFILE");

        return 0;
    }
}

public sealed class CompletionSuggestCommand : Command<CompletionSuggestCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "[PARTIAL]")]
        [Description("Partial command to complete")]
        public string? Partial { get; init; }
    }

    // All available commands and subcommands
    static readonly FrozenDictionary<string, string[]> Commands = new Dictionary<string, string[]>
    {
        [""] = ["sync", "clean", "music", "mail", "completion", "-v", "--verbose"],
        ["sync"] = ["all", "yt", "lastfm", "status", "-v", "--verbose", "-r", "--reset"],
        ["clean"] = ["local", "purge"],
        ["music"] = ["search", "lookup", "schema"],
        ["music search"] = ["--source", "--mode", "--limit", "--fields", "--output", "-v"],
        ["mail"] = ["create"],
        ["completion"] = ["install", "suggest"],
    }.ToFrozenDictionary();

    // Option values
    static readonly FrozenDictionary<string, string[]> OptionValues = new Dictionary<
        string,
        string[]
    >
    {
        ["--source"] = ["discogs", "musicbrainz"],
        ["--mode"] = ["pop", "classical"],
        ["--output"] = ["table", "json"],
        ["--fields"] =
        [
            "default",
            "all",
            "artist",
            "album",
            "year",
            "label",
            "country",
            "format",
            "barcode",
            "genre",
            "style",
            "id",
        ],
    }.ToFrozenDictionary();

    public override int Execute(
        CommandContext context,
        Settings settings,
        CancellationToken cancellationToken
    )
    {
        string partial = settings.Partial?.Trim() ?? "";
        string[] words = partial.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        List<string> suggestions = [];

        // Check if we're completing an option value
        if (words.Length >= 1)
        {
            string lastWord = words[^1];
            if (
                lastWord.StartsWith("--")
                && OptionValues.TryGetValue(lastWord, out string[]? values)
            )
            {
                suggestions.AddRange(values);
            }
            else if (
                words.Length >= 2
                && words[^2].StartsWith("--")
                && OptionValues.TryGetValue(words[^2], out string[]? prevValues)
            )
            {
                // Filter values that start with current input
                suggestions.AddRange(
                    prevValues.Where(v =>
                        v.StartsWith(lastWord, StringComparison.OrdinalIgnoreCase)
                    )
                );
            }
            else
            {
                // Build context key from words
                string contextKey = string.Join(" ", words.Take(words.Length - 1));
                if (Commands.TryGetValue(contextKey, out string[]? cmds))
                {
                    suggestions.AddRange(
                        cmds.Where(c => c.StartsWith(lastWord, StringComparison.OrdinalIgnoreCase))
                    );
                }
                else if (Commands.TryGetValue(words[0], out string[]? subCmds))
                {
                    suggestions.AddRange(
                        subCmds.Where(c =>
                            c.StartsWith(lastWord, StringComparison.OrdinalIgnoreCase)
                        )
                    );
                }
            }
        }
        else
        {
            // Root level
            if (Commands.TryGetValue("", out string[]? rootCmds))
            {
                suggestions.AddRange(rootCmds);
            }
        }

        // Output suggestions (one per line for PowerShell)
        foreach (string suggestion in suggestions.Distinct())
        {
            Console.WriteLine(suggestion);
        }

        return 0;
    }
}
