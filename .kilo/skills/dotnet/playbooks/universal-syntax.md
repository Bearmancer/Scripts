# Universal .NET Syntax & Logic

## Shared Operators
These operators follow the same logic across C# and PowerShell (v7+).

### Ternary Operator (`? :`)
Short-form conditional expression.
- **C#:** `var x = condition ? trueVal : falseVal;`
- **PWSH:** `$x = $condition ? $trueVal : $falseVal`

### Null-Coalescing Operator (`??`)
Returns the left-hand operand if it is not null; otherwise, it returns the right-hand operand.
- **C#:** `var x = y ?? "default";`
- **PWSH:** `$x = $y ?? 'default'`

### Null-Coalescing Assignment (`??=`)
Assigns the value of the right-hand operand to the left-hand operand only if the left-hand operand evaluates to null.
- **C#:** `x ??= new List<string>();`
- **PWSH:** `$x ??= @()`

## Resource Management
### Disposable Patterns
- **C#:** `using var x = ...;` or `using (var x = ...) { ... }`
- **PWSH:** `try { $x = ... } finally { if ($x) { $x.Dispose() } }` (Manual disposal preferred for shell stability).

## Serialization Consistency
Always prefer `System.Text.Json` over `Newtonsoft.Json` or `ConvertFrom-Json` (unless wrapper logic is needed).
- **C#:** `JsonSerializer.Serialize(obj)`
- **PWSH:** `[System.Text.Json.JsonSerializer]::Serialize($obj)`
