---
applyTo: "**"
---

## Formatting

- Indent with tabs (display width: 4).
- Line endings: CRLF.
- Encoding: UTF-8.
- Trim trailing whitespace; every file ends with exactly one newline.

## Change Philosophy

- Make the smallest correct change that satisfies the requirement — do not refactor unrelated code.
- Delete dead code and unused imports in the same commit that introduces the change.

## Naming

- Use full, descriptive identifiers; avoid single-letter names except for trivial loop counters.
- Do not use abbreviations unless they are universally understood (`id`, `url`, `http`).

## Comments

- Omit comments that restate what the code does — the code is the documentation.
- Add a comment only when the *why* would not be obvious to a reader unfamiliar with the domain.
