Set-StrictMode -Version Latest

# #region Timing Infrastructure
# $Script:ProfileStart = [System.Diagnostics.Stopwatch]::StartNew()
# $Script:ModuleTimes = [ordered]@{}

# function Write-ProfileTiming {
#     param([string]$Component)
#     $Script:ModuleTimes[$Component] = $Script:ProfileStart.ElapsedMilliseconds
# }

# function Show-ProfileSummary {
#     $prev = 0
#     Write-Host "`nProfile timing:" -ForegroundColor Cyan
#     foreach ($item in $Script:ModuleTimes.GetEnumerator()) {
#         $delta = $item.Value - $prev
#         Write-Host ("  {0,-20} {1,4}ms (+{2})" -f $item.Key, $item.Value, $delta) -ForegroundColor DarkGray
#         $prev = $item.Value
#     }
#     Write-Host ("  {0,-20} {1,4}ms" -f 'TOTAL', $Script:ProfileStart.ElapsedMilliseconds) -ForegroundColor Green
# }
# #endregion

#region Module Path & UTF-8
$env:PSModulePath = 'C:\Users\Lance\Dev\Scripts\powershell' + [IO.Path]::PathSeparator + $env:PSModulePath
[Console]::InputEncoding = [Console]::OutputEncoding = $Global:OutputEncoding = [System.Text.Encoding]::UTF8
$PSDefaultParameterValues['Out-File:Encoding'] = 'utf8'
$PSDefaultParameterValues['*:Encoding'] = 'utf8'
$env:PYTHONIOENCODING = 'utf-8'
# Write-ProfileTiming 'Init'
#endregion

#region ScriptsToolkit
Import-Module ScriptsToolkit -ErrorAction SilentlyContinue
# Write-ProfileTiming 'ScriptsToolkit'
#endregion

#region PSCompletions - Provides unified completion menu
# PSCompletions takes over Tab key via Set-PSReadLineKeyHandler internally
# It uses TabExpansion2 to handle ALL completions (psc add, Register-ArgumentCompleter, carapace, etc.)
# Config: enable_menu=1, enable_menu_enhance=1 (defaults)
Import-Module PSCompletions -ErrorAction SilentlyContinue
# Write-ProfileTiming 'PSCompletions'
#endregion

#region PSReadLine - Basic line editing (Tab handled by PSCompletions)
# PSCompletions menu_enhance uses Set-PSReadLineKeyHandler for Tab
# We only configure prediction and colors here
Set-PSReadLineOption -PredictionSource History
Set-PSReadLineOption -PredictionViewStyle InlineView
Set-PSReadLineOption -Colors @{ "Selection" = "`e[7m" }
# Write-ProfileTiming 'PSReadLine'
#endregion

#region Carapace - Multi-shell completion provider
# Carapace registers completers via Register-ArgumentCompleter
# PSCompletions menu displays these completions
$env:CARAPACE_BRIDGES = 'zsh,fish,bash,inshellisense'
carapace _carapace | Out-String | Invoke-Expression
# Write-ProfileTiming 'Carapace'
#endregion

#region Argc - Additional completions (dotnet, whisper-ctranslate2)
if (Get-Command -Name argc -CommandType Application -ErrorAction SilentlyContinue) {
    $argcCmds = @('dotnet', 'whisper-ctranslate2') | Where-Object {
        Get-Command -Name $_ -CommandType Application -ErrorAction SilentlyContinue
    }
    if ($argcCmds) {
        argc --argc-completions powershell @argcCmds | Out-String | Invoke-Expression
    }
}
# Write-ProfileTiming 'Argc'
#endregion

#region PSFzf - Lazy loaded fuzzy finder (Ctrl+R history, Ctrl+T files)
# PSFzf is NOT imported at startup - only when Ctrl+R or Ctrl+T is pressed
# This saves ~400ms startup time while preserving fuzzy search capability
Set-PSReadLineKeyHandler -Key 'Ctrl+r' -BriefDescription 'FzfHistory' -ScriptBlock {
    if (-not (Get-Module PSFzf)) {
        Import-Module PSFzf -ErrorAction SilentlyContinue
        Set-PsFzfOption -PSReadlineChordProvider 'Ctrl+t' -PSReadlineChordReverseHistory 'Ctrl+r'
    }
    Invoke-FzfPsReadlineHandlerHistory
}
Set-PSReadLineKeyHandler -Key 'Ctrl+t' -BriefDescription 'FzfProvider' -ScriptBlock {
    if (-not (Get-Module PSFzf)) {
        Import-Module PSFzf -ErrorAction SilentlyContinue
        Set-PsFzfOption -PSReadlineChordProvider 'Ctrl+t' -PSReadlineChordReverseHistory 'Ctrl+r'
    }
    Invoke-FzfPsReadlineHandlerProvider
}
# Write-ProfileTiming 'PSFzf-Lazy'
#endregion

#region Summary
# $Script:ProfileStart.Stop()
# Show-ProfileSummary
# Write-Host "(Tab=psc menu, Ctrl+R=fzf history, Ctrl+T=fzf files)" -ForegroundColor DarkGray
#endregion
