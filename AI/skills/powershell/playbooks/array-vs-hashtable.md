# Playbook: Array vs Hash Table Examples

## Creating and Accessing

```powershell
# Array @()
$empty = @()
$items = @(1, 2, 3)
$items[0]       # 1
$items[-1]      # 3 last item
$items[0, 2]    # 1, 3 multiple indices

# Hash table @{}
$empty = @{}
$person = @{ Name = 'Kevin'; Age = 36 }
$person['Name']     # 'Kevin' bracket syntax
$person.Name        # 'Kevin' property syntax
$person.City = 'Austin'  # add property
```

## Adding Items

```powershell
# Array: += creates NEW array expensive
$items = @(1, 2, 3)
$items += 4           # Copies entire array + new element

# Hash table: Add method or property syntax
$map = @{}
$map.Add('key', 'value')
$map.key2 = 'value2'   # Also works
```

## Checking Contents

```powershell
# Array contains
$list = @('red', 'green', 'blue')
$list -contains 'green'           # True

# Hash table contains
$map = @{ Red = '#FF0000'; Green = '#00FF00' }
$map.ContainsKey('Red')           # True
$map.ContainsValue('#00FF00')     # True
```

## `$null` Check Pattern

```powershell
# BAD: -eq on array checks each ELEMENT
if ($array -eq $null) { }

# GOOD: $null on left side
if ($null -eq $array) { }

# BEST: null check then count
if ($null -ne $array -and $array.Count -gt 0) { }
```

## Hash Table Enumeration Gotcha

```powershell
# BAD: BadEnumeration error modifying during iteration
$h.Keys | ForEach-Object { $h[$_] = 'new value' }

# GOOD: Clone keys first
$h.Keys.Clone() | ForEach-Object { $h[$_] = 'new value' }

# GOOD: Convert to array first
@($h.Keys) | ForEach-Object { $h[$_] = 'new value' }
```

## PowerShell 7+ Operators

```powershell
# Null coalescing
$x = $null
$x ?? 'default'          # Returns 'default'
$x ??= 'value'           # Assigns 'value' to $x if null

# Ternary
$condition ? 'true' : 'false'

# Pipeline chain AND
Get-Process Chrome && Stop-Process -Name Chrome

# Pipeline chain OR  
npm install || Remove-Item -Recurse ./node_modules
```
