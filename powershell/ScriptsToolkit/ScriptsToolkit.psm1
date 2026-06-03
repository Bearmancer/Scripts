#Requires -Version 7

using namespace System.IO

$Script:MediaExtensions = @('.mp4', '.mkv', '.avi', '.mp3', '.flac', '.wav', '.webm', '.m4a', '.opus', '.ogg')

function Get-MediaFiles {
	<#
	.SYNOPSIS
		Enumerate media files in a directory.
	.PARAMETER Directory
		Directory to search.
	.PARAMETER Recurse
		Recurse into subdirectories.
	.EXAMPLE
		Get-MediaFiles -Directory (Get-Item ./videos) -Recurse
	#>
	[CmdletBinding()]
	param(
		[Parameter(Mandatory)]
		[DirectoryInfo]$Directory,

		[switch]$Recurse
	)
	Get-ChildItem $Directory -Recurse:$Recurse -File |
		Where-Object Extension -In $Script:MediaExtensions
}

function Invoke-Whisper {
	<#
	.SYNOPSIS
		Transcribe media files via whisper-ctranslate2 (run through uvx).
	.DESCRIPTION
		Walks a file or directory, skips files that already have a sibling .srt,
		and writes subtitles next to the source. Defaults to English and the
		distil-large-v3.5 model.
	.PARAMETER Path
		Media file or directory of media files. Defaults to the current directory.
	.PARAMETER Recurse
		Recurse into subdirectories when Path is a directory.
	.PARAMETER Language
		Whisper language code. Default: eng.
	.PARAMETER Model
		Whisper model name. Default: distil-large-v3.5.
	.EXAMPLE
		whisp ./interview.mp4
	.EXAMPLE
		whisp ./Spoken\ Word -Recurse
	.EXAMPLE
		whisp ./podcast.mp3 -Language en -Model large-v3
	.EXAMPLE
		whisp ./episodes -WhatIf
	.NOTES
		Requires uv (https://docs.astral.sh/uv/) and ffmpeg on PATH.
		'whisp' is the alias.
	#>
	[CmdletBinding(SupportsShouldProcess)]
	[Alias('whisp')]
	param(
		[Parameter(Position = 0)]
		[string]$Path = $PWD.ProviderPath,

		[switch]$Recurse,

		[string]$Language = 'eng',

		[string]$Model = 'distil-large-v3.5'
	)

	$uvx = Get-Command uvx -ErrorAction SilentlyContinue
	if (-not $uvx) {
		Write-Error 'uvx not found. Install: https://docs.astral.sh/uv/'
		return
	}

	$leaf = Get-Item -LiteralPath $Path -ErrorAction SilentlyContinue
	if (-not $leaf) {
		Write-Error "Path not found: $Path"
		return
	}

	if ($leaf.Attributes.HasFlag([FileAttributes]::Directory)) {
		$files = @(Get-MediaFiles -Directory $leaf -Recurse:$Recurse)
	}
	else {
		if ($Script:MediaExtensions -notcontains $leaf.Extension.ToLower()) {
			Write-Error "Unsupported file type: $($leaf.Extension)"
			return
		}
		$files = @($leaf)
	}

	if ($files.Count -eq 0) {
		Write-Warning 'No media files found.'
		return
	}

	$total   = $files.Count
	$i       = 0
	$skipped = 0
	$failed  = 0
	$ok      = 0

	Write-Host "Transcribing $total file(s) | lang=$Language model=$Model | uvx whisper-ctranslate2" -ForegroundColor Cyan
	Write-Host ''

	$splat = [ordered]@{
		language      = $Language
		model         = $Model
		output_format = 'srt'
		verbose       = 'False'
	}

	foreach ($f in $files) {
		$i++
		$srt = [Path]::ChangeExtension($f.FullName, '.srt')

		if (Test-Path -LiteralPath $srt) {
			Write-Host "[$i/$total] SKIP (srt exists): $($f.Name)" -ForegroundColor DarkGray
			$skipped++
			continue
		}

		if (-not $PSCmdlet.ShouldProcess($f.FullName, 'whisper-ctranslate2')) { continue }

		Write-Host "[$i/$total] $($f.Name)" -ForegroundColor Cyan

		$splat.output_dir = $f.DirectoryName
		& $uvx.Path whisper-ctranslate2 @splat $f.FullName

		if ($LASTEXITCODE -ne 0) {
			Write-Warning "  FAILED (exit $LASTEXITCODE): $($f.Name)"
			$failed++
			continue
		}

		if (Test-Path -LiteralPath $srt) {
			Write-Host "  OK -> $srt" -ForegroundColor Green
			$ok++
		}
		else {
			Write-Warning "  No SRT produced: $($f.Name)"
			$failed++
		}
	}

	Write-Host ''
	Write-Host "Done. OK: $ok | Skipped: $skipped | Failed: $failed" -ForegroundColor Cyan
}
