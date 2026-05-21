# Phase 25: Performance — Loop Optimizations

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Optimize explicitly marked `for` loops to use `foreach` struct enumerators.

**Architecture:** Use modern C# `foreach` over `List<T>` which is optimized.

---

### Task 25.1: Optimize FetchScrobblesBatchAsync

**Files:**
- Modify: `csharp/src/Services/Sync/LastFmService.cs`

- [ ] **Step 1: Write failing test**
Create test asserting `FetchScrobblesBatchAsync` does not contain `for (` block.
- [ ] **Step 2: Read-back Verification**
- [ ] **Step 3: Run test — expect FAIL**
- [ ] **Step 4: Implementation**
Convert `for` to `foreach` and remove `// PERFORMANCE` comment.
- [ ] **Step 5: Run test — expect PASS**
- [ ] **Step 6: Commit**

---

### Task 25.2: Optimize FetchPageAsync

**Files:**
- Modify: `csharp/src/Services/Sync/LastFmService.cs`

- [ ] **Step 1: Write failing test**
Create test asserting `FetchPageAsync` does not contain `for (` block.
- [ ] **Step 2: Read-back Verification**
- [ ] **Step 3: Run test — expect FAIL**
- [ ] **Step 4: Implementation**
Convert `for` to `foreach` and remove `// PERFORMANCE` comment.
- [ ] **Step 5: Run test — expect PASS**
- [ ] **Step 6: Commit**

---

### Task 25.3: Optimize SaveMergedScrobblesAsync

**Files:**
- Modify: `csharp/src/Services/Sync/LastFmService.cs`

- [ ] **Step 1: Write failing test**
Create test asserting `SaveMergedScrobblesAsync` does not contain `for (` block.
- [ ] **Step 2: Read-back Verification**
- [ ] **Step 3: Run test — expect FAIL**
- [ ] **Step 4: Implementation**
Convert `for` to `foreach` and remove `// PERFORMANCE` comment.
- [ ] **Step 5: Run test — expect PASS**
- [ ] **Step 6: Commit**
