# ConEmu ScriptsToolkit

> **Environment:** ConEmu (Windows Console Emulator)
> **Language:** ConEmu Tasks (Pseudo-XML/GUI) & Batch Wrapper
> **Philosophy:** Define "Tasks" for recurring workflows that can handle multi-tab/console setups.

---

## Complete Implementation (`ConEmuTasks.xml` Snippet)

ConEmu tasks are usually defined in the `ConEmu.xml` configuration file. Below is the XML block representing the implementation of the toolkit commands as accessible Tasks.

> **Instruction:** Paste these blocks into your `ConEmu.xml` under `<key name="Tasks">` OR use the Settings GUI (Win+Alt+P) -> Startup -> Tasks to add them manually.

```xml
<key name="Tasks">
    <!-- 
        Task: Sync:All
        Description: Run all sync jobs in sequence
        Feature: -new_console:c splits the output; /k keeps window open
    -->
    <value name="{Sync:All}" type="string" data='cmd /k "dotnet run --project C:\Users\Lance\Dev\csharp -- sync yt && dotnet run --project C:\Users\Lance\Dev\csharp -- sync lastfm"'/>

    <!--
        Task: Toolkit:Menu
        Description: Show the Toolkit Help using Python
        Feature: reusing python directly
    -->
    <value name="{Toolkit:Menu}" type="string" data='python C:\Users\Lance\Dev\python\toolkit\cli.py --help -new_console:p'/>

    <!--
        Task: Shells:Matrix
        Description: Open 4 shells in a 2x2 grid (Powershell, Bash, Fish, Nu)
        Feature: -new_console:sV (split vertical) / sH (split horizontal)
    -->
    <value name="{Shells:Matrix}" type="string" data='pwsh -NoLogo -new_console:d:C:\Users\Lance\Dev
    
    ; git-bash -new_console:sV
    
    ; fish -new_console:sH
    
    ; nu -new_console:sH'/>
</key>
```

## Batch Wrapper (`tk.bat`)

Since ConEmu is a host, not a language, the primary way to "script" is via Batch files that invoke the ConEmu macro processor `ConEmuC.exe`.

Save this as `C:\Users\Lance\Dev\tk.bat` and ensure it's in your PATH.

```batch
@echo off
:: ScriptsToolkit - ConEmu Wrapper
:: Usage: tk [command]

setlocal EnableDelayedExpansion

:: Configuration
set "REPO=C:\Users\Lance\Dev"
set "PY_TOOLKIT=%REPO%\python\toolkit\cli.py"

:: Dispatcher
if "%1"=="" goto help
if "%1"=="dirs" goto dirs
if "%1"=="sync" goto sync
if "%1"=="splittest" goto splittest
goto unknown

:help
    echo.
    echo  [96mConEmu ScriptsToolkit [0m
    echo  =======================
    echo  tk dirs [path]   :: List dirs (Python)
    echo  tk sync          :: Run syncs (Dotnet)
    echo  tk splittest     :: Open 4-pane grid
    echo.
    exit /b 0

:dirs
    :: %~2 removes quotes from the second argument
    set "target=%~2"
    if "%target%"=="" set "target=."
    python "%PY_TOOLKIT%" filesystem tree --directory "%target%"
    exit /b %ERRORLEVEL%

:sync
    :: Example of controlling ConEmu tabs from batch
    :: -new_console ensures it opens in a new tab if run inside ConEmu
    echo Starting Sync...
    start "YouTube Sync" /WAIT cmd /c "dotnet run --project %REPO%\csharp -- sync yt"
    start "LastFM Sync" /WAIT cmd /c "dotnet run --project %REPO%\csharp -- sync lastfm"
    echo Done.
    exit /b 0

:splittest
    :: This uses ConEmuC to script the GUI itself
    :: -GuiMacro: send commands to the active ConEmu window
    if not defined ConEmuBuild (
        echo Error: distinct ConEmu session not found.
        exit /b 1
    )
    
    :: Split current pane vertically (50%)
    ConEmuC -GuiMacro Split(2, 50, 0)
    :: Create a new horizontal split in the new pane
    ConEmuC -GuiMacro Split(2, 50, 1)
    exit /b 0

:unknown
    echo Unknown command: %1
    exit /b 1
```

---

## Symbol & Feature Glossary (ConEmu / Batch)

### ConEmu Switches (The `-new_console` magic)
These switches are intercepted by ConEmu's hook DLL when executing a command. They are **NOT** passed to the payload application.

| Symbol         | Usage                   | Detailed Explanation                                                                      |
| -------------- | ----------------------- | ----------------------------------------------------------------------------------------- |
| `-new_console` | `cmd -new_console`      | Tells ConEmu to run this command in a new Tab.                                            |
| `:sV`          | `-new_console:sV`       | **Split Vertical**. Opens the new tab as a pane to the right of the current one.          |
| `:sH`          | `-new_console:sH`       | **Split Horizontal**. Opens the new tab as a pane below the current one.                  |
| `:c`           | `-new_console:c`        | **Close**. Automatically close the tab when the command finishes (or `n` for "No close"). |
| `:d:path`      | `-new_console:d:C:\Dev` | Sets the **Directory** startup path for the new console.                                  |
| `:p`           | `-new_console:p`        | run as **Administrator** (Elevated privileges).                                           |

### Batch Scripting Chars
| Symbol         | Usage                             | Detailed Explanation                                                                                             |
| -------------- | --------------------------------- | ---------------------------------------------------------------------------------------------------------------- |
| `@echo off`    | Top of file                       | Prevents command echoing, keeping output clean.                                                                  |
| `::`           | `:: Comment`                      | Modern batch comment style (actually an invalid label). Faster and cleaner than `REM`.                           |
| `%~1`          | `set v=%~1`                       | Argument expansion. `1` is the first arg. `~` strips surrounding quotes. Essential for path handling.            |
| `goto`         | `goto label`                      | Jumps to a label defined by `:label`. The primary control flow in Batch.                                         |
| `call`         | `call :func`                      | Calls a label like a function/subroutine. returns when `exit /b` is hit.                                         |
| `start`        | `start "" /WAIT cmd`              | Launches a separate process. `/WAIT` pauses the script until that process ends.                                  |
| `%ERRORLEVEL%` | `exit /b %ERRORLEVEL%`            | Holds the exit code of the last run command. `0` usually means success.                                          |
| `setlocal`     | `setlocal EnableDelayedExpansion` | Limits variable changes to this script execution and enables `!var!` syntax for dynamic evaluation inside loops. |
