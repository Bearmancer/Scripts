# PowerShell — Error Handling Patterns

## Typed catch blocks

Order: specific exception types first; bare `catch` last.

```powershell
try {
    $content = Get-Content -Path $Path -ErrorAction Stop
}
catch [System.Management.Automation.ItemNotFoundException] {
    Write-Error "File not found: $Path"; return $null
}
catch [System.UnauthorizedAccessException] {
    Write-Error "Access denied: $Path";  return $null
}
catch {
    Write-Error "Unexpected error: $_";  return $null
}
```

---

## `finally` for resource release

```powershell
$resource = $null
try {
    $resource = Acquire-Resource $Name
    # use resource
}
catch {
    Write-Error "Operation failed: $_"
}
finally {
    if ($resource) { $resource.State = 'released' }   # always runs
}
```

---

## Custom `ErrorRecord` for terminating validation errors

Use `$PSCmdlet.ThrowTerminatingError` instead of `throw` — preserves the pipeline and emits a structured error record.

```powershell
if ($Value -lt 0) {
    $errorRecord = [System.Management.Automation.ErrorRecord]::new(
        [System.ArgumentException]'Value must be non-negative',
        'NegativeValue',
        [System.Management.Automation.ErrorCategory]::InvalidArgument,
        $Value
    )
    $PSCmdlet.ThrowTerminatingError($errorRecord)
}
```

---

## Structured error aggregation

Never surface partial failure as a thrown exception when the caller needs the success set.

```powershell
$errors    = @()
$successes = @()

foreach ($item in $Items) {
    try {
        # process $item
        $successes += $item
    }
    catch {
        $errors += @{ Item = $item; Error = $_.Exception.Message }
    }
}

if ($errors.Count -gt 0) {
    $errors | ForEach-Object { Write-Warning "  $($_.Item): $($_.Error)" }
}

return @{ Successes = $successes; Errors = $errors }
```
