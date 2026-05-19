# Modern C# Playbook (C# 13-15+)

## Field-Backed Properties (C# 13+)
```csharp
public string Name { 
    get; 
    set => field = value?.Trim() ?? throw new ArgumentNullException(nameof(value)); 
}
```

## Union Types (C# 15+)
```csharp
[Union]
public partial record Result {
    public record Success(string Data) : Result;
    public record Error(string Message) : Result;
}
```

## Collection Expressions (`[]`)
Mandatory for all collection initializations.
```csharp
int[] numbers = [1, 2, 3];
ReadOnlySpan<char> span = ['a', 'b', 'c'];
List<string> list = [with(capacity: 10), "a", "b"]; // C# 15
```

## Extension Members (C# 14+)
Define properties and static members in extension blocks.
```csharp
public static class EnumerableExtensions {
    extension<T>(IEnumerable<T> source) {
        public bool IsEmpty => !source.Any();
    }
}
```

## Lambda Modifiers (C# 14+)
```csharp
var parse = (text, out result) => Int32.TryParse(text, out result);
```

## Implicit Span Conversions
```csharp
ReadOnlySpan<char> span = "hello"; // No .AsSpan() required
```
