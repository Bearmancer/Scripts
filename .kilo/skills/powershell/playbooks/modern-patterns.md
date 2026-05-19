# Modern PowerShell Playbook

## Control Flow (v7+)
```powershell
# Ternary
$status = $age -ge 18 ? "Adult" : "Minor"

# Null-coalescing
$name = $inputName ?? "Guest"
$config ??= @{ Enabled = $true }

# Pipeline chains
mkdir "dist" && cp "src/*.js" "dist/" || Write-Error "Build failed"
```

## Classes & Enums
```powershell
enum TaskStatus { Pending; Running; Done }

class BuildTask {
    [string]$Name
    [TaskStatus]$Status
    [datetime]$Time = [datetime]::Now

    BuildTask([string]$name) {
        $this.Name = $name
    }

    [void] Complete() {
        $this.Status = [TaskStatus]::Done
    }
}
```

## Advanced Objects & Splatting
```powershell
$params = @{
    Path    = 'C:\temp'
    Filter  = '*.log'
    Recurse = $true
}
Get-ChildItem @params

# Fast objects
$results = foreach ($i in 1..10) {
    [PSCustomObject]@{
        Id   = $i
        Time = [datetime]::Now
    }
}
```

## Script Execution
- **EncodedCommand:** Use for passing complex blocks to `powershell.exe`.
- **Progress:** Use `$ProgressPreference = 'SilentlyContinue'` for faster non-interactive scripts.
