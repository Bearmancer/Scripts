# PowerShell Scripts Final State Legend

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
- **Rectangle (Pink)**: A PowerShell Module (`.psm1` / `.psd1`).
- **Rectangle (Light Pink)**: A standalone PowerShell script (`.ps1`).
- **Rounded Rectangle (Blue)**: A function exported by a module or defined in a script.
- **Parallelogram (Green)**: Data, external tools, or output files.
- **Arrow**: Represents an import, call, or data flow.

## Script Organization Overview
The final state transitions to a modular architecture:
- **Modules**: Logic is encapsulated in `ScriptsToolkit` and `AgentChats` modules, providing a clean API and better scope management.
- **Managed Profile**: The PowerShell profile becomes a simple loader that imports the necessary modules.
- **Standalone Scripts**: Setup scripts like `Install-Env.ps1` remain standalone but leverage the `ScriptsToolkit` module for shared utility functions.
