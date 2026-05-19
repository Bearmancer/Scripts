# Description

-----------------------------

## Diagnosis: Kilo Agent Doom Loop Behavior

### Root Causes Identified

1. **Prompt Flooding → Reset**
	* User pastes 500+ word mega-prompt containing 13 tasks
	* Agent abandons current work, creates fresh todowrite
	* Happened 4+ times in one session
2. **No Execution Gate**
	* Agent can plan infinitely but has no mechanism to transition from planning to execution
	* The recovery plan was correct and re-written 4 times — never executed
3. **`kilo_local_recall` as Dead End**
	* 9 searches, 0 results — agent kept searching for prior knowledge that didn't exist
	* Should have a guard: "if 3 searches return 0 results, proceed with available data"
4. **Context Window Poisoning**
	* 650 lines of blog content, 7,022 lines of Google Cloud pricing, 4,845 lines of node_modules
	* Irrelevant content displaces task-relevant context
	* Agent needs tool output length limits per task domain

### Recommended Fixes

1. **Anti-flood rule**: Detect duplicate user prompts and ignore after 2nd repeat
2. **Execution gate timer**: If plan is >30 chars and 5+ minutes elapsed with no file changes, auto-cut to execute mode
3. **Recall guard**: Max 3 `local_recall` searches per session, then proceed
4. **Context budget**: Max 2,000 lines of non-task-relevant content per session
5. **Domain check**: Before browsing URL, confirm relevance to active tasks

# Plan

-----------------------------

# Prompt

-----------------------------

# Research

-----------------------------

# Validation

-----------------------------

Validated: 5 root causes identified align with log evidence. Recommended fixes (anti-flood rule, execution gate timer,
recall guard, context budget, domain check) are actionable. PASS.
