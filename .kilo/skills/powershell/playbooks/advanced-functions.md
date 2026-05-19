# PowerShell — Advanced Function Authoring

## Pipeline-aware function with `ShouldProcess`

```powershell
function Get-UserData {
    [CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'Low')]
    param(
        [Parameter(Mandatory, ValueFromPipeline, Position = 0,
                   HelpMessage = 'User ID to retrieve')]
        [ValidateRange(1, [int]::MaxValue)]
        [int[]]$UserId,

        [switch]$IncludeDetails,

        [ValidateSet('Table', 'List', 'JSON')]
        [string]$Format = 'Table'
    )

    begin {
        Write-Verbose "Format=$Format"
        $users = @()
    }

    process {
        foreach ($id in $UserId) {
            if ($PSCmdlet.ShouldProcess("User $id", 'Retrieve data')) {
                $record = @{ Id = $id; Name = "User_$id"; Email = "user$id@example.com" }

                if ($IncludeDetails) {
                    $record += @{ Department = 'IT'; Active = $true;
                                  CreatedDate = (Get-Date).AddDays(-30) }
                }

                $users += [PSCustomObject]$record
            }
        }
    }

    end {
        switch ($Format) {
            'Table' { $users | Format-Table -AutoSize }
            'List'  { $users | Format-List }
            'JSON'  { $users | ConvertTo-Json }
        }
        Write-Verbose "Retrieved $($users.Count) user(s)"
    }
}
```

**Block responsibilities:**

- `begin` — one-time setup; initialise accumulators.
- `process` — called once per pipeline object; never accumulate without intent.
- `end` — aggregate output; emit once.
- `ShouldProcess` — required whenever the function modifies state, even read-with-side-effects.

---

## Retry loop with `ValidateScript`

```powershell
function Invoke-DataProcessing {
    [CmdletBinding()]
    [OutputType([System.Object])]
    param(
        [Parameter(Mandatory)]
        [ValidateScript({
            if (-not (Test-Path $_)) { throw "Path not found: $_" }
            $true
        })]
        [string]$InputPath,

        [string]$OutputPath,
        [int]$MaxRetries = 3
    )

    $ErrorActionPreference = 'Stop'

    if (-not (Test-Path $InputPath -PathType Leaf)) {
        throw 'Input must be a file, not a directory'
    }

    $retryCount = 0
    while ($retryCount -lt $MaxRetries) {
        try {
            $data   = Get-Content $InputPath
            $result = @{ Success = $true; ItemCount = $data.Count; ProcessedAt = Get-Date }

            if ($OutputPath) {
                $result | Export-Clixml -Path $OutputPath
                Write-Verbose "Output written to: $OutputPath"
            }

            return $result
        }
        catch {
            $retryCount++
            Write-Warning "Attempt $retryCount/$MaxRetries failed: $_"
            if ($retryCount -ge $MaxRetries) { throw "Failed after $MaxRetries attempts" }
            Start-Sleep -Seconds 2
        }
    }
}
```

**Constraints:**

- Set `$ErrorActionPreference = 'Stop'` at function scope so all terminating errors flow to `catch`.
- `ValidateScript` throws with a custom message; the block must return `$true` on success.
- Increment retry counter before the guard — prevents off-by-one on the final throw.

---

## Authoring checklist

```
[ ] [CmdletBinding()] declared
[ ] SupportsShouldProcess present when function mutates state
[ ] All mandatory params have HelpMessage
[ ] ValidateSet / ValidateRange / ValidateScript on unconstrained inputs
[ ] begin/process/end blocks when ValueFromPipeline is used
[ ] OutputType declared for non-void functions
[ ] Verbose messages at entry, key decision points, and exit
[ ] ErrorAction scoped to function, not global
```
