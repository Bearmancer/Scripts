# Phase 21: DateTimeOffset Migration

- [ ] **Step 0: Pre-flight Validation, State Capture & Backup**
- [ ] **Step 1: Write the failing test**
- [ ] **Step 2: Read-back Verification (Test File)**
- [ ] **Step 3: Run test to verify it fails (Red Phase)**
- [ ] **Step 3.5: State Assessment & Justification**
- [ ] **Step 4: Write exact implementation**
  - Migrate `Models/YouTube.cs` and `Models/LastFm.cs` to `DateTimeOffset`
  - Update `Models/StateTransitions.cs`
  - Create constants for DateTime strings and use them globally
  - Refactor JSON state files / StateManager to correctly handle DTO parsing
- [ ] **Step 5: Run test to verify it passes (Green Phase)**
- [ ] **Step 6: Post-state Capture & Commit**
