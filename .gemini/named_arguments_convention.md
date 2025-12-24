# C# Named Arguments Convention

## Rule
Use named arguments for method calls with **3+ parameters**.

## Quick Reference
```csharp
// ❌ Forbidden
CreateUser("Alice", 30, true, null, "admin");

// ✅ Required  
CreateUser(name: "Alice", age: 30, isActive: true, department: null, role: "admin");
```

## Exceptions
- Well-known 1-2 param methods: `Console.WriteLine()`, `File.Exists()`
- Fluent/builder chains: `.Where().OrderBy().Take()`
- Collection initializers: `[1, 2, 3]`

---

## Additional AI Instruction Improvements

### Suggested Additions to MEMORY[user_global]

```markdown
## C# Style Extensions

### Method Calls
- Using positional arguments for methods with 3+ parameters — use named arguments.
- Mixing positional and named arguments — use all named or all positional (prefer named for 3+).

### Async/Await
- Forgetting `ConfigureAwait(false)` in library code — add for non-UI contexts.
- Using `.Result` or `.Wait()` — always use `await`.

### LINQ
- Complex multi-line LINQ without line breaks — format each clause on its own line:
  ❌ `items.Where(x => x.Active).OrderBy(x => x.Name).Select(x => x.Id).ToList()`
  ✅ 
  ```csharp
  items
      .Where(x => x.Active)
      .OrderBy(x => x.Name)
      .Select(x => x.Id)
      .ToList()
  ```

### String Formatting
- Using `string.Format` or `+` concatenation — prefer interpolation: `$"Hello {name}"`.
- Not escaping special chars in Spectre.Console — use `Console.Escape()` wrapper.

### Null Handling
- Using `if (x != null)` — prefer pattern matching: `if (x is { })` or `x is not null`.
- Not using null-coalescing — prefer `??` and `??=`.
```