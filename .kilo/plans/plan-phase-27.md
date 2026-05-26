# Phase 27: Testing — Utility Classes

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Establish test coverage for core C# and Python utility functions.

**Architecture:** Use `TUnit` for C# and `pytest` for Python utilities.

---

### Task 27.1: Test StringExtensions

**Files:**
- Create: `csharp/src/Tests/Core/StringExtensionsTests.cs`

- [ ] **Step 1: Write test stub**
- [ ] **Step 2: Read-back Verification**
- [ ] **Step 3: Run test — verify failure due to lack of implementation**
- [ ] **Step 4: Implementation**
Add test coverage for `EqualsIgnoreCase`, `ContainsIgnoreCase`, etc.
- [ ] **Step 5: Run test — expect PASS**
- [ ] **Step 6: Commit**

---

### Task 27.2: Test SheetNameHelper

**Files:**
- Create: `csharp/src/Tests/Core/SheetNameHelperTests.cs`

- [ ] **Step 1: Write test stub**
- [ ] **Step 2: Read-back Verification**
- [ ] **Step 3: Run test — verify failure due to lack of implementation**
- [ ] **Step 4: Implementation**
Add tests for character limits, invalid chars, nulls.
- [ ] **Step 5: Run test — expect PASS**
- [ ] **Step 6: Commit**

---

### Task 27.3: Test Python run_command

**Files:**
- Create: `python/tests/toolkit/test_utils.py`

- [ ] **Step 1: Write test stub**
- [ ] **Step 2: Read-back Verification**
- [ ] **Step 3: Run test — verify failure due to lack of implementation**
- [ ] **Step 4: Implementation**
Mock `subprocess.Popen` and cover success/failure paths.
- [ ] **Step 5: Run test — expect PASS**
- [ ] **Step 6: Commit**

---

### Task 27.4: Test Python filesystem utils

**Files:**
- Create: `python/tests/toolkit/test_filesystem.py`

- [ ] **Step 1: Write test stub**
- [ ] **Step 2: Read-back Verification**
- [ ] **Step 3: Run test — verify failure due to lack of implementation**
- [ ] **Step 4: Implementation**
Use `tmp_path` to test folder size and directory listing.
- [ ] **Step 5: Run test — expect PASS**
- [ ] **Step 6: Commit**
