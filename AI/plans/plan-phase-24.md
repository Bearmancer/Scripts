# Phase 24: Security — Deserialization & Injection

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Resolve immediate arbitrary code execution and command injection vulnerabilities.

**Architecture:** Replace Python `pickle` with `json`; enforce `ArgumentList` in C# Process execution.

---

### Task 24.1: Secure Python Pickle usage

**Files:**
- Modify: `python/toolkit/lastfm.py`

- [ ] **Step 1: Write failing test**
Create a test that verifies `lastfm.py` does not contain `import pickle`.
- [ ] **Step 2: Read-back Verification**
- [ ] **Step 3: Run test — expect FAIL**
- [ ] **Step 4: Implementation**
Replace `pickle.dump/load` with `json.dump/load` in `python/toolkit/lastfm.py`.
- [ ] **Step 5: Run test — expect PASS**
- [ ] **Step 6: Commit**

---

### Task 24.2: Fix Command Injection in LibreTranslateHostManager

**Files:**
- Modify: `csharp/src/Services/Language/LibreTranslateHostManager.cs`

- [ ] **Step 1: Write failing test**
Create a test that asserts `Arguments` is not assigned, and `ArgumentList.Add()` is used.
- [ ] **Step 2: Read-back Verification**
- [ ] **Step 3: Run test — expect FAIL**
- [ ] **Step 4: Implementation**
Refactor `RunDockerQuery` and `RunDockerCommand` to use `ArgumentList`.
- [ ] **Step 5: Run test — expect PASS**
- [ ] **Step 6: Commit**
