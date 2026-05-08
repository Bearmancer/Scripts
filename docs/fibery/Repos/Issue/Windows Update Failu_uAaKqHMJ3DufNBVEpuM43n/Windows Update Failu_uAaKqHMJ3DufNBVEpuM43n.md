# Description

-----------------------------

Windows Update error 80246007 prevents Windows 11 25H2 update. MSI installer cache corrupted (errors 1603/1612). Repair
scripts exist (Fix-WindowsUpdates.ps1, Fix-Updates-Integrated.ps1) but did not resolve root cause. WU datastore
corruption suspected. See WindowsUpdate.log for details.

# Plan

-----------------------------

# Windows Update Repair Plan

## Root Cause

WU datastore corruption (error 80246007 = WU_E_DS_DECLINENOTIFICATIONS). The Windows Update internal database (
DataStore.edb) has corrupted decline-notification state. MSI installer cache corruption is a secondary symptom.

## Steps

1. \[ \] Run Windows Update Troubleshooter (wsreset.exe)
2. \[ \] DISM /RestoreHealth to repair system image
3. \[ \] SFC /ScanNow to repair system files
4. \[ \] Manual WU datastore reset: stop WU services, delete DataStore.edb, restart
5. \[ \] If MSI errors persist: run Windows Installer CleanUp utility
6. \[ \] Registry fix: check HKEY_LOCAL_MACHINESOFTWAREMicrosoftWindowsCurrentVersionWindowsUpdate for corrupt state
7. \[ \] As last resort: Windows 11 25H2 ISO in-place upgrade (keeps apps/files)

## Related Files

* Fix-WindowsUpdates.ps1
* Fix-Updates-Integrated.ps1
* RepairUpdates.cmd
* WindowsUpdate.log

# Prompt

-----------------------------

# Prompt

Investigate Windows Update errors (80246007) and create a diagnostic issue. Document the relationship to VSCode NUL
artifacts - the Copilot agent telemetry log (stored as NUL file) may be interfering with Windows Update's
IsCommitRequired check by corrupting the WU datastore. Other angles: check if the Copilot debug logging setting
`github.copilot.chat.agentDebugLog.fileLogging.enabled` is the root cause of NUL file creation.

# Research

-----------------------------

# Windows Update Research Log

## Error 80246007 Analysis

* Symbolic name: WU_E_DS_DECLINENOTIFICATIONS
* Meaning: Internal WU datastore corruption in decline-notification tracking
* This is NOT a network issue, NOT a proxy issue, NOT a service-not-running issue
* Datastore file: C:WindowsSoftwareDistributionDataStoreDataStore.edb (ESE/Jet Blue database)

## WindowsUpdate.log Key Events

* Multiple WU service restarts with exit code 0x240001
* Certificate validity warnings (system clock OK per logs)
* Update Windows 11 25H2 fails at IsCommitRequired check
* WU client version 10.0.19041.6093 on Windows 10 22H2 (19045)

## Previous Repairs (2026-05-01)

* SoftwareDistribution + catroot2 delete: DONE (by Fix-Updates-Integrated.ps1)
* Winsock reset: DONE (reboot pending)
* These resets address download corruption but NOT datastore state corruption

# Update 2026-05-03: Full System Diagnosis Complete

## Key Discovery

CMD `del "\\?\path\to\NUL"` works. PowerShell Remove-Item does not. The 13-target PS1 script crashed before executing
because of:

1. ${currentUser} variable delimiter bug (line 106)
2. Missing closing foreach brace
3. Both recovery logs (133626, 141216) crashed after 9 lines

## Content of NUL Files

* Insiders NUL (1.2MB): GitHub Copilot OpenTelemetry agent telemetry spans - Copilot debug logging output
* Stable VSCode nul (0 bytes): Empty
* Profile nul (43 bytes): Text: "Media Server: Backups Config Data Tests"

## Root Cause The 1.2MB NUL file is Copilot agent telemetry. The setting
`github.copilot.chat.agentDebugLog.fileLogging.enabled: true` causes the agent to write span data to a file literally named "NUL" instead of a proper log filename.

## Solution

Created NUL-Purge.cmd (CMD-native, no PowerShell dependency). Uses `del /f /q "\\?\path\NUL"` which works because the
extended-path prefix bypasses Win32 DOS device name handler. Escalation chain: del -> takeown+icacls+del -> rd -> chkdsk
reboot.

Script at: C:/Users/Lance/Desktop/Hooks/NUL-Purge.cmd\
Usage: NUL-Purge.cmd \[--purge|--force\]

## Expanded Scope Results (2026-05-03)

* Scanned 14 target directories including Desktop for snapshots
* Found 0 NUL artifacts after cleanup
* Old snapshot directory (vscode-insiders-snapshot-20260503-025254) contained backup NUL - deleted

## Prevention

Disable `github.copilot.chat.agentDebugLog.fileLogging.enabled` in VSCode settings (done).

# Validation

-----------------------------

# Validation

1. \[ \] Run Windows Update Troubleshooter: msdt.exe /id WindowsUpdateDiagnostic
2. \[ \] DISM /Online /Cleanup-Image /RestoreHealth
3. \[ \] SFC /SCANNOW
4. \[ \] Manual datastore reset: stop WU services, delete DataStore.edb, restart services
5. \[ \] Check registry HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\WindowsUpdate for corruption
6. \[ \] Try Windows 11 25H2 installation via Media Creation Tool / ISO in-place upgrade
