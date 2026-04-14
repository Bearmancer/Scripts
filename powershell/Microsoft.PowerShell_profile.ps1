Set-StrictMode -Version Latest

function Write-Log {
    param([string]$Message, [string]$Level = 'INFO')
    $ts   = Get-Date -Format 'HH:mm'
    $color = switch ($Level) {
        'INFO'  { 'Cyan'   }
        'OK'    { 'Green'  }
        'WARN'  { 'Yellow' }
        'ERROR' { 'Red'    }
        default { 'White'  }
    }
    Write-Host "[$ts] $Message" -ForegroundColor $color
}
