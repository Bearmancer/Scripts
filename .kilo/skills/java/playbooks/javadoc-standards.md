---
name: java-docs
description: Javadoc documentation standards for LeetCode Solution.java files — format, tags, HTML rules, CI requirements. Use when generating or fixing doc comments.
---

# Javadoc Standards for LeetCode Solutions

## Format
- Use `/** */` Javadoc (NOT `///` markdown comments). IntelliJ renders Javadoc HTML natively.
- Javadoc must appear immediately before the `class Solution` declaration.

## Required Section Headings (CI enforced by javadoc-lint.yml)
- `<h1>LeetCode #{index}: {title}</h1>` — title with problem link
- `<h2>Problem</h2>` — problem description
- `<h2>Examples</h2>` — input/output examples
- `<h2>Constraints</h2>` — constraint list

## Allowed HTML Tags
All standard Javadoc HTML: `<p>`, `<strong>`, `<code>`, `<pre>`, `<ul>/<li>`,
`<sup>`, `<em>`, `<i>`, `<h1>`-`<h6>`, `<a>`, `<img>`.
Attributes are stripped by the scraper but harmless if they survive.

## Inline Code
Use `<code>text</code>` directly in Javadoc HTML (NOT `{@code}` — that's for
method-level `@param`/`@return` docs only).

## Code Blocks
Use `<pre><strong>Input:</strong> ...</pre>` for examples (LeetCode format).
The `<strong>` inside `<pre>` renders as bold monospace — intentional.

## CI Expectations
- The javadoc-lint.yml workflow validates section headings, LeetCode link, NeetCode link
- `javac -Xdoclint:all` validates HTML well-formedness
- Both pass when Javadoc is generated via the scraper

## AGENTS.md Constraints
- NEVER modify solution logic (method bodies, class structure)
- ONLY modify doc comments and file-level scaffolding
- Do NOT infer or generate solution-specific documentation
