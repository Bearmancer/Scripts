# Master Task List

- Fix all basedpyrighterror individually after running terminal cmd

---

## Music Commands Refactoring

- Merge into one music command but with regional (# ...) separation

---

### assess contents of all py files individually
### create new structure to integrate all cohesively 
### create new CLI structure with all features including last.fm 
### do not add py scrobble to other parts of dir including pwsh
### create new file to show best way to natively integrate all missing py features in C# 
### consider adding of cliwrap for easier path/subprocess handling
### use imagesharp for image manipulation
### explain how imagesharp differs from pillow in handling of specifying positions within a frame and also how it extracts frame compared to PIL


## System & Maintenance

- **4. Apply PowerShell optimization suggestions** - find better way to expedite loading of pwsh

- **5. File Extension Repair command** - find better way to fix missing extensions 
  - One-liner to detect and fix missing extensions:
  ```powershell
  Get-ChildItem -File | Where-Object { -not $_.Extension } | ForEach-Object { $ext = (ffprobe -v error -show_entries format=format_name -of csv=p=0 $_.FullName 2>$null).Split(',')[0]; if ($ext) { Rename-Item $_.FullName "$($_.Name).$ext" } }

  did not work - PS D:\Google Drive\Games\Others>   Get-ChildItem -Recurse -File | Where-Object { -not $_.Extension } | ForEach-Object { $ext = (ffprobe -v error -show_entries format=format_name -of csv=p=0 $_.FullName 2>$null).Split(',')[0]; if ($ext) { Rename-Item $_.FullName "$($_.Name).$ext" } }
  Rename-Item: The filename, directory name, or volume label syntax is incorrect.
  Rename-Item: The filename, directory name, or volume label syntax is incorrect.
  Rename-Item: The filename, directory name, or volume label syntax is incorrect.
  Rename-Item: The filename, directory name, or volume label syntax is incorrect.
  Rename-Item: The filename, directory name, or volume label syntax is incorrect.
  Rename-Item: The filename, directory name, or volume label syntax is incorrect.
  Rename-Item: The filename, directory name, or volume label syntax is incorrect.
  Rename-Item: The filename, directory name, or volume label syntax is incorrect.
  Rename-Item: The filename, directory name, or volume label syntax is incorrect.
  Rename-Item: The filename, directory name, or volume label syntax is incorrect.
  Rename-Item: The filename, directory name, or volume label syntax is incorrect.
  Rename-Item: The filename, directory name, or volume label syntax is incorrect.
  Rename-Item: The filename, directory name, or volume label syntax is incorrect.
  Rename-Item: The filename, directory name, or volume label syntax is incorrect.
  Rename-Item: The filename, directory name, or volume label syntax is incorrect.
  Rename-Item: The filename, directory name, or volume label syntax is incorrect.
  Rename-Item: The filename, directory name, or volume label syntax is incorrect.
  Rename-Item: The filename, directory name, or volume label syntax is incorrect.
  Rename-Item: The filename, directory name, or volume label syntax is incorrect.
  Rename-Item: The filename, directory name, or volume label syntax is incorrect.
  Rename-Item: The filename, directory name, or volume label syntax is incorrect.
  Rename-Item: The filename, directory name, or volume label syntax is incorrect.
  Rename-Item: The filename, directory name, or volume label syntax is incorrect.
  Rename-Item: The filename, directory name, or volume label syntax is incorrect.
  Rename-Item: The filename, directory name, or volume label syntax is incorrect.
  Rename-Item: The filename, directory name, or volume label syntax is incorrect.
  Rename-Item: The filename, directory name, or volume label syntax is incorrect.
  Rename-Item: The filename, directory name, or volume label syntax is incorrect.
  Rename-Item: The filename, directory name, or volume label syntax is incorrect.
  Rename-Item: Cannot retrieve the dynamic parameters for the cmdlet. The specified wildcard character pattern is not valid: [cheersmumbai @ DT
  PS D:\Google Drive\Games\Others> ^C
  PS D:\Google Drive\Games\Others>
```
  - Uses ffprobe to detect format, renames file with correct extension
  - NOT saved to module (per requirement)
- [ ] **6. Missing fields implementation**
  - Complete to find ALL info (Year, Label, CatalogNumber)
  - Improve progress display: `[✓ Label: DG] [✓ Year: 1985] [? CatalogNumber] (via MusicBrainz)`

---

## Region Cleanup

- [ ] **8. Restore regions for large files**
  - Only add regions to files where they genuinely add clarity
  - Skip small files where regions add noise
