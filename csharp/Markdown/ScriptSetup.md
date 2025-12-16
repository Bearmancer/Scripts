# Script Setup Guide

This guide explains how to run the CLI without `dotnet run --`.

---

## PowerShell Alias (Recommended for Development)

Add to your `$PROFILE`:

```powershell
function scripts { dotnet run --project "C:\Users\Lance\Dev\Scripts\csharp\CSharpScripts.csproj" -- $args }
```

To edit profile:
```powershell
notepad $PROFILE
```

Then restart PowerShell or run:
```powershell
. $PROFILE
```

### Usage

```powershell
scripts sync all
scripts music search "Beatles"
scripts music lookup mb:12345
```

---

## Tab Completion

After setting up the alias, install tab completion:

```powershell
scripts completion install
```

This adds auto-complete support to your profile. Restart PowerShell to activate.

---

## PowerShell Alias vs Dotnet Tool

| Aspect            | PowerShell Alias           | Dotnet Tool                                    |
| ----------------- | -------------------------- | ---------------------------------------------- |
| **Setup**         | Add function to `$PROFILE` | `dotnet pack` + `dotnet tool install --global` |
| **Startup Speed** | Slower (compiles each run) | Faster (pre-compiled)                          |
| **Code Changes**  | Immediate (uses source)    | Requires re-pack + re-install                  |
| **Portability**   | This machine only          | Install on any machine with .NET               |
| **Best For**      | Active development         | Distribution to others                         |

### When to Use Each

**Use Alias when:**
- Actively developing the CLI
- Want instant code changes to apply
- Only running on your machine

**Use Dotnet Tool when:**
- Sharing with others
- Need fast startup time
- Distributing a stable version

---

## Dotnet Tool Setup (Alternative)

If you later want to publish as a tool:

```powershell
# Add to .csproj
# <PackAsTool>true</PackAsTool>
# <ToolCommandName>scripts</ToolCommandName>

dotnet pack
dotnet tool install --global --add-source ./nupkg scripts
```

Then `scripts` works from anywhere without the alias.
