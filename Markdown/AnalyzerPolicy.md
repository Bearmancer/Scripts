# Analyzer Warning Policy

## Configuration

```xml
<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
<AnalysisLevel>latest-recommended</AnalysisLevel>
```

---

## Warnings Fixed

### CA2201 — Exception type not sufficiently specific

**Fix**: Use built-in specific exceptions like `InvalidOperationException`, `ArgumentException`, etc.

```csharp
// WRONG
throw new Exception("Not found");

// CORRECT
throw new InvalidOperationException("Not found");
```

---

### CA1822 — Member does not access instance data

**What it means**: A method doesn't use `this` (instance fields/properties), so it could be `static`.

**Why it matters**:
- Static methods are slightly faster (no `this` pointer)
- Clarifies intent — method is a pure utility
- Enables calling without an instance

**Fix**: Add `static` modifier.

```csharp
// BEFORE
private void LogState(string label, State state) { ... }

// AFTER
private static void LogState(string label, State state) { ... }
```

---

### CA1001 — Type owns disposable field but is not IDisposable

**Fix**: Implement `IDisposable` when holding disposable fields.

```csharp
public class MyClass : IDisposable
{
    private readonly HttpClient client = new();
    
    public void Dispose()
    {
        client?.Dispose();
        GC.SuppressFinalize(this);
    }
}
```

---

## Summary

| Code   | Category       | Action |
| ------ | -------------- | ------ |
| CA2201 | Exception type | Fix    |
| CA1822 | Static member  | Fix    |
| CA1001 | Disposable     | Fix    |

**Policy**: Fix all warnings. Zero suppressions.
