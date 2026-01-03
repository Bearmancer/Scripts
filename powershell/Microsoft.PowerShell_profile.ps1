Set-StrictMode -Version Latest

[Console]::InputEncoding = [Console]::OutputEncoding = $Global:OutputEncoding = [System.Text.Encoding]::UTF8
$env:PYTHONIOENCODING = 'utf-8'
$env:PYTHONWARNINGS = 'ignore'

Set-PSReadLineOption -PredictionSource History -PredictionViewStyle InlineView -Colors @{ Selection = "`e[7m" }
Set-PSReadLineKeyHandler -Key Tab -Function MenuComplete

Import-Module PSCompletions

$env:CARAPACE_BRIDGES = 'zsh,fish,bash,inshellisense'
carapace _carapace | Out-String | Invoke-Expression

#region Native Command Completers
$nativeCompleters = @{
    dotnet = { 
        param($wordToComplete, $commandAst, $cursorPosition)
        dotnet complete --position $cursorPosition $commandAst 2>$null
    }
    winget = { 
        param($wordToComplete, $commandAst, $cursorPosition)
        winget complete --word=$wordToComplete --commandline $commandAst --position $cursorPosition 2>$null
    }
}

foreach ($completer in $nativeCompleters.GetEnumerator()) {
    $commandName = $completer.Key
    $scriptBlock = $completer.Value
    
    Register-ArgumentCompleter -Native -CommandName $commandName -ScriptBlock {
        param($wordToComplete, $commandAst, $cursorPosition)
        
        $completions = & $scriptBlock $wordToComplete $commandAst $cursorPosition
        
        foreach ($completion in $completions) {
            [System.Management.Automation.CompletionResult]::new(
                $completion,
                $completion,
                'ParameterValue',
                $completion
            )
        }
    }.GetNewClosure()
}
#endregion

#region PSFzf Configuration
$env:FZF_DEFAULT_OPTS = '--height 40% --layout=reverse --border --info=inline --tiebreak=length,begin'
$Script:PSFzfLoaded = $false

function Initialize-PSFzf {
    if ($Script:PSFzfLoaded) { return }
    $Script:PSFzfLoaded = $true
    Import-Module PSFzf
    Set-PsFzfOption -PSReadlineChordProvider 'Ctrl+t' -PSReadlineChordReverseHistory 'Ctrl+r'
}

@{
    'Ctrl+r' = { Initialize-PSFzf; Invoke-FzfPsReadlineHandlerHistory }
    'Ctrl+t' = { Initialize-PSFzf; Invoke-FzfPsReadlineHandlerProvider }
    'Alt+c'  = { Initialize-PSFzf; Invoke-FzfPsReadlineHandlerSetLocation }
    'Alt+a'  = { Initialize-PSFzf; Invoke-FzfPsReadlineHandlerHistoryArgs }
}.GetEnumerator() | ForEach-Object {
    Set-PSReadLineKeyHandler -Key $_.Key -ScriptBlock $_.Value
}
#endregion