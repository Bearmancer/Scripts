Set-StrictMode -Version Latest

function Merge-Files {
	[CmdletBinding()]
	param(
		[Parameter(Position = 0)]
		[string]$Format = 'md',

		[Parameter(Position = 1)]
		[string]$Path = $PWD.ProviderPath,

		[switch]$NoRecurse
	)

	# Fully qualified .NET types for StrictMode compatibility
	$script:SearchAllDir = [System.IO.SearchOption]::AllDirectories
	$script:SearchTopDir = [System.IO.SearchOption]::TopDirectoryOnly
	$script:DesktopPath = [System.Environment]::GetFolderPath('Desktop')
	$script:SepPre = "`n`n================================================================================`nFILE: "
	$script:SepPost = "`n================================================================================`n`n"

	# Sanitize extension: handles 'md', '.md', '*.md'
	$ext = $Format.TrimStart('*').TrimStart('.')

	# Validate path exists and is accessible
	if (-not (Test-Path -Path $Path -PathType Container -ErrorAction Stop)) {
		Write-Error "Path does not exist or is not a directory: $Path"
		return
	}

	$opt = if ($NoRecurse) {
		$script:SearchTopDir
	}
	else {
		$script:SearchAllDir
	}

	# GetFiles can return $null on error — handle gracefully under StrictMode
	$files = [System.IO.Directory]::GetFiles($Path, "*.$ext", $opt)
	if ($null -eq $files -or $files.Length -eq 0) {
		Write-Warning 'No matching files found.'
		return
	}

	$stamp = [System.DateTime]::Now.ToString('yyyyMMdd-HHmmss')
	$dir = [System.IO.Path]::GetFileName($Path.TrimEnd('\', '/'))
	$out = [System.IO.Path]::Combine($script:DesktopPath, "${dir}_${stamp}.$ext")

	# Initialize to $null for safe disposal in finally block
	$writer = $null
	try {
		$writer = [System.IO.StreamWriter]::new($out, $false,[System.Text.UTF8Encoding]::new($false), 65536)

		foreach ($f in $files) {
			$writer.Write($script:SepPre)
			$writer.Write($f)
			$writer.Write($script:SepPost)
			$writer.Write([System.IO.File]::ReadAllText($f))
		}
	}
	catch {
		# $f may not be defined if error occurred before/during loop — safe fallback
		$failedFile = if ($null -ne (Get-Variable f -ErrorAction SilentlyContinue)) {
			$f
		}
		else {
			'unknown'
		}
		Write-Error "Merge failed at '${failedFile}': $_"
	}
	finally {
		# Safe disposal pattern: StrictMode forbids calling methods on $null
		if ($null -ne $writer) {
			$writer.Dispose()
		}
	}

	Write-Host "Written: $out ($( $files.Length ) files merged)"
}
