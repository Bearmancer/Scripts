Set-StrictMode -Version Latest

[Console]::InputEncoding = [Console]::OutputEncoding = $Global:OutputEncoding = [System.Text.Encoding]::UTF8
$env:PYTHONIOENCODING = 'utf-8'
$env:PYTHONWARNINGS = 'ignore'

# carapace _carapace | Out-String | Invoke-Expression

# Import-Module $PSScriptRoot\ScriptsToolkit\ScriptsToolkit.psd1

# Import-Module PSCompletions
# Set-PSReadLineOption -PredictionViewStyle ListView