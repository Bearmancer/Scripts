namespace CSharpScripts.CLI.Commands;

#region Install Command

public sealed class CompletionInstallCommand : Command<CompletionInstallCommand.Settings>
{
	public override int Execute(
		CommandContext context,
		Settings settings,
		CancellationToken cancellationToken
	)
	{
		var profilePath = GetFolderPath(folder: SpecialFolder.UserProfile);
		var psProfilePath = Combine(
			path1: profilePath,
			path2: "Documents",
			path3: "PowerShell",
			path4: "Microsoft.PowerShell_profile.ps1"
		);

		var exePath =
			ProcessPath
			?? throw new InvalidOperationException(message: "Could not determine executable path");

		var completionScript =
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
			var existing = ReadAllText(path: psProfilePath);
			if (existing.Contains(value: marker))
			{
				Console.Info(message: "Tab completion already installed in profile");
				Console.Dim(text: psProfilePath);
				return 0;
			}
		}

		var profileDir = GetDirectoryName(path: psProfilePath);
		if (profileDir is { } && !Directory.Exists(path: profileDir))
			CreateDirectory(path: profileDir);

		AppendAllText(path: psProfilePath, NewLine + completionScript + NewLine);

		Console.WritePanel(
			header: "System Configuration",
			markupContent: $"[bold green]✓ Tab completion installed successfully![/]\n\n"
				+ $"[dim]Profile:[/]\n[link=file:///{psProfilePath}]{psProfilePath}[/]\n\n"
				+ $"[yellow]Action Required:[/]\nRestart PowerShell or run: [bold]. $PROFILE[/]"
		);

		return 0;
	}

	public sealed class Settings : CommandSettings { }
}

#endregion

#region Suggest Command

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
		[key: "music"] = ["search", "fill", "lookup", "schema"],
		[key: "music search"] = ["--source", "--mode", "--limit", "--fields", "--output", "-v"],
		[key: "music fill"] = ["--input", "--output", "-i", "-o"],
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
		var partial = settings.Partial?.Trim() ?? "";
		var words = partial.Split(' ', StringSplitOptions.RemoveEmptyEntries);

		List<string> suggestions = [];

		if (words.Length >= 1)
		{
			var lastWord = words[^1];
			if (
				lastWord.StartsWith(value: "--")
				&& OptionValues.TryGetValue(key: lastWord, out var values)
			)
			{
				suggestions.AddRange(collection: values);
			}
			else if (
				words.Length >= 2
				&& words[^2].StartsWith(value: "--")
				&& OptionValues.TryGetValue(words[^2], out var prevValues)
			)
			{
				suggestions.AddRange(prevValues.Where(v => v.StartsWith(value: lastWord)));
			}
			else
			{
				var contextKey = Join(separator: " ", words.Take(words.Length - 1));
				if (Commands.TryGetValue(key: contextKey, out var cmds))
					suggestions.AddRange(cmds.Where(c => c.StartsWith(value: lastWord)));
				else if (Commands.TryGetValue(words[0], out var subCmds))
					suggestions.AddRange(subCmds.Where(c => c.StartsWith(value: lastWord)));
			}
		}
		else
		{
			if (Commands.TryGetValue(key: "", out var rootCmds))
				suggestions.AddRange(collection: rootCmds);
		}

		foreach (var suggestion in suggestions.Distinct())
			System.Console.WriteLine(value: suggestion);

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
