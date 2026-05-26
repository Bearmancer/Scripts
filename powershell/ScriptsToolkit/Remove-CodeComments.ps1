param([string]$FilePath)

if (-not (Test-Path $FilePath)) {
  Write-Error "File not found: $FilePath"
  exit 1
}

$ext = [System.IO.Path]::GetExtension($FilePath).ToLower()

$langs = @{
  '.cs'   = @{
    Line       = @('//')
    BlockOpen  = '/*'   ; BlockClose = '*/'
    WholeLine  = $false ; SkipInline = $false
    Str        = @('"""', '"', "@'", "'")
    Preserve   = @('///')
  }
  '.py'   = @{
    Line       = @('#')
    BlockOpen  = $null  ; BlockClose = $null
    WholeLine  = $false ; SkipInline = $false
    Str        = @('"""', "'''", '"', "'")
    Preserve   = @('#!/', '# -*-', '# type:', '# noqa', '# pragma:', '# region', '# endregion')
  }
  '.cmd'  = @{
    Line       = @()
    BlockOpen  = $null  ; BlockClose = $null
    WholeLine  = $false ; SkipInline = $true
    Str        = @('"')
    Preserve   = @()
  }
  '.bat'  = @{
    Line       = @()
    BlockOpen  = $null  ; BlockClose = $null
    WholeLine  = $false ; SkipInline = $true
    Str        = @('"')
    Preserve   = @()
  }
  '.ps1'  = @{
    Line       = @('#')
    BlockOpen  = '<#'   ; BlockClose = '#>'
    WholeLine  = $false ; SkipInline = $false
    Str        = @('"', "'")
    Preserve   = @('#Requires', '#!/')
  }
  '.psm1' = @{
    Line       = @('#')
    BlockOpen  = '<#'   ; BlockClose = '#>'
    WholeLine  = $false ; SkipInline = $false
    Str        = @('"', "'")
    Preserve   = @('#Requires')
  }
  '.psd1' = @{
    Line       = @('#')
    BlockOpen  = '<#'   ; BlockClose = '#>'
    WholeLine  = $false ; SkipInline = $false
    Str        = @('"', "'")
    Preserve   = @('#Requires')
  }
  '.sh'   = @{
    Line       = @('#')
    BlockOpen  = $null  ; BlockClose = $null
    WholeLine  = $false ; SkipInline = $false
    Str        = @('"', "'")
    Preserve   = @('#!/')
  }
  '.bash' = @{
    Line       = @('#')
    BlockOpen  = $null  ; BlockClose = $null
    WholeLine  = $false ; SkipInline = $false
    Str        = @('"', "'")
    Preserve   = @('#!/')
  }
  '.zsh'  = @{
    Line       = @('#')
    BlockOpen  = $null  ; BlockClose = $null
    WholeLine  = $false ; SkipInline = $false
    Str        = @('"', "'")
    Preserve   = @('#!/')
  }
  '.js'   = @{
    Line       = @('//')
    BlockOpen  = '/*'   ; BlockClose = '*/'
    WholeLine  = $false ; SkipInline = $false
    Str        = @('"', "'", '`')
    Preserve   = @()
  }
  '.ts'   = @{
    Line       = @('//')
    BlockOpen  = '/*'   ; BlockClose = '*/'
    WholeLine  = $false ; SkipInline = $false
    Str        = @('"', "'", '`')
    Preserve   = @('///')
  }
  '.tsx'  = @{
    Line       = @('//')
    BlockOpen  = '/*'   ; BlockClose = '*/'
    WholeLine  = $false ; SkipInline = $false
    Str        = @('"', "'", '`')
    Preserve   = @()
  }
  '.html' = @{
    Line       = @()
    BlockOpen  = '<!--'  ; BlockClose = '-->'
    WholeLine  = $false  ; SkipInline = $false
    Str        = @('"', "'")
    Preserve   = @()
  }
  '.css'  = @{
    Line       = @()
    BlockOpen  = '/*'   ; BlockClose = '*/'
    WholeLine  = $false ; SkipInline = $false
    Str        = @('"', "'")
    Preserve   = @()
  }
  '.scss' = @{
    Line       = @('//')
    BlockOpen  = '/*'   ; BlockClose = '*/'
    WholeLine  = $false ; SkipInline = $false
    Str        = @('"', "'")
    Preserve   = @()
  }
  '.java' = @{
    Line       = @('//')
    BlockOpen  = '/*'   ; BlockClose = '*/'
    WholeLine  = $false ; SkipInline = $false
    Str        = @('"', "'")
    Preserve   = @()
  }
}

if (-not $langs.ContainsKey($ext)) {
  Write-Host "Unsupported file type: $ext"
  exit 0
}

$lang = $langs[$ext]

function Remove-TrailingComment {
  param(
    [string]   $Line,
    [string[]] $Tokens,
    [string[]] $StrDelims
  )
  if (-not $Tokens -or $Tokens.Count -eq 0) { return $Line }
  
  $i      = 0
  $inStr  = $false
  $strEnd = ''
  
  while ($i -lt $Line.Length) {
    if ($inStr) {
      if ($Line[$i] -eq '\' -and ($i + 1) -lt $Line.Length) { $i += 2; continue }
      if ($Line.Substring($i).StartsWith($strEnd)) {
        $inStr = $false
        $i    += $strEnd.Length
        continue
      }
    }
    else {
      $opened = $false
      foreach ($d in ($StrDelims | Sort-Object Length -Descending)) {
        if ($Line.Substring($i).StartsWith($d)) {
          $inStr  = $true
          $strEnd = $d
          $i     += $d.Length
          $opened = $true
          break
        }
      }
      if (-not $opened) {
        foreach ($tok in ($Tokens | Sort-Object Length -Descending)) {
          if ($Line.Substring($i).StartsWith($tok)) {
            return $Line.Substring(0, $i).TrimEnd()
          }
        }
      }
    }
    if (-not $inStr) { $i++ }
  }
  return $Line
}

$lines      = Get-Content -Path $FilePath
$out        = [System.Collections.Generic.List[string]]::new()
$inBlock    = $false
$blockOpen  = $lang.BlockOpen
$blockClose = $lang.BlockClose

foreach ($rawLine in $lines) {
  $line = $rawLine
  
  if ($blockOpen) {
    if ($inBlock) {
      $ci = $line.IndexOf($blockClose)
      if ($ci -ge 0) {
        $inBlock = $false
        $line    = $line.Substring($ci + $blockClose.Length).TrimStart()
        if ($line.Trim() -eq '') { continue }
      }
      else { continue }
    }
    
    $rebuilt = ''
    $rest    = $line
    while ($rest.Length -gt 0 -and -not $inBlock) {
      $oi = $rest.IndexOf($blockOpen)
      if ($oi -lt 0) { $rebuilt += $rest; break }
      $rebuilt += $rest.Substring(0, $oi)
      $rest     = $rest.Substring($oi + $blockOpen.Length)
      $ci       = $rest.IndexOf($blockClose)
      if ($ci -ge 0) {
        $rest = $rest.Substring($ci + $blockClose.Length).TrimStart()
      }
      else {
        $inBlock = $true
        $rest    = ''
      }
    }
    $line = $rebuilt
  }
  
  if ($rawLine.Trim() -eq '') {
    if (-not $inBlock) { $out.Add('') }
    continue
  }
  
  $preserved = $false
  foreach ($p in $lang.Preserve) {
    if ($line.TrimStart().StartsWith($p)) { $preserved = $true; break }
  }
  if ($preserved) { $out.Add($rawLine); continue }
  
  if ($lang.SkipInline) {
    $t = $line.TrimStart()
    if ($t -imatch '^@?REM(\s|$)' -or $t.StartsWith('::')) { continue }
    if ($line.Trim() -ne '') { $out.Add($line) }
    continue
  }
  
  $line = Remove-TrailingComment -Line $line -Tokens $lang.Line -StrDelims $lang.Str
  
  if ($line.Trim() -eq '') { continue }
  $out.Add($line)
}

$out | Set-Content -Path $FilePath -Encoding UTF8
Write-Host "Comments removed from: $FilePath"
