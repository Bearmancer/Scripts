# Phase 26: Performance — N+1 Call Elimination

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Eliminate sequential API calls inside loops.

**Architecture:** Use `Task.WhenAll` to execute API requests concurrently.

---

### Task 26.1: Concurrent MusicBrainz Recording Fetches

**Files:**
- Modify: `csharp/src/Services/Music/MusicBrainzService.cs`

- [ ] **Step 1: Write failing test**
Create test asserting `GetReleaseAsync` uses `Task.WhenAll`.
- [ ] **Step 2: Read-back Verification**
- [ ] **Step 3: Run test — expect FAIL**
- [ ] **Step 4: Implementation**
Project tracks into tasks and await `Task.WhenAll`.
- [ ] **Step 5: Run test — expect PASS**
- [ ] **Step 6: Commit**

---

### Task 26.2: Concurrent YouTube Translation Requests

**Files:**
- Modify: `csharp/src/Services/Sync/YouTube/YouTubeTranslationService.cs`

- [ ] **Step 1: Write failing test**
Create test asserting `YouTubeTranslationService` uses `Task.WhenAll`.
- [ ] **Step 2: Read-back Verification**
- [ ] **Step 3: Run test — expect FAIL**
- [ ] **Step 4: Implementation**
Convert sequential translation requests to concurrent tasks.
- [ ] **Step 5: Run test — expect PASS**
- [ ] **Step 6: Commit**
