# Modern .NET Library & CLI Playbook

## System.Text.Json (Modern)
```csharp
var options = new JsonSerializerOptions {
    PropertyNamingPolicy = JsonNamingPolicy.PascalCase,
    RespectNullableAnnotations = true,
    RespectRequiredConstructorParameters = true,
    AllowDuplicateProperties = false
};

// PipeReader Support
var result = await JsonSerializer.DeserializeAsync<T>(pipe.Reader);
```

## LINQ (Advanced Aggregations)
```csharp
// CountBy (Frequency)
var counts = items.CountBy(x => x.Category);

// AggregateBy (Stateful)
var totals = items.AggregateBy(x => x.Key, seed: 0, (acc, item) => acc + item.Val);

// Index
foreach (var (index, item) in items.Index()) { ... }
```

## Collections
```csharp
// OrderedDictionary (Generic)
OrderedDictionary<string, int> d = [];
d.TryAdd("key", 1, out int index);

// ReadOnlySet
ReadOnlySet<int> set = new(hashSet);
```

## GUID Version 7
```csharp
var id = Guid.CreateVersion7(); // Sortable
```

## Base64Url
```csharp
string encoded = Base64Url.EncodeToString(bytes);
```
