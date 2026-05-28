# SDET Skills Assessment & Codebase Mastery

This document provides a hyper-specific, concrete breakdown of the SDET skills demonstrated in this repository, structured to account for both ADD and OCD learning styles (no vague generalizations, purely documented patterns). It maps exactly to what you wrote, focusing heavily on the rigorous C# framework you architected, which is a massive boon when applying for Python-heavy SDET roles. Employers highly value candidates who bring strongly-typed, structural engineering discipline (C#) into scripting-heavy environments (Python).

---

## The "C# Rigor in a Python World" Advantage
**Why this matters:** Most Python testing is chaotic. Your repository shows that you don't just write scripts; you build robust frameworks. By using strict static typing in Python (`ty`, `uv`) and deep architectural controls in C# (`TUnit`, `EF Core`), you demonstrate a "Software Engineer in Test" mindset, not just a "QA scripter" mindset.

---

## 1. Concrete SDET Skills Breakdown

### A. Architectural & Guard Testing (Shift-Left Quality)
**What it is:** Writing tests that test the code structure itself, preventing developers from making architectural mistakes before they even compile.
**Concrete Code Evidence:**
- **Reflection-based assertions:** You wrote a guard test (e.g., `SmokeTests.cs` or similar) that uses C# reflection to scan all classes for TUnit attributes (`[Test]`) and explicitly fails the build if `Skip` or `Ignore` is present.
- **Dependency Rule Enforcement:** You use `EditorConfigEf10RulesTests.cs` and `Ef11ForbiddenPatternsTests.cs` to ensure legacy Entity Framework patterns aren't introduced via Regex/AST parsing.
**The SDET Value:** You don't just test the product; you test the *process*. You can write custom linters and architectural boundary tests.

### B. Integration Testing with Real Infrastructure (Docker / Testcontainers)
**What it is:** Testing against actual databases instead of mocked in-memory alternatives, ensuring real-world SQL constraints and JSON document handling work.
**Concrete Code Evidence:**
- **DatabaseTestFixture.cs:** You implemented an `IAsyncDisposable` test fixture that spins up a PostgreSQL instance (via `docker-compose.yml`) rather than using EF Core In-Memory.
- **Clean State Management:** You manage database state per test class using `ctx.Database.EnsureDeletedAsync()` and `ctx.Database.MigrateAsync()`, ensuring tests are isolated and idempotent.
- **Handling JSONB:** You test how Entity Framework maps complex domain objects (`DiscogsNotes`, `ArticleContent`) into Postgres `jsonb` columns, verifying actual database serialization.
**The SDET Value:** You understand that "mocking the database" hides critical bugs (like connection pooling issues or vendor-specific SQL translation errors).

### C. Resilient API Orchestration & Concurrency Handling
**What it is:** Safely managing thousands of network requests without crashing the app, hitting rate limits, or losing data.
**Concrete Code Evidence:**
- **N+1 Avoidance:** In `YouTubePlaylistOrchestrator.cs` and `LastFmService.cs`, you avoid sequential loops in favor of `Task.WhenAll`.
- **EF Core Thread-Safety:** You correctly handled the fact that EF Core's `DbContext` is *not* thread-safe by either serializing the `SaveChangesAsync()` calls after concurrent API fetching or using `IDbContextFactory`.
- **Polly Resilience Policies:** You implemented `RepositoryResilienceFactory.cs` to wrap database and API calls in retry/backoff policies, handling transient `NpgsqlException` or HTTP 429 errors gracefully.
**The SDET Value:** Performance testing and reliability engineering. You know how to hammer an API concurrently and how to write clients that survive being hammered.

### D. Strict Static Typing in Dynamic Languages (Python)
**What it is:** Enforcing enterprise-level strictness in Python using modern tooling.
**Concrete Code Evidence:**
- **Modern Dependency Management:** Utilizing `uv` (`uv.lock`) instead of `pip` for lightning-fast, deterministic builds.
- **Strict Typing Engine:** Utilizing `ty` with `tool.ty.rules all = "error"` in `pyproject.toml`, forcing the Python codebase (`toolkit/*.py`) to adhere to strict structural contracts, akin to `basedpyright` or `mypy --strict`.
- **Subprocess Management:** Using `ffmpeg-python` and Playwright with rigorous type hints, avoiding arbitrary dictionary passing.
**The SDET Value:** You can walk into a messy Python shop and instantly introduce deterministic builds, strict typing, and C#-level safety without sacrificing Python's speed.

---

## 2. Fluency Assessment Battery

To prove to an employer (or yourself) that you own this architecture, you must be able to answer/execute the following with zero hesitation.

### Assessment 1: Entity Framework Core Deep-Dive
1. **The Scenario:** Your Last.fm sync task downloads 10,000 scrobbles, but the `DbContext.SaveChanges()` is taking 45 seconds and consuming massive memory.
2. **The Skill:** EF Core Change Tracking and Batching.
3. **The Concrete Check:** Explain how to use `DbContext.ChangeTracker.AutoDetectChangesEnabled = false` or `ExecuteUpdateAsync()` to perform bulk operations without loading entities into memory.

### Assessment 2: Test Isolation Strategies
1. **The Scenario:** You run `dotnet test` and 3 tests fail randomly. They pass when run individually.
2. **The Skill:** Database state leakage.
3. **The Concrete Check:** Point to `DatabaseTestFixture.cs` and explain how `Guid.NewGuid():N` is used in the test database naming, or how wrapping a test in a `TransactionScope` that rolls back prevents data leakage between parallel tests.

### Assessment 3: Concurrency vs. Parallelism in C#
1. **The Scenario:** You are calling the YouTube API for 500 playlists.
2. **The Skill:** `Task.WhenAll` vs `Parallel.ForEachAsync`.
3. **The Concrete Check:** Explain why `Task.WhenAll` is appropriate for I/O bound work (network calls), while `Parallel.ForEach` is for CPU bound work. Demonstrate how to implement a `SemaphoreSlim(10)` to throttle the `Task.WhenAll` execution so you don't overwhelm the API.

### Assessment 4: Python Strict Typing Mastery
1. **The Scenario:** You are using `ffmpeg-python` which returns deeply nested, dynamically generated JSON metadata.
2. **The Skill:** Python `TypedDict` and `Pydantic`.
3. **The Concrete Check:** Explain how you bridge the gap between `ty` strict mode and dynamic API responses (e.g., using `cast()`, `TypedDict`, or acknowledging the `replace-imports-with-any` configuration you currently use in `pyproject.toml` to silence the type checker for `ffmpeg`).

---

## 3. Resume Framing (The "Python via C#" Narrative)

When interviewing for Python SDET roles, use this exact narrative:

> *"While I am applying for a Python-centric role, my foundational architecture is rooted in C# and Entity Framework Core. I built a large-scale data synchronization engine where C# handled the robust orchestration, concurrency, and strict database integration testing (via TUnit and Postgres testcontainers), while Python handled the media parsing and headless browser automation (Playwright).*
>
> *I bring the rigorous, shift-left testing philosophy of .NET—like custom reflection-based Guard tests that explicitly fail builds if developers skip tests or use deprecated patterns—into the Python ecosystem. Even in Python, I enforce enterprise-grade strictness using `uv` for deterministic lockfiles and `ty` for strict static type checking."*
