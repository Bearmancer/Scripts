# Description

-----------------------------

# PowerShell Cline Hook Best Practices

## §as-hashtable: Always use ConvertFrom-Json -AsHashtable

* Without -AsHashtable, ConvertFrom-Json returns PSCustomObject in PS 6+
* PSCustomObject requires fragile, lossy conversion to \[hashtable\]
* The buggy conversion `$obj.PSObject.Properties | ForEach { @{} } | Select -First 1` drops all but 1st property
* Nested arrays/objects leak ArrayList enumerators that crash on \[hashtable\] type casts

## §no-type-constraint: Never use \[hashtable\] on param()

* param(\[hashtable\]$Input) fires at PARAMETER BINDING TIME — before function body
* A non-hashtable argument crashes OUTSIDE any try/catch
* Use soft guards: param($Input) + if ($Input -isnot \[hashtable\]) { $Input = @{} }

## §escape-order: In manual JSON building

* Escape double-quote FIRST, then backslash
* Correct: $msg -replace '"', '"' -replace '\\', '\\'
* Wrong: $msg -replace '\\', '\\' -replace '"', '"' (doubles escapes)

## §single-hardening: One input boundary

* Read-HookStdin is the only function touching stdin
* After it returns \[hashtable\] (via -AsHashtable), all downstream functions are safe

## §empty-errorMessage: For AI auto-continue

* Non-empty errorMessage triggers Cline "Proceed anyway?" UX dialog
* AI reads stderr independently
* Hook crashes: log full error+stack to stderr, emit {"cancel":false,"errorMessage":""}
* errorMessage only non-empty when cancel=true

## Array handling in stdin

* Cline may send JSON arrays of events
* Read-HookStdin: if ($parsed -is \[System.Collections.IList\]) { $parsed = $parsed\[0\] }

## #Requires -Version 7.0 vs ?? operator

* PowerShell 7's ?? null-coalescing operator is safe with #Requires -Version 7.0
* PSScriptAnalyzer runs PS5 parser; 'Unexpected token ??' warnings are false positives

## Downstream concern files

* Concern files called via `& $concernFile -HookInput $Input ...` receive arguments by named parameter
* \[hashtable\] type constraints in concern param() blocks crash at call site, same as common.ps1
* All concern files must use $paramName (no type) + soft -isnot guards