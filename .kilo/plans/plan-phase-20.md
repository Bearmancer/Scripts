# Phase 20: Compile Excludes & Loop Standard

- [ ] **Step 0: Pre-flight Validation, State Capture & Backup**
- [ ] **Step 1: Write the failing test**
- [ ] **Step 2: Read-back Verification (Test File)**
- [ ] **Step 3: Run test to verify it fails (Red Phase)**
- [ ] **Step 3.5: State Assessment & Justification**
- [ ] **Step 4: Write exact implementation**
  - Add `Compile Remove` to `CSharpScripts.csproj` for `Reader/**`, `Orchestrators/**`, `CLI/**`, `Program.cs`
  - Convert `for` loops in `LastFmService.cs` and `HtmlCleanupHelper.cs` to `foreach`
- [ ] **Step 5: Run test to verify it passes (Green Phase)**
- [ ] **Step 6: Post-state Capture & Commit**
