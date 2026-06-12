# PowerShell Scripts Current State Legend

## PowerShell Types
- `[string]`: A sequence of characters.
- `[int]`: A 32-bit signed integer.
- `[switch]`: A boolean value that is `true` if the parameter is present, `false` otherwise.
- `[string[]]`: An array of strings.
- `[object]`: A generic .NET object.
- `[System.IO.DirectoryInfo]`: A .NET object representing directory information.

## Parameter Attributes
- `[CmdletBinding()]`: Enables common parameters (e.g., `-Verbose`, `-ErrorAction`).
- `[ValidateSet('...')]`: Restricts the parameter value to a specific set of allowed strings.
- `[Parameter(Mandatory=$true)]`: Indicates that the parameter must be provided for the function to run.

## Flowchart Symbols
- **Rectangle (Pink)**: A PowerShell script file (`.ps1`).
- **Rounded Rectangle (Blue)**: A function defined within a script.
- **Parallelogram (Green)**: Data, external tools, or output files.
- **Arrow**: Represents a call, dependency, or data flow.

## Script Organization Overview
The current organization consists of:
- **Root Scripts**: High-level utility scripts (`Install-Env.ps1`, `Export-AgentChats.ps1`).
- **ScriptsToolkit**: A directory containing specialized utility scripts and a shared data file (`ScriptsToolkit.Data.ps1`).
- **Profile Integration**: A registration script that modifies the user's PowerShell profile to enable global access to specific functions.
