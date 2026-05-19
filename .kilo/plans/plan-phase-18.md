# Phase 18: TUnit Test Migration

- [ ] **Step 0: Pre-flight Validation, State Capture & Backup**
- [ ] **Step 1: Write the failing test**
- [ ] **Step 2: Read-back Verification (Test File)**
- [ ] **Step 3: Run test to verify it fails (Red Phase)**
- [ ] **Step 3.5: State Assessment & Justification**
- [ ] **Step 4: Write exact implementation**
  - Create `csharp/tests/CSharpScripts.Tests/CSharpScripts.Tests.csproj` (OutputType: Exe, TUnit package)
  - Update `Scripts.slnx`
  - Create a smoke test `SmokeTests.cs`
  - Update `.kilo/tests/RunTests.ps1` and `VerifyTestBuild.ps1`
- [ ] **Step 5: Run test to verify it passes (Green Phase)**
- [ ] **Step 6: Post-state Capture & Commit**
