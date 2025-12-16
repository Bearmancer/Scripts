# .NET 10 Tab Completion

This document covers .NET's built-in tab completion and how to integrate it.

---

## .NET Built-in Tab Completion

Starting with .NET Core, the `dotnet` CLI supports shell tab completion natively.

**Reference**: https://learn.microsoft.com/en-us/dotnet/core/tools/enable-tab-autocomplete

---

## Installation by Shell

### PowerShell

Add to your `$PROFILE`:

```powershell
# .NET CLI tab completion
Register-ArgumentCompleter -Native -CommandName dotnet -ScriptBlock {
    param($wordToComplete, $commandAst, $cursorPosition)
    dotnet complete --position $cursorPosition "$commandAst" | ForEach-Object {
        [System.Management.Automation.CompletionResult]::new($_, $_, 'ParameterValue', $_)
    }
}
```

### Bash

Add to `~/.bashrc`:

```bash
# .NET CLI tab completion
_dotnet_bash_complete() {
    local cur="${COMP_WORDS[COMP_CWORD]}"
    local IFS=$'\n'
    COMPREPLY=( $(dotnet complete --position ${COMP_POINT} "${COMP_LINE}") )
}
complete -F _dotnet_bash_complete dotnet
```

### Zsh

Add to `~/.zshrc`:

```zsh
# .NET CLI tab completion
_dotnet_zsh_complete() {
    local completions=("$(dotnet complete --position ${CURSOR} "${words}")")
    reply=( "${(ps:\n:)completions}" )
}
compctl -K _dotnet_zsh_complete dotnet
```

---

## Custom Application Completion

For your own CLI application (not `dotnet`), you need to implement completion support.

### Using Spectre.Console.Cli

Spectre.Console.Cli has built-in support for generating completions:

```csharp
// In your app configuration
var app = new CommandApp();
app.Configure(config =>
{
    config.AddBranch("completion", completion =>
    {
        completion.AddCommand<InstallCompletionCommand>("install");
        completion.AddCommand<GenerateCompletionCommand>("generate");
    });
});
```

### Completion Command Implementation

```csharp
public class GenerateCompletionCommand : Command<GenerateCompletionCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<SHELL>")]
        public string Shell { get; init; } = "powershell";
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        // Generate shell-specific completion script
        var script = settings.Shell.ToLower() switch
        {
            "powershell" => GeneratePowerShellCompletion(),
            "bash" => GenerateBashCompletion(),
            "zsh" => GenerateZshCompletion(),
            _ => throw new ArgumentException($"Unknown shell: {settings.Shell}")
        };
        
        System.Console.WriteLine(script);
        return 0;
    }

    private string GeneratePowerShellCompletion() =>
        """
        Register-ArgumentCompleter -Native -CommandName scripts -ScriptBlock {
            param($wordToComplete, $commandAst, $cursorPosition)
            scripts completion complete --position $cursorPosition "$commandAst" | ForEach-Object {
                [System.Management.Automation.CompletionResult]::new($_, $_, 'ParameterValue', $_)
            }
        }
        """;
}
```

---

## Current Implementation Review

### Existing Code (CompletionCommands.cs)

The current implementation uses a static dictionary approach:

```csharp
// Current approach - manual completion definitions
private static readonly Dictionary<string, string[]> Completions = new()
{
    [""] = ["sync", "clean", "music", "mail", "completion", "-v", "--verbose"],
    ["sync"] = ["all", "yt", "lastfm", "status", "-v", "--verbose", "-r", "--reset"],
    // ...
};
```

### Migration to .NET 10

.NET 10 doesn't add new completion features for custom applications. The `dotnet complete` command is for the dotnet CLI itself.

**Recommendation**: Keep the current Spectre.Console.Cli approach, which handles completions well:

1. Spectre.Console.Cli automatically extracts commands and options
2. Manual dictionary can be replaced with reflection-based discovery
3. PowerShell `Register-ArgumentCompleter` still needed for shell integration

---

## Improved Implementation

### Auto-Discovery from Commands

```csharp
public class CompleteCommand : Command<CompleteCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandOption("--position")]
        public int Position { get; init; }

        [CommandArgument(0, "[PARTIAL]")]
        public string? Partial { get; init; }
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        var words = settings.Partial?.Split(' ') ?? [];
        var completions = GetCompletions(words, settings.Position);
        
        foreach (var completion in completions)
            System.Console.WriteLine(completion);
        
        return 0;
    }

    private IEnumerable<string> GetCompletions(string[] words, int position)
    {
        // Logic to determine context and return appropriate completions
        // Based on CommandApp configuration, not static dictionary
    }
}
```

---

## Installation Command

Users install completions with:

```powershell
# Generate and add to profile
scripts completion generate powershell >> $PROFILE

# Or use install command that does this automatically
scripts completion install
```

---

## Summary

| Aspect                   | Status                                   |
| ------------------------ | ---------------------------------------- |
| .NET built-in completion | For `dotnet` CLI only, not custom apps   |
| Spectre.Console.Cli      | Handles command/option discovery         |
| Shell integration        | Requires shell-specific scripts          |
| Current implementation   | Works, uses static dictionary            |
| Recommended improvement  | Auto-discover from CommandApp reflection |
