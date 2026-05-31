# SDET Skills Assessment & Codebase Mastery
**Target Role:** Python SDET / Automation Architect
**Candidate Advantage:** High-rigor C# structural engineering applied to dynamic environments.
**Design Philosophy:** ADD/OCD Optimized. Zero assumptions. Hyper-modular. 100% concrete documentation.

---

## MODULE 1: The "C# Rigor in a Python World" Protocol
*Why it matters:* Most Python testing environments lack structural integrity. By leveraging strict C# engineering principles (enforced static typing, architectural guard tests, deterministic dependency locking), the candidate demonstrates an "Architect" mindset rather than a "Scripter" mindset.

### 1.1 Architectural & Guard Testing (Shift-Left Quality)
**Definition:** Programmatic enforcement of architectural boundaries and coding standards before compilation.
**Concrete Code Evidence:**
- **Reflection-based assertions:** Implementation of `SmokeTests.cs` using C# reflection to scan all assemblies for `[Test]` attributes, explicitly failing the build if `Skip` or `Ignore` strings are detected.
- **AST/Regex Dependency Rules:** Implementation of `EditorConfigEf10RulesTests.cs` and `Ef11ForbiddenPatternsTests.cs` to prevent developers (or AI agents) from introducing deprecated Entity Framework patterns.

### 1.2 Integration Testing with Real Infrastructure
**Definition:** Validating data access layers against actual vendor engines rather than in-memory mocks.
**Concrete Code Evidence:**
- **Docker Compose Fixtures:** Implementation of `DatabaseTestFixture.cs` (an `IAsyncDisposable` fixture) that binds to a live PostgreSQL 16 container, bypassing `EF Core In-Memory`.
- **Idempotent Test State:** Utilization of `ctx.Database.EnsureDeletedAsync()` and `ctx.Database.MigrateAsync()` to guarantee clean schema state per test class execution.

### 1.3 Resilient API Orchestration & Concurrency
**Definition:** Engineering network I/O to maximize throughput without exceeding third-party rate limits.
**Concrete Code Evidence:**
- **Concurrency primitives:** Elimination of sequential `foreach` loops in `YouTubePlaylistOrchestrator.cs` in favor of `Task.WhenAll`.
- **Thread-safe DB Access:** Mitigation of EF Core's non-thread-safe `DbContext` by batching in-memory collections or utilizing `IDbContextFactory`.
- **Polly Resilience:** Implementation of `RepositoryResilienceFactory.cs` for exponential backoff handling of `NpgsqlException` and HTTP 429 errors.

### 1.4 Strict Static Typing in Dynamic Languages (Python)
**Definition:** Enforcing enterprise-level compilation safety in Python.
**Concrete Code Evidence:**
- **Dependency Locking:** Usage of `uv` (and `uv.lock`) instead of standard `pip` for high-speed, deterministic builds.
- **Strict Mode Enforcement:** Configuration of `ty` (`tool.ty.rules all = "error"`) in `pyproject.toml`, forcing code to adhere to rigorous structural contracts akin to `basedpyright`.

---

## MODULE 2: The "Research Interviewer" Elicitation System
*Why it matters:* SDETs must extract concrete, testable requirements from vague stakeholder requests. This module utilizes the Smithery `agentient/research-interviewer` framework for systematic knowledge elicitation.

### 2.1 The 6-Phase Elicitation Workflow
1. **Establish (Context):** Define `interview_goal`, `topic`, and `output_format` (e.g., `REQUIREMENTS`). Set `validation_mode` (empathetic, balanced, rigorous).
2. **Map (MECE Scope):** Decompose the topic into Mutually Exclusive, Collectively Exhaustive dimensions.
3. **Question (Adaptive):** Execute one of 8 question types (e.g., Grand Tour, Structural, Devil's Advocate) based on current knowledge gaps. *Strict constraint: ONE question per turn.*
4. **Track (Confidence):** Assign epistemic status (EPISTEMIC, ALEATORY, MODEL) and a confidence score (0.0 - 1.0) to every finding.
5. **Validate (Synthesis):** Cross-reference findings to build a Consistency Matrix. Surface Explicit, Implicit, and Structural assumptions. "Steelman" the gathered knowledge.
6. **Surface (Output):** Generate final `CONTRACT-01` XML schema (e.g., `<requirements>`, `<job_stories>`).

### 2.2 Quality Gates & Traceability
- **Gate 1:** MECE Structure confirmed (No overlapping boundaries).
- **Gate 2:** Epistemic Labeling applied to 100% of findings.
- **Gate 3:** Confidence Threshold >= 0.85 reached before termination.

---

## MODULE 3: Fluency Assessment Battery

This battery is designed to assess technical fluency and architectural judgment.

### Part A: Multiple Choice Questions (Rapid Fluency)
*Target time: 10 minutes.*

**1. When enforcing strict typing in Python (`ty` / `basedpyright`), how do you correctly handle a dynamic, deeply nested JSON response from an undocumented API like `ffmpeg` where writing a complete `TypedDict` is impossible?**
a) Use `try/except` blocks around all property accesses.
b) Cast the response to `typing.Any` or utilize the `replace-imports-with-any` configuration in `pyproject.toml` to explicitly isolate the dynamic boundary.
c) Write a Python script to auto-generate the classes at runtime.
d) Disable type checking globally for that module.

**2. In `DatabaseTestFixture.cs`, why is `Guid.NewGuid():N` appended to the Postgres database name?**
a) To satisfy PostgreSQL naming conventions for temporary schemas.
b) To guarantee state isolation and prevent test data leakage when running test classes in parallel.
c) To bypass Testcontainers caching limitations.
d) To enable Entity Framework's automatic query batching.

**3. According to the Research Interviewer protocol, if an interviewee provides an answer that depends heavily on how a specific term is defined by their organization, what uncertainty tag must be applied?**
a) EPISTEMIC
b) ALEATORY
c) MODEL
d) ASSUMPTIVE

**4. You are syncing 5,000 Last.fm scrobbles using C# `Task.WhenAll`. EF Core throws an `InvalidOperationException: A second operation was started on this context`. What is the precise architectural fix?**
a) Wrap the `DbContext.SaveChangesAsync()` inside a `lock(db)` block.
b) Change the database provider to `UseInMemoryDatabase()`.
c) Resolve a transient `IDbContextFactory` per task to ensure thread safety, or await the HTTP calls concurrently but execute the database inserts sequentially.
d) Increase the `CommandTimeout` in the connection string.

### Part B: 30-Minute Deep Interview (Essay & Debug Scenarios)
*Target time: 30 minutes.*

**Scenario 1: System Debugging (The Flaky E2E Test)**
*Context:* You have a Python Playwright script extracting data from a web UI. The test passes 80% of the time locally, but fails 50% of the time in CI (Docker container). The logs show `TimeoutError: element .submit-btn not visible after 10000ms`.
*Prompt:* Write a structured, MECE (Mutually Exclusive, Collectively Exhaustive) debugging plan.
*Requirements:*
1. Categorize your investigation into 3 distinct dimensions (e.g., Environment, Network, Application State).
2. Detail exactly how you will identify if the uncertainty is EPISTEMIC (we just don't know the state) or ALEATORY (inherent network randomness).
3. Provide the concrete Playwright code adjustment you would implement to wait for network idle or DOM stabilization rather than arbitrary timeouts.

**Scenario 2: Architectural Essay (Shift-Left Quality in Python)**
*Context:* You are hired as the Lead SDET for a Python team that currently uses `pip` and relies solely on E2E Selenium tests that run nightly.
*Prompt:* Draft a proposal to implement the "C# Rigor" you demonstrated in your portfolio.
*Requirements:*
1. Explain how you will migrate them to `uv`.
2. Detail how you will implement a reflection/AST-based "Guard Test" in Python (similar to `SmokeTests.cs`) to prevent developers from using `time.sleep()` in their automation scripts.
3. Defend your choices against a developer who says "Python is supposed to be flexible, you are making it too rigid."

**Scenario 3: Requirements Elicitation (Research Interviewer Protocol)**
*Context:* A Product Manager tells you: *"We need to test the new video ingestion pipeline. Make sure it works fast."*
*Prompt:* Apply the Research Interviewer workflow.
*Requirements:*
1. Formulate one **Grand Tour** question to establish the landscape.
2. Formulate one **Devil's Advocate** question to stress-test their definition of "works fast".
3. Write the exact `<job_story>` XML block (matching `CONTRACT-01`) that would result from defining the non-functional requirement for processing speed.
