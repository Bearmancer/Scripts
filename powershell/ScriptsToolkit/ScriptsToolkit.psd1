@{
	RootModule           = 'ScriptsToolkit.psm1'
	ModuleVersion        = '1.0.0'
	GUID                 = '8f3a4b5c-6d7e-4f1a-9b2c-3d4e5f6a7b8c'
	Author               = 'Lance'
	Description          = 'Personal PowerShell scripts toolkit'
	PowerShellVersion    = '7.0'
	CompatiblePSEditions = @('Core')
	FunctionsToExport    = @('Invoke-Whisper', 'Get-MediaFiles')
	AliasesToExport      = @('whisp')
	CmdletsToExport      = @()
	VariablesToExport    = @()
}
