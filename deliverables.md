# deliverables.md

## Phase 2: Interview Question Bank

### SECTION 1: Project comprehension
**Q:** This repository has a clear split between C# code and a Python toolkit. Walk me through the purpose of each and why you chose to implement specific features (like audio/video operations or PDF parsing) in one language versus the other.
**Targets:** Verifies they understand the core architectural boundary of the project and why they chose the toolset for specific jobs (e.g., Python for data parsing/media, C# for robust orchestration/EF Core).
**Strong answer would include:**
- Explanation that Python excels at scripting, file operations (`ffmpeg-python`, `Pillow`, OCR integration), and interacting with various text formats (e.g., ePubs, PDFs).
- Mentioning C# is used for type-safe database interactions (Entity Framework Core 10), data synchronization logic (Last.fm, YouTube), and complex orchestration.
- Reference to specific orchestration patterns (e.g., `ScrobbleSyncOrchestrator`, `YouTubePlaylistOrchestrator`).
**Follow-up if they nail it:** How do these two halves of the repository communicate with each other, or are they entirely disjoint workflows?

**Q:** Your database relies on PostgreSQL via Docker. How do you handle schema changes and local environment setup when developing?
**Targets:** Checks if they actually wrote the database migrations and understand the local dev flow.
**Strong answer would include:**
- Mentions using Entity Framework Core Migrations (e.g., `20260523102551_InitialCreate.cs`).
- Describes the `docker-compose.yml` setup and the `PGCONNSTR` environment variable needed for connection.
- Understands that tests use a real instance, avoiding in-memory DB caveats.
**Follow-up if they nail it:** What challenges did you face syncing local DB state across different components, or how do you ensure the database is clean before tests run?

**Q:** What is the purpose of the `.kilo` and `.kiro` directories, along with the `AGENTS.md` and `AI` directory?
**Targets:** Verifies they are the author and didn't just copy boilerplate. Understands the AI-driven or plan-driven development workflow used.
**Strong answer would include:**
- Explanation that the project uses a Consolidated TDD Plan (CPM) managed in `.kilo/plans/plan.md` with discrete phase files.
- Mentioning `AGENTS.md` provides instructions for AI agents (like Claude/Gemini/Copilot) used during development.
- Describing the `AI/plans` and `AI/research` directories as documentation and task tracking for AI-assisted work.
**Follow-up if they nail it:** How has using AI agents changed the way you structure your project files or write your test suite?

**Q:** You have several external integrations (Last.fm, YouTube, Discogs, MusicBrainz, Fibery). Choose one and describe the authentication flow and error handling you implemented.
**Targets:** Probes understanding of third-party API integration and resilience.
**Strong answer would include:**
- Detail on a specific service (e.g., `YouTubeService` using Google Credentials, or `LastFmService`).
- Mentioning the `RepositoryResilienceFactory.cs` or Polly for retry logic and resilience against rate limits or transient errors.
- Discussion on how secrets are managed (e.g., `.env` file, `auth.json`).
**Follow-up if they nail it:** If you hit a hard rate limit on the YouTube API that requires waiting an hour, how does your system handle that state without losing progress?

**Q:** Looking at `Program.cs`, you are using Spectre.Console (`CommandApp`). Walk me through how a command like `sync all` is routed from the entry point to the actual execution logic.
**Targets:** Architectural recall. Tracing execution flow from UI to domain logic.
**Strong answer would include:**
- Mentions `SpectreTypeRegistrar` and Dependency Injection (`ServiceCollection`).
- Describes the branching logic in `app.Configure` (e.g., `sync` -> `all` mapping to `SyncAllCommand`).
- Explains how the command resolves its dependencies (like `SyncLastFmCommand` or orchestrators) and executes.
**Follow-up if they nail it:** Why use a DI container in a console app instead of just instantiating the services directly in the command?

### SECTION 2: Testing strategy and design
**Q:** You're using TUnit for your C# tests instead of xUnit, NUnit, or MSTest. Why did you choose TUnit, and what specific features are you leveraging?
**Targets:** Evaluates their understanding of the testing framework chosen and the reasoning behind it.
**Strong answer would include:**
- Explains TUnit is a modern, fast, or specific testing framework for .NET.
- Mentions global test skip enforcement via reflection (e.g., `SmokeTests.cs` failing if `Skip` or `Ignore` is used).
- Discusses integration with `Microsoft.Testing.Platform` in `global.json`.
**Follow-up if they nail it:** How does TUnit handle test parallelization compared to xUnit, especially given your database integration tests?

**Q:** Your database tests (e.g., `DatabaseTestFixture.cs`) use a real PostgreSQL database rather than an in-memory provider. Why make this architectural choice, and what are the trade-offs?
**Targets:** Assesses understanding of integration vs. unit testing and the limitations of in-memory databases.
**Strong answer would include:**
- Acknowledges that EF Core In-Memory provider doesn't accurately simulate relational features (constraints, specific SQL generation).
- Mentions the use of the `docker-compose.yml` file to spin up a real Postgres instance.
- Discusses the trade-off of test speed and complexity (managing DB state between tests) versus accuracy.
**Follow-up if they nail it:** I noticed a comment in `DatabaseTestFixture.cs` saying "No Testcontainers — the Docker Compose Postgres is already running." Why did you choose to rely on an external compose file rather than spinning up the container programmatically using Testcontainers?

**Q:** I see a strict rule in your memory/documentation: "Tests in the C# codebase must never be skipped; missing dependencies or fixtures should trigger an explicit failure/exception rather than a graceful skip." Why enforce this?
**Targets:** Testing philosophy and CI/CD discipline.
**Strong answer would include:**
- Explanation that skipped tests often become forgotten technical debt ("broken window" theory).
- Argument that if an environment lacks dependencies, the build/test pipeline should fail hard so the environment is fixed, rather than silently ignoring coverage.
- Mentions the reflection-based guard in `SmokeTests.cs`.
**Follow-up if they nail it:** What happens if a flaky third-party API is down? Should the build fail, or should those tests be mocked?

**Q:** Looking at your repository, there's a robust C# test suite (`csharp/tests/Scripts.Tests`), but the Python `toolkit` has no `tests/` directory present in the standard tree. Why is that?
**Targets:** Probing a visible gap in coverage and understanding of cross-language testing priorities.
**Strong answer would include:**
- Acknowledging the gap honestly (e.g., "The Python code was built primarily as wrapper scripts or exploratory tools, while the core business logic resides in C#").
- Discussing that while `pytest` configuration exists in memory/docs (using `.pytest_tmp`), the actual tests haven't been implemented or committed yet.
- Explaining how they manually verified the Python scripts.
**Follow-up if they nail it:** If you had a week to add tests to the Python toolkit, where would you start and what mocking strategy would you use for things like `ffmpeg` or `exiftool`?

**Q:** You have several "Guard" tests (e.g., `EF11GuardTests.cs`, `EditorConfigEf10RulesTests.cs`). What is the purpose of these architectural tests?
**Targets:** Understanding of architecture tests and enforcing coding standards programmatically.
**Strong answer would include:**
- Explanation that these tests ensure developers (or AI agents) don't introduce deprecated patterns (like EF 10 patterns in an EF 11 migration).
- Mentioning that it acts as a programmatic linter to enforce project-specific rules that standard analyzers might miss.
- Shows a proactive approach to maintaining codebase quality over time.
**Follow-up if they nail it:** How do you balance between writing a custom Roslyn Analyzer versus a simple regex-based unit test for these guards?

**Q:** Your integration tests drop and recreate the database on initialization (in `DatabaseTestFixture.cs`). How does this impact test suite performance, and how do you isolate state between individual test methods?
**Targets:** Practical knowledge of test data management and performance tuning.
**Strong answer would include:**
- Acknowledging that DB recreation is slow but guarantees a clean schema.
- Explaining that individual test classes might share the fixture, but individual tests need unique data (e.g., using Guids for IDs like `"test-release-" + Guid.NewGuid()`) to avoid cross-test contamination.
- Understanding of transactions (e.g., wrapping tests in a rollback transaction) if they use it.
**Follow-up if they nail it:** If the test suite grows to 1000+ DB tests, dropping the DB per class will be too slow. How would you refactor the test data strategy?

### SECTION 3: Technical depth — tools and frameworks
**Q:** You're using Entity Framework Core (version 10.0.8 according to your csproj). Talk to me about your `ScriptsDbContext` configuration. How did you handle complex types or JSON columns, like `DiscogsNotes` or `ArticleContent`?
**Targets:** Deep EF Core knowledge and handling of modern data types in Postgres.
**Strong answer would include:**
- Discussion of EF Core's JSON column mapping or owned entity types (e.g., `OwnsOne` or `ToJson()`).
- Mentions how `Npgsql.EntityFrameworkCore.PostgreSQL` handles `jsonb` under the hood.
- References the `FixJsonDocumentModel` migration.
**Follow-up if they nail it:** How do you handle querying against those JSON columns? Does EF Core translate your LINQ queries efficiently into Postgres JSON operators?

**Q:** Your Python toolkit uses `uv` for dependency management instead of standard `pip` or `poetry`. Why did you make this switch, and how does it affect your workflow?
**Targets:** Awareness of modern Python tooling and its benefits.
**Strong answer would include:**
- Mentions `uv` is significantly faster because it's written in Rust.
- Explains that the project avoids a standard `.venv` directory in favor of `.uv` (configurable via `UV_PROJECT_ENVIRONMENT`).
- Discusses how `uv.lock` ensures deterministic builds similar to `package-lock.json`.
**Follow-up if they nail it:** What compatibility issues, if any, have you run into using `uv` compared to standard `pip`, especially in CI?

**Q:** You use `Spectre.Console` extensively in the C# application. How do you handle long-running background tasks, like the Last.fm sync, while keeping the console UI responsive?
**Targets:** Knowledge of asynchronous programming in C# and console UI threading.
**Strong answer would include:**
- Mentions `SyncProgressRenderer` and `SyncProgressTracker`.
- Explains the use of `async/await` and `Task.WhenAll` to run the actual sync logic concurrently.
- Discusses updating the Spectre `Progress` or `Status` context safely from background threads.
**Follow-up if they nail it:** How do you handle graceful cancellation (e.g., `Ctrl+C`) during a sync operation without corrupting the database state?

**Q:** The Python `toolkit` uses `playwright` for some web automation or scraping. Given that you also have API integrations (like Last.fm and YouTube), what scenarios forced you to use Playwright instead of a direct HTTP client like `requests`?
**Targets:** Understanding when to use headless browsers vs API clients.
**Strong answer would include:**
- Explains that Playwright is necessary for sites heavily reliant on client-side rendering (React/SPA) where the data isn't in the initial HTML payload.
- Mentions bypassing basic anti-bot protections or handling complex auth flows (OAuth consent screens) that are hard to script with raw requests.
- Discusses the overhead of Playwright vs `requests`.
**Follow-up if they nail it:** How do you manage Playwright's browser binaries in a Dockerized or CI environment?

**Q:** You implemented several OCR providers (Azure, Google Vision, Tesseract) behind an `IOcrProvider` interface in C#. How do you handle the different response formats and latency characteristics of these providers?
**Targets:** Interface design (Strategy pattern) and handling heterogeneous external dependencies.
**Strong answer would include:**
- Explains the strategy pattern: the core app depends on `IOcrProvider`, not the specific implementation.
- Mentions mapping vendor-specific JSON responses into a unified internal model (e.g., `OcrTextCleanup`).
- Discusses handling network latency for Azure/Google vs local execution time for Tesseract.
**Follow-up if they nail it:** If you wanted to fallback to Tesseract only if Azure fails or rate-limits you, how would you design that circuit breaker in your DI container?

### SECTION 4: Problem-solving and trade-offs
**Q:** In your C# code, there's a directive to "Eliminate sequential N+1 API calls inside loops... by mapping to a collection of tasks and using `Task.WhenAll`". Can you walk me through a specific place you implemented this and the challenge of managing concurrent database contexts?
**Targets:** Concurrency, N+1 problem, and EF Core DbContext lifecycle.
**Strong answer would include:**
- Identifies a sync orchestrator (e.g., fetching 100 YouTube videos).
- Mentions that EF Core `DbContext` is *not* thread-safe, so you cannot use the same context inside `Task.WhenAll`.
- Explains the solution: either using a `IDbContextFactory` to spawn a context per task, or gathering all data concurrently and *then* saving sequentially.
**Follow-up if they nail it:** What happens if one of the 50 concurrent tasks fails? Does `Task.WhenAll` throw immediately, and how do you capture the other 49 successes?

**Q:** You chose to store state on the filesystem (`state/` directory with `pristine`, `youtube`, etc. holding JSON files) despite having a PostgreSQL database. Why duplicate this data or split the storage strategy?
**Targets:** Understanding data architecture and caching strategies.
**Strong answer would include:**
- Explains that the JSON files act as a raw cache or pristine source of truth from APIs (e.g., YouTube playlist states) to avoid re-fetching and hitting API limits.
- Mentions the database is used for normalized querying, relationships, and application state.
- Discusses the trade-off of ensuring the filesystem cache and the database stay in sync.
**Follow-up if they nail it:** How do you handle a scenario where a script crashes halfway through updating the DB, leaving the filesystem out of sync with Postgres?

**Q:** In `ScriptsDbContextModelSnapshot.cs` and the migrations, there's a lot of churn around entity structure (e.g., `AddDomainEntities`, `FixJsonDocumentModel`, `AddReleaseProgress`). Looking back, what was the biggest architectural pivot you had to make in your database design?
**Targets:** Self-reflection on database normalization and evolving requirements.
**Strong answer would include:**
- Describes a specific refactoring (e.g., `EntityRefactoring/AlbumMbidRemovalTests.cs`).
- Explains why the initial design failed (e.g., relying on MusicBrainz IDs that turned out to be inconsistent, forcing a move to a different primary key or composite key).
- Shows understanding of how migrations handle data loss during schema changes.
**Follow-up if they nail it:** If you had to deploy this app to production today with zero downtime, how would you handle a migration that renames a heavily-used column?

**Q:** The Python code has strict type checking enforced via `ty` (`tool.ty.rules all = "error"` in `pyproject.toml`). Why enforce this level of strictness in Python, a dynamically typed language, especially for a personal toolkit?
**Targets:** Code quality philosophy and static typing in Python.
**Strong answer would include:**
- Argues that type hints prevent runtime `AttributeError`s and make the codebase self-documenting.
- Mentions that AI agents and IDEs perform much better with strict types.
- Acknowledges the overhead of typing dynamic responses (like `ffmpeg` or `pylast`), referencing the `replace-imports-with-any` configuration.
**Follow-up if they nail it:** How do you handle typing for deeply nested JSON responses from undocumented APIs where creating `TypedDict`s feels like overkill?

### SECTION 5: Entry-level growth indicators
**Q:** You've built a complex system using C#, Python, Postgres, and Docker. If you were starting this project over from scratch today, what is one major architectural decision you would change?
**Targets:** Hindsight, maturity, and ability to critique one's own work.
**Strong answer would include:**
- A concrete technical regret (e.g., "I would have used a single language to simplify CI/CD," or "I would have used a lightweight document store instead of Postgres since I rely heavily on JSON").
- Clear reasoning based on pain points experienced during development.

**Q:** I noticed the Python side lacks a formal test suite. As an SDET, how would you prioritize building out the testing strategy for the Python toolkit, and what framework would you use?
**Targets:** Pragmatic approach to technical debt and testing.
**Strong answer would include:**
- Proposes using `pytest`.
- Prioritizes testing the most complex or error-prone logic first (e.g., file parsers or data transformers) rather than simple API wrappers.
- Mentions using `pytest.mark.parametrize` for OCR/data extraction edge cases and `tmp_path` for filesystem isolation.

**Q:** What was the most difficult bug you encountered while building this framework, and what debugging tools or techniques did you use to track it down?
**Targets:** Hands-on debugging skills and resilience.
**Strong answer would include:**
- A specific example (e.g., EF Core tracking issue, N+1 query performance bug, Docker network routing issue).
- Mentions reading logs, using breakpoints, isolating the issue in a unit test, or leveraging the `.kilo/plans` structure to systematically trace the root cause.


---

## Phase 3: Resume Decomposition

### Project title and one-line summary
**Project Title:** Multi-Language Data Synchronization & Processing Framework
**Summary:** Designed and engineered an end-to-end framework integrating C# (.NET 10) and Python to orchestrate, synchronize, and analyze large-scale media and document datasets via PostgreSQL and third-party APIs.

### Decomposition into resume-ready components

**Component 1: Test-Driven Data Architecture**
*What it demonstrates:* Knowledge of modern C#, Entity Framework Core, and robust integration testing.
*Resume bullet:* Architected a normalized PostgreSQL database using Entity Framework Core 10, validating schema migrations and business logic through [X]+ integration tests using TUnit and Docker Compose.
*Talking point:* "I needed a reliable way to store unstructured data from APIs, so I utilized EF Core's JSON column mapping. To ensure my data access layer was rock-solid, I built an integration test fixture that spins up a real Postgres database via Docker, ensuring my tests interact with the actual database engine rather than an inaccurate in-memory mock."

**Component 2: Resilient API Orchestration**
*What it demonstrates:* Handling concurrency, rate limits, and third-party integrations (Last.fm, YouTube).
*Resume bullet:* Engineered resilient synchronization orchestrators utilizing C# `Task.WhenAll` concurrency and Polly-based retry policies, reducing data fetch times by [X]% while adhering to strict third-party API rate limits.
*Talking point:* "When syncing thousands of YouTube videos and Last.fm scrobbles, sequential API calls were too slow. I refactored the orchestrators to run concurrently. To handle inevitable transient errors and rate limits without crashing the app, I implemented a resilience factory to automatically back off and retry failed requests."

**Component 3: Extensible Python Media Toolkit**
*What it demonstrates:* Cross-language competency, automation, and strict typing.
*Resume bullet:* Developed a strictly-typed Python automation toolkit utilizing `uv` for high-speed dependency resolution, leveraging `ffmpeg-python` and Playwright for media processing and headless data extraction.
*Talking point:* "While C# handled the core orchestration, I used Python for heavy lifting in media processing and OCR. I enforced strict static typing using `ty` and managed dependencies with `uv` to ensure the scripts were as robust and deterministic as the main .NET application."

**Component 4: Programmatic Code Quality Enforcement**
*What it demonstrates:* Shift-left quality mindset and architectural testing.
*Resume bullet:* Implemented custom reflection-based architectural tests (Guard tests) to programmatically enforce coding standards, preventing deprecated EF patterns and ensuring 100% adherence to global testing policies.
*Talking point:* "To prevent regressions and enforce team standards automatically, I wrote unit tests that inspect the codebase itself. For example, one test uses reflection to fail the build if any test is marked to be 'Skipped', enforcing a strict 'fix it or delete it' philosophy for the test suite."

### Skills and tools to list
- **Primary tools/frameworks:** C# 12/.NET 10, Entity Framework Core, TUnit, Python 3.14, PostgreSQL, Docker/Testcontainers, Playwright, Spectre.Console, uv.
- **Testing types demonstrated:** Integration Testing (DB fixtures), Unit Testing, Architectural/Guard Testing, TDD (Test-Driven Development).
- **Do not claim:** E2E UI Testing (Playwright is used in Python, but there is no formal E2E test suite present in the repo), CI/CD Pipeline Configuration (no `.github/workflows` or similar files exist in the repo), Python Test Automation (no `pytest` suite exists in the codebase).

---

## Phase 4: README Recommendation

### Current README assessment
*Verdict:* **Missing.** The repository currently lacks a `README.md` entirely.
*Impact:* This severely hurts the candidate's chances. An SDET hiring manager relies on the README as the front door to the project. Without it, a complex multi-language repo with AI-agent tracking directories (`.kilo`, `AI/`) looks like an unapproachable dump of personal scripts rather than a professional engineering portfolio.

### Ideal README structure for this repo

**1. Project Title & Overview**
*What to put in it:* A clear, 2-sentence description of what this repository actually does (e.g., "A personal media and document synchronization engine that aggregates data from YouTube, Last.fm, and local files into a PostgreSQL database").
*Why it matters to an SDET interviewer:* Shows you can communicate business value and high-level architecture succinctly, rather than just listing technologies.

**2. Architecture & Tech Stack**
*What to put in it:* A brief list or diagram explaining the split: C# (.NET 10) for orchestration/DB, Python (managed by `uv`) for media/OCR processing, and PostgreSQL via Docker for state.
*Why it matters to an SDET interviewer:* Proves intentional design. It answers the immediate question: "Why are there two languages in this repo?"

**3. Testing Strategy**
*What to put in it:* Explain the use of TUnit for C#, the integration test approach using Docker Compose for the Postgres database, and the "Guard" architectural tests that enforce coding standards. Be honest about the current state of Python testing.
*Why it matters to an SDET interviewer:* This is the most critical section for an SDET role. It shows you think deeply about *how* to test, not just how to write code. Highlighting the "No skipped tests" policy shows senior-level discipline.

**4. Local Setup & Running Tests (The 5-Minute Guide)**
*What to put in it:* Exact, copy-pasteable bash commands to spin up the environment and run the tests.
```bash
# 1. Start the database
docker compose up -d postgres
# 2. Copy environment file
cp .env.example .env
# 3. Run the C# test suite
dotnet test csharp/tests/Scripts.Tests/
```
*Why it matters to an SDET interviewer:* If an interviewer cannot run your tests locally in under 5 minutes, they will assume your testing framework is brittle, undocumented, or works "on my machine" only.

**5. Future Improvements / Roadmap**
*What to put in it:* Acknowledge technical debt. Specifically mention adding a `pytest` suite for the Python toolkit and setting up GitHub Actions for automated CI execution.
*Why it matters to an SDET interviewer:* Demonstrates self-awareness, maturity, and the ability to prioritize engineering tasks. It turns a weakness (missing Python tests) into a talking point about continuous improvement.

### README anti-patterns to avoid
- **Do not invent a product.** Don't write the README as if this is a SaaS startup. Be honest that it's a personal toolkit, but present it with professional engineering standards.
- **Do not ignore the AI folders.** The `.kilo`, `.kiro`, and `AI` folders dominate the root directory. Add a small note explaining that this project utilizes an AI-driven planning architecture (Consolidated TDD Plan). Otherwise, it looks like messy, generated boilerplate.
- **Do not list tools you can't defend.** Don't list Testcontainers prominently in the README if the tests actually rely on the `docker-compose.yml` being manually spun up (as noted in `DatabaseTestFixture.cs`). Be precise about your dependencies.
