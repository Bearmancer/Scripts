# Code Modernization Tasks

## 🟢 Safe Analysis & Refactoring (Auto-Run Friendly)

1. Assess places where ternary operator is nested/too complex and affects readability

2. Check places where switch expression was placed and if some other code structure would be cleaner

3. Check for exception handling in project if exception filter can be used for cleaner exception handling

4. Utilize modern collection expression syntax everywhere via `[<data>]`
   - Note: Dictionary initializers like `new Dictionary<string, object> { ["Key"] = value }` cannot use collection expressions

5. Ensure null operators are faithfully applied everywhere to prevent verbose null checks via `??=`, `?.`, `??`, and `?.=` (C#14)

6. Ensure usage of target-typed `new()` where possible and `var` where methods are used for initialization

7. Check why some places use `or` while others use `||` — any meaningful reasoning/stylistic choice?

8. Assess for ways to modernize: tuples, modern switch expressions (functional style like F#), multiple exception handling, `var`, and robust GlobalUsings to prevent file-level using statements

9. Ensure separation of "modules" via comments the way it is in Logger.cs

10. General assessment for additional modernization opportunities

## 🟡 Requires Review (May Need Confirmation)

11. Why is there no action inside the catch block when the JSON is malformed? (May require adding error handling logic)

____
