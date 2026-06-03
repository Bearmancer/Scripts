# Windows 10 Comprehensive Failure Purge Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Repair all identified Windows 10 failures: WSL corruption, broken EXEs, installer cache corruption, OpenSSH crash loop, conhost.exe instability, and system file integrity issues.

**Architecture:** Sequential repair phases — system image repair first (foundation), then installer cache cleanup, then individual component repairs. Each phase validates before proceeding.

**Tech Stack:** Windows Admin CMD, DISM, SFC, MSI cleanup, PowerShell (Windows)

---

## Pre-Requisites

- [ ] **Step 1: Create System Restore Point**

Open **Windows CMD as Administrator** and run:
```cmd
wmic.exe /Namespace:\\root\default Path SystemRestore Call CreateRestorePoint "Pre-Repair Baseline", 100, 7
```
Expected: `Method execution successful.`

- [ ] **Step 2: Backup Windows Installer Registry Keys**

```cmd
reg export "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Installer" C:\Backup\installer-backup.reg
reg export "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData" C:\Backup\installer-userdata-backup.reg
```
Expected: Files saved to `C:\Backup\`

---

## Phase 1: System Image & File Repair

### Task 1: Repair Windows Component Store (DISM)

**Files:**
- Modify: Windows component store (`C:\Windows\WinSxS`)
- Log: `C:\Windows\Logs\DISM\dism.log`

- [ ] **Step 1: Run DISM RestoreHealth**

```cmd
DISM /Online /Cleanup-Image /CheckHealth
```
Expected: `No component store corruption detected.` or repairable issues listed.

- [ ] **Step 2: Run DISM RestoreHealth (if needed)**

```cmd
DISM /Online /Cleanup-Image /RestoreHealth
```
Expected: `The operation completed successfully.`

- [ ] **Step 3: Verify CBS Log is Clean**

```cmd
findstr /i "error fail" C:\Windows\Logs\CBS\CBS.log | findstr /i /v "Info" | tail -5
```
Expected: No new critical errors after DISM run.

---

### Task 2: System File Checker (SFC)

**Files:**
- Scans: `C:\Windows\System32\*`
- Log: `C:\Windows\Logs\CBS\CBS.log`

- [ ] **Step 1: Run SFC**

```cmd
sfc /scannow
```
Expected: `Windows Resource Protection did not find any integrity violations.`

- [ ] **Step 2: Check SFC Results**

If SFC found and repaired files, re-run to confirm:
```cmd
sfc /scannow
```
Expected: Clean scan on second run.

---

### Task 3: Code Integrity Fix (fcon.dll / aepic.dll)

**Files:**
- Repair: `C:\Windows\System32\fcon.dll`
- Repair: `C:\Windows\System32\aepic.dll`

- [ ] **Step 1: Verify DLL Integrity**

```cmd
sfc /verifyfile C:\Windows\System32\fcon.dll
sfc /verifyfile C:\Windows\System32\aepic.dll
```
Expected: File verification succeeded.

- [ ] **Step 2: If verification fails, extract from component store**

```cmd
takeown /f C:\Windows\System32\fcon.dll
icacls C:\Windows\System32\fcon.dll /grant Administrators:F
copy C:\Windows\WinSxS\amd64_microsoft-windows-fcon_31bf3856ad364e35_10.0.19041.1_none_*\fcon.dll C:\Windows\System32\fcon.dll
```
Repeat for `aepic.dll` with the matching WinSxS source.

---

## Phase 2: Windows Installer Cache Repair

### Task 4: Fix Corrupt MSI Registration (Error 1714/1612)

**Files:**
- Registry: `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData`
- Cache: `C:\Windows\Installer\`

- [ ] **Step 1: Identify Corrupt Products**

```cmd
wmic product where "name like '%%Windows Subsystem%%'" get name,version,identifyingnumber
wmic product where "name like '%%Google Chrome%%'" get name,version,identifyingnumber
wmic product where "name like '%%Visual C++%%'" get name,version,identifyingnumber
```
Expected: List of product GUIDs.

- [ ] **Step 2: Check Installer Cache for Missing Files**

```cmd
dir C:\Windows\Installer\*.msi /s 2>nul | find /c ".msi"
```
Expected: Count > 0. If cache is nearly empty, that's the root cause.

- [ ] **Step 3: Remove Orphaned Registry Entries (WSL First)**

```cmd
reg delete "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{B637A6A6-5591-4503-AFD8-776164EB837A}" /f
```
Expected: `The operation completed successfully.`

- [ ] **Step 4: Clean WSL Package State**

```cmd
dir /s /b C:\Windows\Installer\$PatchCache$\*WSL* 2>nul
dir /s /b C:\Windows\Installer\$PatchCache$\*linux* 2>nul
```
Delete any orphaned WSL-related files found.

- [ ] **Step 5: Verify MSI Service is Running**

```cmd
sc query msiserver
```
Expected: `STATE: 4 RUNNING`

If not running:
```cmd
net start msiserver
```

---

### Task 5: Reinstall WSL

**Files:**
- Feature: Windows Subsystem for Linux
- Package: `Microsoft-Windows-Subsystem-Linux`

- [ ] **Step 1: Disable WSL Feature**

```cmd
dism /online /disable-feature /featurename:Microsoft-Windows-Subsystem-Linux /norestart
```
Expected: `The operation completed successfully.`

- [ ] **Step 2: Enable WSL2 Feature**

```cmd
dism /online /enable-feature /featurename:VirtualMachinePlatform /all /norestart
dism /online /enable-feature /featurename:Microsoft-Windows-Subsystem-Linux /all /norestart
```
Expected: `The operation completed successfully.`

- [ ] **Step 3: Reboot**

```cmd
shutdown /r /t 10 /c "WSL feature reset - rebooting"
```

- [ ] **Step 4: After Reboot, Install Ubuntu**

```cmd
wsl --set-default-version 2
wsl --install -d Ubuntu
```
Expected: Ubuntu downloads and installs successfully.

---

## Phase 3: Component Repairs

### Task 6: Fix conhost.exe Crashes

**Files:**
- Binary: `C:\Windows\System32\conhost.exe`
- Log: Application Event Log (Event 1000)

- [ ] **Step 1: Verify conhost.exe Integrity**

```cmd
sfc /verifyfile C:\Windows\System32\conhost.exe
```
Expected: `Windows Resource Protection did not find any integrity violations.`

- [ ] **Step 2: If corrupted, replace from WinSxS**

```cmd
takeown /f C:\Windows\System32\conhost.exe
icacls C:\Windows\System32\conhost.exe /grant Administrators:F
copy C:\Windows\WinSxS\amd64_microsoft-windows-console_31bf3856ad364e35_10.0.19041.1_none_*\conhost.exe C:\Windows\System32\conhost.exe
```

- [ ] **Step 3: Clear Font Cache (common cause of conhost crashes)**

```cmd
net stop FontCache
del /q C:\Windows\ServiceProfiles\LocalService\AppData\Local\FontCache\*.dat 2>nul
net start FontCache
```

- [ ] **Step 4: Test Console**

Open a new CMD window and run:
```cmd
echo "Console test" && dir C:\Windows\System32\conhost.exe
```
Expected: No crash.

---

### Task 7: Fix OpenSSH Crash Loop

**Files:**
- Service: `sshd`
- Config: `C:\ProgramData\ssh\sshd_config`

- [ ] **Step 1: Stop Crash Loop**

```cmd
net stop sshd
sc config sshd start= demand
```
Expected: Service stopped and set to manual start.

- [ ] **Step 2: Check SSH Event Log**

```cmd
wevtutil qe "OpenSSH/Operational" /c:10 /f:text /rd:true
```
Expected: Review for specific SSH crash cause.

- [ ] **Step 3: Verify sshd Binary**

```cmd
where sshd
sfc /verifyfile "C:\Windows\System32\OpenSSH\sshd.exe"
```
Expected: Binary exists and passes verification.

- [ ] **Step 4: Reinstall OpenSSH (if needed)**

```cmd
winget uninstall Microsoft.OpenSSH.Beta
winget install Microsoft.OpenSSH.Beta --source winget
sc config sshd start= auto
net start sshd
```

- [ ] **Step 5: Verify Service Running**

```cmd
sc query sshd
```
Expected: `STATE: 4 RUNNING`

---

### Task 8: Fix Windows Defender

**Files:**
- Service: WinDefend
- Intelligence: `C:\ProgramData\Microsoft\Windows Defender\Platform\*`

- [ ] **Step 1: Force Update Defender Intelligence**

```cmd
"%ProgramFiles%\Windows Defender\MpCmdRun.exe" -SignatureUpdate
```
Expected: `Signature update successfully completed.`

- [ ] **Step 2: Run Full Scan**

```cmd
"%ProgramFiles%\Windows Defender\MpCmdRun.exe" -Scan -ScanType 2
```
Expected: Scan completes without errors.

- [ ] **Step 3: Check Defender Event Log**

```cmd
wevtutil qe "Microsoft-Windows-Windows Defender/Operational" /c:5 /f:text /rd:true
```
Expected: No error events (Event 1000/1001).

---

### Task 9: Fix VSS (Volume Shadow Copy)

**Files:**
- Service: VSS
- Provider: `C:\Windows\System32\vssvc.exe`

- [ ] **Step 1: Restart VSS Service**

```cmd
net stop vss
net start vss
```

- [ ] **Step 2: Re-register VSS Components**

```cmd
net stop swprv
regsvr32 /s ole32.dll
regsvr32 /s oleaut32.dll
regsvr32 /s vss_ps.dll
vssvc /register
net start swprv
```

- [ ] **Step 3: Test Shadow Copy Creation**

```cmd
wmic shadowcopy list
```
Expected: List of shadow copies or empty (no errors).

---

## Phase 4: Network & Registry Cleanup

### Task 10: Fix DCOM Permission Errors

**Files:**
- Registry: `HKCR\AppID\{15C20B67-12E7-4BB6-92BB-7AFF07997402}`

- [ ] **Step 1: Check DCOM Settings**

```cmd
dcomcnfg
```
Navigate to: Component Services > Computers > My Computer > DCOM Config

- [ ] **Step 2: Grant Local Activation Permission**

Find the application with CLSID `{2593F8B9-4EAF-457C-B68A-50F6B8EA6B54}` and grant Local Activation to `LANCE\Lance`.

---

### Task 11: Fix TCP/IP Port Exhaustion

**Files:**
- Registry: `HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters`

- [ ] **Step 1: Increase ephemeral port range**

```cmd
netsh int ipv4 set dynamicport tcp start=1025 num=64511
```

- [ ] **Step 2: Reduce TIME_WAIT timeout**

```cmd
reg add "HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v TcpTimedWaitDelay /t REG_DWORD /d 30 /f
```

- [ ] **Step 3: Reset TCP/IP stack**

```cmd
netsh int ip reset
netsh winsock reset
```
Reboot required after this step.

---

## Phase 5: Validation

### Task 12: Final Validation

- [ ] **Step 1: Run Full SFC Scan**

```cmd
sfc /scannow
```
Expected: `Windows Resource Protection did not find any integrity violations.`

- [ ] **Step 2: Run DISM Check**

```cmd
DISM /Online /Cleanup-Image /CheckHealth
```
Expected: `No component store corruption detected.`

- [ ] **Step 3: Test WSL**

```cmd
wsl --list --verbose
```
Expected: Ubuntu listed with VERSION 2 and STATE Running.

- [ ] **Step 4: Test SSH**

```cmd
sc query sshd
```
Expected: `STATE: 4 RUNNING`

- [ ] **Step 5: Test Console**

Open multiple CMD windows, run `dir` commands. No crashes.

- [ ] **Step 6: Check Event Log for 24 Hours**

Monitor Application and System logs for 24 hours. Expected: No recurrence of Error 1714, Event 1000 crashes, or Event 7031 service terminations.

---

## Summary

| Phase | Tasks | Issues Fixed |
|-------|-------|--------------|
| 1. System Image | 3 | Component store corruption, system file integrity, Code Integrity DLLs |
| 2. MSI Cache | 2 | Error 1714/1612 for WSL, Chrome, VC++ |
| 3. Components | 4 | conhost.exe crashes, OpenSSH loop, Defender, VSS |
| 4. Network/Registry | 2 | DCOM permissions, TCP/IP exhaustion |
| 5. Validation | 1 | End-to-end verification |
