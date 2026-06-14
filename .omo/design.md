# Universal Serilog Tracing Design

## Overview
The goal is to provide maximalist method observability (entry, exit, duration, and exception tracking) across the entire codebase without introducing external Dashboards, without violating the `internal static class` architecture rules defined in `AGENTS.md`, and with minimal boilerplate.

## Selected Approach: The 1-Liner (`Log.Track`)
We will expand the existing `Log` static wrapper to include a `MethodTracker` struct that implements `IDisposable`.

### Implementation Details
1. **Core Extension:**
   - Add a `MethodTracker` struct to `src/Core/Log.cs`.
   - Add a `Log.Track(object? args = null, [CallerMemberName] string methodName = "")` method.
2. **Behavior:**
   - **Initialization:** Logs a `Verbose`/`Trace` message: `[VRB] -> Entering {MethodName}. Args: {Args}` and starts a stopwatch.
   - **Disposal:** Logs a `Debug` message upon method completion: `[DBG] <- Exiting {MethodName}. Duration: {ElapsedMs}ms`. 
   - **Exceptions:** If the method crashes, the global exception handler will log the stack trace, but the `Track` block will still log the elapsed duration before the crash.
3. **Application:**
   - Apply `using var _ = Log.Track(new { arg1, arg2 });` to the top of significant methods across the codebase, specifically targeting the 5 core Azure services (`AzureAuth`, `AzureDocumentIntelligenceService`, `AzureOpenAIService`, `AzureTranslationService`, `AzureVisionService`) and key Orchestrators/CLI Commands.
   - Refactor existing manual `Console.WriteLine` trace logs to use this pattern.

## Non-Goals
- We will NOT use `[LoggerMessage]` (Source Generators) as it requires too much boilerplate for our needs.
- We will NOT use Aspect-Oriented Programming (AOP) like Metalama or Fody IL Weaving to maintain explicit code and preserve predictable stack traces.
- We will NOT implement Dependency Injection (DI) logging proxies, as that violates the `AGENTS.md` static class rule.

## Self-Review Checklist
- [x] Internal consistency: The architecture matches the repository's strict rules.
- [x] Scope: Highly focused on the `Log.cs` extension and applying the 1-liner to Azure services.
- [x] Ambiguity: The exact code pattern is defined.
