namespace CSharpScripts.CLI.Commands;

#region CompletionInstallCommand

public sealed class CompletionInstallCommand : Command<CompletionInstallCommand.Settings>
{
    public override int Execute(
        CommandContext context,
        Settings settings,
        CancellationToken cancellationToken
    )
    {
        string profilePath = GetFolderPath(folder: SpecialFolder.UserProfile);
        string psProfilePath = Combine(
            path1: profilePath,
            path2: "Documents",
            path3: "PowerShell",
            path4: "Microsoft.PowerShell_profile.ps1"
        );

        string exePath =
            ProcessPath
            ?? throw new InvalidOperationException(message: "Could not determine executable path");

        string completionScript =
            @"
# scripts CLI tab completion (auto-generated)
Register-ArgumentCompleter -Native -CommandName scripts -ScriptBlock {
    param($wordToComplete, $commandAst, $cursorPosition)
    $words = $commandAst.ToString() -split '\s+'
    & """
            + exePath.Replace(oldValue: "\\", newValue: "\\\\")
            + @""" completion suggest $($words[1..($words.Length-1)] -join ' ') 2>$null | ForEach-Object {
        [System.Management.Automation.CompletionResult]::new($_, $_, 'ParameterValue', $_)
    }
}";

        var marker = "# scripts CLI tab completion";

        if (File.Exists(path: psProfilePath))
        {
            string existing = ReadAllText(path: psProfilePath);
            if (existing.Contains(value: marker))
            {
                Console.Info(message: "Tab completion already installed in profile");
                Console.Dim(text: psProfilePath);
                return 0;
            }
        }

        string? profileDir = GetDirectoryName(path: psProfilePath);
        if (profileDir is { } && !Directory.Exists(path: profileDir))
            CreateDirectory(path: profileDir);

        AppendAllText(path: psProfilePath, NewLine + completionScript + NewLine);

        Console.Success(message: "Tab completion installed!");
        Console.Info(message: "Profile: {0}", psProfilePath);
        Console.Warning(message: "Restart PowerShell or run: . $PROFILE");

        return 0;
    }

    public sealed class Settings : CommandSettings { }
}

#endregion

#region CompletionSuggestCommand

public sealed class CompletionSuggestCommand : Command<CompletionSuggestCommand.Settings>
{
    private static readonly FrozenDictionary<string, string[]> Commands = new Dictionary<
        string,
        string[]
    >
    {
        [key: ""] = ["sync", "clean", "music", "mail", "completion", "-v", "--verbose"],
        [key: "sync"] = ["all", "yt", "lastfm", "status", "-v", "--verbose", "-r", "--reset"],
        [key: "clean"] = ["local", "purge"],
        [key: "music"] = ["search", "lookup", "schema"],
        [key: "music search"] = ["--source", "--mode", "--limit", "--fields", "--output", "-v"],
        [key: "mail"] = ["create"],
        [key: "completion"] = ["install", "suggest"],
    }.ToFrozenDictionary();

    private static readonly FrozenDictionary<string, string[]> OptionValues = new Dictionary<
        string,
        string[]
    >
    {
        [key: "--source"] = ["discogs", "musicbrainz"],
        [key: "--mode"] = ["pop", "classical"],
        [key: "--output"] = ["table", "json"],
        [key: "--fields"] =
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
        string[] words = partial.Split(
            separator: ' ',
            options: StringSplitOptions.RemoveEmptyEntries
        );

        List<string> suggestions = [];

        if (words.Length >= 1)
        {
            string lastWord = words[^1];
            if (
                lastWord.StartsWith(value: "--", comparisonType: StringComparison.Ordinal)
                && OptionValues.TryGetValue(key: lastWord, out string[]? values)
            )
            {
                suggestions.AddRange(collection: values);
            }
            else if (
                words.Length >= 2
                && words[^2].StartsWith(value: "--", comparisonType: StringComparison.Ordinal)
                && OptionValues.TryGetValue(words[^2], out string[]? prevValues)
            )
            {
                suggestions.AddRange(
                    prevValues.Where(v =>
                        v.StartsWith(
                            value: lastWord,
                            comparisonType: StringComparison.OrdinalIgnoreCase
                        )
                    )
                );
            }
            else
            {
                string contextKey = Join(separator: " ", words.Take(words.Length - 1));
                if (Commands.TryGetValue(key: contextKey, out string[]? cmds))
                    suggestions.AddRange(
                        cmds.Where(c =>
                            c.StartsWith(
                                value: lastWord,
                                comparisonType: StringComparison.OrdinalIgnoreCase
                            )
                        )
                    );
                else if (Commands.TryGetValue(words[0], out string[]? subCmds))
                    suggestions.AddRange(
                        subCmds.Where(c =>
                            c.StartsWith(
                                value: lastWord,
                                comparisonType: StringComparison.OrdinalIgnoreCase
                            )
                        )
                    );
            }
        }
        else
        {
            if (Commands.TryGetValue(key: "", out string[]? rootCmds))
                suggestions.AddRange(collection: rootCmds);
        }

        foreach (string suggestion in suggestions.Distinct())
            Console.WriteLine(text: suggestion);

        return 0;
    }

    public sealed class Settings : CommandSettings
    {
        [CommandArgument(position: 0, template: "[PARTIAL]")]
        [Description(description: "Partial command to complete")]
        public string? Partial { get; init; }
    }
}

#endregion
