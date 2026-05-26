# Merge Non-Doc Branches into Main Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Merge every code-bearing branch into `main`, keep docs-only branch work out of the merge set, and finish with a clean, validated `main` that matches `origin/main`.

**Architecture:** Treat `main` as the integration target and keep the merge work on a temporary integration branch so the current workspace stays isolated until the branch set is complete. First sync against `origin/main`, then classify live branches with `git branch --contains` and `gh pr list`. Merge only branches that change code, tests, or build behavior. Skip branches that are docs-only or already reachable from `main`, and validate after each merge wave with `dotnet restore`, `dotnet build`, and `dotnet test`.

**Tech Stack:** Git 2.x, GitHub CLI (`gh`), PowerShell 7+, .NET 10 SDK.

**Current branch snapshot as of 2026-05-27:**
- Candidate non-doc branches: `mega-plan-creation`, `ef-state`, `jstor-free-pdf-download`
- Excluded docs-only branch: `origin/feature/cpm-srp-refactoring-3041881028894447998`
- Observed state: local `main` already contains the tip commits for `mega-plan-creation` and `ef-state`, so those merges should be verified for containment before any merge command is run

---

### Task 1: Lock the branch set and classify merge candidates

Files:
- `main`
- `origin/main`
- `mega-plan-creation`
- `ef-state`
- `jstor-free-pdf-download`
- `origin/feature/cpm-srp-refactoring-3041881028894447998`

- [ ] **Step 1: Fetch the latest refs and capture the live branch list**

```powershell
git fetch origin --prune
git branch -a --format='%(refname:short) %(objectname:short) %(subject)'
gh pr list --state open --base main --json number,title,headRefName,state,mergeStateStatus
```

Expected: local and remote refs are current; the docs-only CPM SRP branch is visible but excluded from the merge set.

- [ ] **Step 2: Confirm which known candidate branch tips are already reachable from `main`**

```powershell
git branch --contains 4895312
git branch --contains f6513c1
git branch --contains ddd2c1b
```

Expected: any branch whose tip is already in `main` is marked "skip merge, already contained" and not merged again.

- [ ] **Step 3: Record the merge order for any remaining non-doc branches**

```markdown
Merge order:
1. `jstor-free-pdf-download`
2. `mega-plan-creation`
3. `ef-state`
```

Expected: the order is deterministic, low-risk work goes first, and the docs-only branch stays out of scope.

### Task 2: Sync the integration branch with `origin/main`

Files:
- `merge/non-doc-branches`
- `origin/main`

- [ ] **Step 1: Create a clean integration branch from the current local `main`**

```powershell
git switch -c merge/non-doc-branches main
```

Expected: a new integration branch exists and the original `main` branch is left untouched.

- [ ] **Step 2: Merge `origin/main` into the integration branch**

```powershell
git merge --no-ff origin/main
```

Expected: the remote `origin/main` tip is incorporated into the integration branch without dropping any local history.

### Task 3: Merge the remaining non-doc branches one at a time

Files:
- `merge/non-doc-branches`
- `jstor-free-pdf-download`
- `mega-plan-creation`
- `ef-state`

- [ ] **Step 1: Merge `jstor-free-pdf-download` if it is not already contained**

```powershell
git merge --no-ff jstor-free-pdf-download
```

Expected: the `.gitignore` and worktree-hygiene changes land without pulling in docs-only content.

- [ ] **Step 2: Merge `mega-plan-creation` only if Task 1 showed it is not already contained**

```powershell
git merge --no-ff mega-plan-creation
```

Expected: the EF Core and data-layer work, including `DbContext` and `SourceRecord` changes, is fully absorbed.

- [ ] **Step 3: Merge `ef-state` only if Task 1 showed it is not already contained**

```powershell
git merge --no-ff ef-state
```

Expected: the final EF/state/test completion work is absorbed cleanly.

- [ ] **Step 4: Keep the docs-only CPM SRP branch out of the merge set**

```powershell
git log --oneline --decorate --graph --max-count=20 merge/non-doc-branches..origin/feature/cpm-srp-refactoring-3041881028894447998
```

Expected: the docs-only branch remains separate unless a deliberate docs merge is planned later.

### Task 4: Validate the merged integration branch

Files:
- `csharp/Scripts.slnx`
- `merge/non-doc-branches`
- `AI/plans/CURRENT_STATUS.md`

- [ ] **Step 1: Restore dependencies**

```powershell
dotnet restore csharp/Scripts.slnx
```

Expected: restore completes without errors.

- [ ] **Step 2: Build the solution**

```powershell
dotnet build csharp/Scripts.slnx
```

Expected: the build succeeds and no new merge-induced errors are introduced.

- [ ] **Step 3: Run the full test suite**

```powershell
dotnet test csharp/Scripts.slnx
```

Expected: all tests pass.

- [ ] **Step 4: Confirm the integration branch only contains the intended merge set**

```powershell
git rev-list --left-right --count merge/non-doc-branches...origin/main
```

Expected: the count shrinks to only the remaining publish delta until the final push, then reaches `0 0`.

### Task 5: Promote the integration branch back to `main` and publish

Files:
- `main`
- `origin/main`

- [ ] **Step 1: Merge the integration branch back into `main`**

```powershell
git switch main
git merge --no-ff merge/non-doc-branches
```

Expected: `main` now contains every selected non-doc branch.

- [ ] **Step 2: Push `main` to origin**

```powershell
git push origin main
```

Expected: `origin/main` matches local `main` unless branch protection blocks the push.

- [ ] **Step 3: If direct push is blocked, use `gh` to merge the integration branch through a PR instead of forcing history**

```powershell
gh pr create --base main --head merge/non-doc-branches --title "Merge non-doc branches into main" --body "Consolidate code-bearing branches and keep docs-only work out of scope."
gh pr merge --merge
```

Expected: GitHub records the merge cleanly and branch protection stays intact.

### Task 6: Cleanup and status update

Files:
- `AI/plans/CURRENT_STATUS.md`
- merged source branches

- [ ] **Step 1: Update `AI/plans/CURRENT_STATUS.md` with the final merge state**

```markdown
Record:
1. Which non-doc branches were merged
2. Which branch tips were already contained in `main`
3. That `origin/feature/cpm-srp-refactoring-3041881028894447998` stayed out of scope
```

Expected: the status doc reflects the final branch merge state without ambiguity.

- [ ] **Step 2: Delete local source branches only after `main` contains their tips**

```powershell
git branch -d jstor-free-pdf-download
git branch -d mega-plan-creation
git branch -d ef-state
```

Expected: the local branch list is clean and no branch is deleted before its work is safely in `main`.

- [ ] **Step 3: Remove remote source branches only after confirmation**

```powershell
git push origin --delete jstor-free-pdf-download
git push origin --delete mega-plan-creation
git push origin --delete ef-state
```

Expected: the remote branch list matches the merged state and nothing active is deleted early.
