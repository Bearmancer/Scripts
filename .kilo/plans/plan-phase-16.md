# Phase 16: Mail Command Removal

- [ ] **Step 0: Pre-flight Validation, State Capture & Backup**
- [ ] **Step 1: Write the failing test**
- [ ] **Step 2: Read-back Verification (Test File)**
- [ ] **Step 3: Run test to verify it fails (Red Phase)**
- [ ] **Step 3.5: State Assessment & Justification**
- [ ] **Step 4: Write exact implementation**
  - Delete `csharp/src/CLI/Mail/`
  - Remove mail branch from `csharp/src/Program.cs`
  - Delete `csharp/src/Services/Mail/`
  - Delete `csharp/src/Models/Mail.cs`
  - Delete `csharp/src/Core/Persistence/MailStateManager.cs`
- [ ] **Step 5: Run test to verify it passes (Green Phase)**
- [ ] **Step 6: Post-state Capture & Commit**
