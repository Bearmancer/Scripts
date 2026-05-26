---
name: "java"
description: "All Java concerns: Java 17+/21+ development, Spring Boot, refactoring, code review, JVM troubleshooting, and Java code graph pipeline."
model: "Claude 4o Mini"
---

# Java

Use this skill for Java implementation, refactoring, architecture, and review across Spring-based services and libraries.

## When to Use

- Writing or reviewing Java 17+ / Java 21+ code
- Designing Spring Boot services, APIs, and integrations
- Refactoring methods, especially removing redundant parameters
- Enforcing Java coding standards and maintainable structure
- Debugging JVM, persistence, concurrency, or framework issues

## Core Reference
- [Javadoc Standards (LeetCode)](playbooks/javadoc-standards.md)

## Core Workflow

1. Clarify the goal, constraints, inputs, and output shape.
2. Identify the simplest correct Java/Spring pattern.
3. Prefer type safety, immutability, and explicit contracts.
4. Refactor toward cohesion: remove redundancy, reduce coupling, keep methods focused.
5. Validate with existing tests or the repo's standard checks.

## Modern Java Guidance

- Prefer records for immutable DTOs and value objects.
- Use sealed types when the hierarchy is closed and exhaustiveness matters.
- Use pattern matching for `instanceof` and `switch` where it improves clarity.
- Use virtual threads for blocking I/O workloads; do not expect them to help CPU-bound work.
- Prefer `var` only when the type is obvious from the right-hand side.
- Keep concurrency safe and explicit; use structured concurrency when available and appropriate.

## Spring and Architecture

- Prefer Spring Boot conventions over custom infrastructure unless there is a clear reason.
- Choose Spring MVC for traditional blocking web apps; choose WebFlux only when reactive flow is needed.
- Use Spring Data JPA for rich domain models; use JDBC/JdbcTemplate for straightforward SQL-heavy work.
- Use DTOs and projections at boundaries to avoid leaking entities across layers.
- Watch for N+1 queries, lazy loading outside transactions, and oversized service methods.

## Coding Standards

- Use PascalCase for classes and records, camelCase for methods and fields, UPPER_SNAKE_CASE for constants.
- Favor immutable fields and constructor injection.
- Keep parameter lists short; use value objects or request records when parameters grow.
- Use `Optional` for absent values from find-style methods; prefer `map` and `flatMap` over `get()`.
- Avoid raw types, broad catch blocks, and silent failures.
- Keep methods short, focused, and named for intent.

## Refactoring Rules

- Remove parameters that are unused, redundant, or derivable from fields, constants, or existing calls.
- Update all call sites when removing a parameter.
- Preserve behavior exactly unless the change is explicitly requested.
- Prefer extracting helpers over adding branching complexity.

### Remove Parameter Example

```java
// Removed cloudCluster because it is already available from the context object.
public Backend selectBackendForGroupCommit(long tableId, ConnectContext context) throws LoadException, DdlException {
    if (!Env.getCurrentEnv().isMaster()) {
        try {
            long backendId = new MasterOpExecutor(context)
                    .getGroupCommitLoadBeId(tableId, context.getCloudCluster());
            return Env.getCurrentSystemInfo().getBackend(backendId);
        } catch (Exception e) {
            throw new LoadException(e.getMessage());
        }
    }
    return Env.getCurrentSystemInfo()
            .getBackend(selectBackendForGroupCommitInternal(tableId, context.getCloudCluster()));
}
```

## Testing and Validation

- Keep changes compilable and consistent with project conventions.
- Add or update tests when behavior changes or risk is non-trivial.
- For refactors, verify all call sites and overloads.
- Prefer deterministic tests; avoid sleeps and hidden timing assumptions.

## Practical Review Checklist

- Is this the right abstraction level?
- Can any parameter, field, or helper be removed?
- Is the code readable without extra explanation?
- Are exceptions meaningful and domain-appropriate?
- Are transactions, lazy loading, and threading boundaries correct?
