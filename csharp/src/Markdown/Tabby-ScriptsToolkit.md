# Tabby ScriptsToolkit

> **Environment:** Tabby (formerly Terminus)
> **Language:** YAML (Config) / TypeScript (Plugin API)
> **Philosophy:** Modern, cross-platform terminal with heavy reliance on configuration profiles and electron-based plugins.

---

## Complete Implementation (`config.yaml`)

In Tabby, you define "Profiles" for quick access to your toolkit commands. These appear in the "New Terminal" dropdown.

Add these entries to your Tabby `config.yaml` (Linux: `~/.config/tabby/config.yaml`, Windows: `%APPDATA%\Tabby\config.yaml`).

```yaml
profiles:
  - name: "Toolkit: Help"
    type: local
    command: python
    # Arguments list for the command
    args: 
      - "C:\\Users\\Lance\\Dev\\python\\toolkit\\cli.py"
      - "--help"
    # Keeping the terminal open after exit (wait for keypress)
    pause: true
    icon: fas fa-book
    color: "#00ff00"

  - name: "Toolkit: Sync All"
    type: local
    command: cmd
    args:
      - "/c"
      - "dotnet run --project C:\\Users\\Lance\\Dev\\csharp -- sync yt && dotnet run --project C:\\Users\\Lance\\Dev\\csharp -- sync lastfm && pause"
    icon: fas fa-sync
    color: "#00ccff"
    workingDirectory: "C:\\Users\\Lance\\Dev"

  - name: "Toolkit: Shell Matrix"
    type: split
    # 'split' type allows defining a layout of terminals
    split:
      direction: horizontal
      children:
        - type: profile
          profile: "PowerShell Core"
        - type: split
          direction: vertical
          children:
            - type: profile
              profile: "Git Bash"
            - type: profile
              profile: "Nu Shell"
```

## Complete Implementation (Tabby Plugin - `index.ts`)

If you want to add a top-level menu item "ScriptsToolkit" to the application menu, you write a small plugin.

**File:** `tabby-scripts-toolkit/src/index.ts`

```typescript
import { NgModule } from '@angular/core'
import { CommonModule } from '@angular/common'
import { ToolbarButtonProvider, AppService, ConfigService } from 'tabby-core'

/**
 * Service to inject a button into the toolbar
 */
class ToolkitButtonProvider extends ToolbarButtonProvider {
    constructor(
        private app: AppService,
        private config: ConfigService
    ) { super() }

    provide() {
        return [{
            icon: 'fas fa-tools',
            title: 'ScriptsToolkit',
            click: async () => {
                // Determine Python path from config or default
                const pythonPath = 'python'
                const scriptPath = 'C:\\Users\\Lance\\Dev\\python\\toolkit\\cli.py'
                
                // Create a new tab running the help command
                this.app.openNewTab({
                    type: 'local',
                    command: pythonPath,
                    args: [scriptPath, '--help'],
                    name: 'Toolkit Help',
                    pause: true
                })
            }
        }]
    }
}

/**
 * The Module Definition
 * Registers the provider with Tabby's dependency injection system
 */
@NgModule({
    imports: [CommonModule],
    providers: [
        { provide: ToolbarButtonProvider, useClass: ToolkitButtonProvider, multi: true },
    ],
})
export default class ScriptsToolkitModule { }
```

---

## Symbol & Feature Glossary (YAML / TypeScript)

### YAML Configuration
| Symbol         | Usage         | Detailed Explanation                                                                            |
| -------------- | ------------- | ----------------------------------------------------------------------------------------------- |
| **Key-Value**  | `key: value`  | Basic mapping. Whitespace **indentation** (2 or 4 spaces) is critical to structure.             |
| **List Item**  | `- value`     | Denotes an item in an array/list.                                                               |
| **Quoting**    | `"C:\\Path"`  | Double quotes allowing escapes. Backslashes in paths must be escaped `\\` in JSON/YAML strings. |
| **Directives** | `type: local` | Tabby specific key. `local` means a local shell process. `ssh` would be remote.                 |
| **Icons**      | `fas fa-sync` | FontAwesome class names used for profile icons in the UI.                                       |

### TypeScript (Angular-style DI used in Tabby)
| Symbol              | Usage                     | Detailed Explanation                                                                                                |
| ------------------- | ------------------------- | ------------------------------------------------------------------------------------------------------------------- |
| **Import**          | `import { x } from 'y'`   | ECMAScript Module syntax. Brings in classes/functions from other compilation units.                                 |
| **Decorator**       | `@NgModule({})`           | Metadata attached to a class. Tells the Angular framework how to process the class (e.g. what providers it offers). |
| **Access Modifier** | `private app: AppService` | Constructor shorthand. Declares a private property `app` and assigns the injected `AppService` to it automatically. |
| **Generic**         | `Promise<void>`           | Type annotation. Represents a value (void) that will be available in the future.                                    |
| **Arrow Func**      | `() => { ... }`           | Anonymous function. `this` context is preserved from the parent scope (crucial in classes).                         |
| **Await/Async**     | `async () => { await x }` | Syntactic sugar for Promises. Makes asynchronous code look synchronous.                                             |
| **Object Literal**  | `{ type: 'local', ... }`  | Creates a plain JavaScript object on the fly to pass configuration options.                                         |
