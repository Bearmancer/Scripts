# C# Region Guidelines

*Last Updated: December 25, 2025*

## What Are Regions?

**Regions** (`#region` / `#endregion`) are code folding markers in C# that group related code sections for better organization.

```csharp
#region Constructor
public MusicService(HttpClient client)
{
    _client = client;
}
#endregion

#region Public Methods
public async Task<Album> GetAlbumAsync(string id)
{
    // implementation
}
#endregion
```

---

## Why Use Regions?

### Benefits

✅ **Discoverability** - Quick navigation with IDE outline  
✅ **Organization** - Logical grouping of related code  
✅ **Folding** - Collapse sections to see file structure  
✅ **Readability** - Clear separation of concerns  
✅ **Onboarding** - Helps new developers understand layout

### Drawbacks

❌ **Noise** - Extra lines that aren't code  
❌ **Maintenance** - Can get out of sync with code  
❌ **Over-use** - Can hide poor class design  
❌ **Debate** - Some developers hate regions  

---

## When to Use Regions

### File Size Threshold

- **0-100 lines:** NO regions (file is already small)
- **100-300 lines:** 0-2 regions (optional, use sparingly)
- **300-600 lines:** 2-3 regions (recommended)
- **600-1000 lines:** 3-5 regions (highly recommended)
- **1000+ lines:** 5-7 regions (critical for navigation)

### Required Regions (Files >300 Lines)

#### 1. Fields & Properties
```csharp
#region Fields & Properties

private readonly HttpClient _client;
private readonly ILogger<MusicService> _logger;

public string ApiKey { get; set; }
public int MaxRetries { get; set; } = 3;

#endregion
```

**When:** File has 5+ fields or properties

#### 2. Constructor(s)
```csharp
#region Constructor

public MusicService(HttpClient client, ILogger<MusicService> logger)
{
    _client = client ?? throw new ArgumentNullException(nameof(client));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
}

#endregion
```

**When:** Constructor has significant initialization logic (>10 lines)

#### 3. Public Methods
```csharp
#region Public Methods

public async Task<Release> SearchReleaseAsync(string query)
{
    // implementation
}

public async Task<Artist> GetArtistAsync(string id)
{
    // implementation
}

#endregion
```

**When:** Class has 3+ public methods

#### 4. Private/Helper Methods
```csharp
#region Private Helper Methods

private async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> action)
{
    // retry logic
}

private string BuildQueryString(Dictionary<string, string> parameters)
{
    // URL building
}

#endregion
```

**When:** Class has 3+ private methods

### Optional Regions

#### API Operations (Service Classes)
```csharp
#region Artist Operations

public async Task<Artist> GetArtistAsync(string id) { }
public async Task<List<Artist>> SearchArtistsAsync(string query) { }

#endregion

#region Release Operations

public async Task<Release> GetReleaseAsync(string id) { }
public async Task<List<Release>> SearchReleasesAsync(string query) { }

#endregion
```

**When:** Service has multiple operation groups (>5 methods per group)

#### Event Handlers (UI Code)
```csharp
#region Event Handlers

private void OnSearchButtonClick(object sender, EventArgs e)
{
    // handler
}

private void OnResultSelected(object sender, SelectionChangedEventArgs e)
{
    // handler
}

#endregion
```

**When:** Class has 3+ event handlers

#### Interface Implementations
```csharp
#region IDisposable Implementation

private bool _disposed = false;

public void Dispose()
{
    Dispose(true);
    GC.SuppressFinalize(this);
}

protected virtual void Dispose(bool disposing)
{
    if (!_disposed)
    {
        if (disposing)
        {
            _client?.Dispose();
        }
        _disposed = true;
    }
}

#endregion
```

**When:** Interface requires multiple methods (IDisposable, IEnumerable, etc.)

---

## Region Naming Conventions

### Standard Names (Alphabetical)

- `#region Constructor` (singular, even if multiple constructors)
- `#region Constants`
- `#region Delegates & Events`
- `#region Enums`
- `#region Fields` or `#region Fields & Properties`
- `#region IDisposable Implementation`
- `#region Indexers`
- `#region Nested Types`
- `#region Operators`
- `#region Private Helper Methods`
- `#region Properties`
- `#region Public Methods`
- `#region Static Methods`

### Descriptive Names (Domain-Specific)

- `#region Artist Operations` (not "Artist Methods")
- `#region Cache Management` (not "Cache Stuff")
- `#region Database Queries` (not "DB")
- `#region HTTP Requests` (not "Network")
- `#region Validation` (not "Checks")

### Anti-Patterns (DON'T)

❌ `#region Private` (too vague)  
❌ `#region Methods` (what kind of methods?)  
❌ `#region Stuff` (meaningless)  
❌ `#region TODO` (use actual TODO comments)  
❌ `#region Generated Code` (use partial classes instead)  

---

## Region Ordering

### Standard Order (Top to Bottom)

1. **Usings** (no region)
2. **Namespace** (no region)
3. **Constants** (if present)
4. **Enums** (if present)
5. **Fields & Properties**
6. **Constructor(s)**
7. **Public Methods** (or operation-specific regions)
8. **Protected Methods** (if inheritance)
9. **Private Helper Methods**
10. **IDisposable Implementation** (if applicable)
11. **Nested Types** (if present)

### Example: Well-Ordered File

```csharp
using System;
using System.Net.Http;

namespace Scripts.Services.Music;

public class DiscogsService : IDisposable
{
    #region Constants
    
    private const string BaseUrl = "https://api.discogs.com";
    private const int DefaultPageSize = 50;
    
    #endregion
    
    #region Fields & Properties
    
    private readonly HttpClient _client;
    private readonly ILogger<DiscogsService> _logger;
    private bool _disposed;
    
    public string ApiKey { get; set; }
    public int MaxRetries { get; set; } = 3;
    
    #endregion
    
    #region Constructor
    
    public DiscogsService(HttpClient client, ILogger<DiscogsService> logger)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    #endregion
    
    #region Artist Operations
    
    public async Task<Artist> GetArtistAsync(string id)
    {
        // implementation
    }
    
    public async Task<List<Artist>> SearchArtistsAsync(string query)
    {
        // implementation
    }
    
    #endregion
    
    #region Release Operations
    
    public async Task<Release> GetReleaseAsync(string id)
    {
        // implementation
    }
    
    public async Task<List<Release>> SearchReleasesAsync(string query)
    {
        // implementation
    }
    
    #endregion
    
    #region Private Helper Methods
    
    private async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> action)
    {
        // retry logic
    }
    
    private string BuildQueryString(Dictionary<string, string> parameters)
    {
        // URL building
    }
    
    #endregion
    
    #region IDisposable Implementation
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _client?.Dispose();
            }
            _disposed = true;
        }
    }
    
    #endregion
}
```

---

## Your C# Files - Region Strategy

Based on file size analysis (largest to smallest):

### Files >1000 Lines (5-7 Regions)

#### GoogleSheetsService.cs (1230 lines)
```csharp
#region Fields & Properties        (private fields, public properties)
#region Constructor                (DI setup, service initialization)
#region Authentication              (Google OAuth, token refresh)
#region Spreadsheet Operations      (create, get, update spreadsheets)
#region Cell Operations             (read, write, format cells)
#region Private Helper Methods      (URL building, error handling)
#region IDisposable Implementation  (cleanup)
```

#### MusicBrainzService.cs (1041 lines)
```csharp
#region Fields & Properties
#region Constructor
#region Artist Operations           (search, get, lookup)
#region Release Operations          (search, get, browse)
#region Recording Operations        (search, get, lookup)
#region Work Operations             (search, get, relationships)
#region Private Helper Methods      (rate limiting, XML parsing, retry logic)
```

#### MusicSearchCommand.cs (1035 lines)
```csharp
#region Fields & Properties
#region Constructor
#region Command Handler             (main Execute method)
#region Discogs Search             (artist, release, master search)
#region MusicBrainz Search         (artist, release, recording search)
#region Result Formatting          (console output, table rendering)
#region Private Helper Methods      (input parsing, result merging)
```

#### DiscogsService.cs (1008 lines)
```csharp
#region Fields & Properties
#region Constructor
#region Artist Operations
#region Release Operations
#region Master Operations
#region Label Operations
#region Private Helper Methods      (rate limiting, pagination, OAuth)
#region IDisposable Implementation
```

### Files 600-1000 Lines (3-4 Regions)

#### YouTubePlaylistOrchestrator.cs (933 lines)
```csharp
#region Fields & Properties
#region Constructor
#region Public Methods              (orchestration, sync, update)
#region Private Helper Methods      (playlist processing, video filtering)
```

#### MusicFillCommand.cs (612 lines)
```csharp
#region Fields & Properties
#region Constructor
#region Command Handler
#region Private Helper Methods
```

#### (Similar for other 600-1000 line files)

### Files 300-600 Lines (2-3 Regions)

#### ScrobbleSyncOrchestrator.cs (414 lines)
```csharp
#region Fields & Properties
#region Constructor
#region Public Methods
```

#### Console.cs (414 lines)
```csharp
#region Fields & Properties
#region Output Methods             (Write, WriteLine, WriteError)
#region Input Methods              (ReadLine, Prompt, Confirm)
```

### Files 100-300 Lines (0-2 Regions)

- Optional: Only if there's clear separation (e.g., public vs private methods)
- Most files this size don't need regions

### Files <100 Lines (0 Regions)

- Never use regions
- File is already small enough to scan visually

---

## Region Best Practices

### DO:

✅ Use descriptive names (`#region Artist Operations`)  
✅ Keep regions focused (one concern per region)  
✅ Maintain alphabetical order within regions  
✅ Close regions immediately after section  
✅ Use consistent naming across files  
✅ Collapse regions when reviewing code structure  

### DON'T:

❌ Nest regions (`#region` inside another `#region`)  
❌ Create single-method regions (use comments instead)  
❌ Use regions to hide bad code (refactor instead!)  
❌ Abbreviate region names (`#region Ctor` → `#region Constructor`)  
❌ Create region for every method (defeats purpose)  
❌ Use regions in files <100 lines  

---

## Alternative to Regions: Partial Classes

For VERY large classes (>1500 lines), consider splitting into partial classes:

```csharp
// DiscogsService.cs (main class)
public partial class DiscogsService
{
    private readonly HttpClient _client;
    
    public DiscogsService(HttpClient client)
    {
        _client = client;
    }
}

// DiscogsService.Artists.cs (artist operations)
public partial class DiscogsService
{
    public async Task<Artist> GetArtistAsync(string id)
    {
        // implementation
    }
}

// DiscogsService.Releases.cs (release operations)
public partial class DiscogsService
{
    public async Task<Release> GetReleaseAsync(string id)
    {
        // implementation
    }
}
```

**When to Use Partial Classes:**
- File >1500 lines
- Clear functional separation (Artists, Releases, Labels)
- Each partial file can be 300-500 lines

**When NOT to Use:**
- Files <1000 lines (regions are sufficient)
- No clear separation (forced splitting)
- Team unfamiliar with partial classes

---

## IDE Support

### Visual Studio

- **Outline Window:** View → Other Windows → Outline
- **Collapse All:** Ctrl+M, Ctrl+O
- **Expand All:** Ctrl+M, Ctrl+L
- **Toggle Region:** Ctrl+M, Ctrl+M

### VS Code (with C# extension)

- **Outline Pane:** Explorer → Outline
- **Fold All:** Ctrl+K, Ctrl+0
- **Unfold All:** Ctrl+K, Ctrl+J
- **Fold Region:** Ctrl+K, Ctrl+[

### Rider

- **Structure Window:** View → Tool Windows → Structure
- **Collapse All:** Ctrl+Numpad-
- **Expand All:** Ctrl+Numpad+
- **Toggle Fold:** Ctrl+Period

---

## Testing Regions

```csharp
// Test files follow same conventions

public class DiscogsServiceTests
{
    #region Test Setup
    
    private DiscogsService _service;
    private Mock<HttpClient> _httpClient;
    
    [SetUp]
    public void Setup()
    {
        // test setup
    }
    
    #endregion
    
    #region Artist Operation Tests
    
    [Test]
    public async Task GetArtistAsync_ReturnsArtist()
    {
        // test
    }
    
    [Test]
    public async Task SearchArtistsAsync_ReturnsResults()
    {
        // test
    }
    
    #endregion
    
    #region Release Operation Tests
    
    // ...
    
    #endregion
}
```

---

## Summary

### Quick Decision Tree

1. **File <100 lines?** → NO regions
2. **File 100-300 lines?** → OPTIONAL (0-2 regions)
3. **File 300-600 lines?** → YES (2-3 regions)
4. **File 600-1000 lines?** → YES (3-5 regions)
5. **File >1000 lines?** → YES (5-7 regions) or consider partial classes

### Standard Region Set (Medium Files)

```csharp
#region Fields & Properties
#region Constructor
#region Public Methods
#region Private Helper Methods
```

### Extended Region Set (Large Files)

```csharp
#region Constants
#region Fields & Properties
#region Constructor
#region [Domain] Operations  (repeat for each domain)
#region Private Helper Methods
#region IDisposable Implementation
```

### Your Implementation Priorities

1. **GoogleSheetsService.cs** (1230 lines) - 7 regions
2. **MusicBrainzService.cs** (1041 lines) - 7 regions
3. **MusicSearchCommand.cs** (1035 lines) - 7 regions
4. **DiscogsService.cs** (1008 lines) - 7 regions
5. **All files 600-1000 lines** - 3-5 regions each
6. **All files 300-600 lines** - 2-3 regions each
7. **Files <300 lines** - Skip or minimal

**Total Effort:** ~6-8 hours for all files needing regions (20 files)
