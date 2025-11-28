---
applyTo: '**'
---
- Prohibit:
    - Comments
    - Positional arguments
    - Async methods (unless synchronous equivalents are unavailable)

- Implement early returns to minimize nesting depth in languages without null-conditional operators

- Keys/secrets are environment variables. 

- Integrate downloading packages if they are missing

- Employ expansive logging for debugging purposes / meaningful exception throw

- Select libraries/tools based on:
    - Free licensing solely
    - Performance benchmarks (cite most recent independent tests)

- Utilize functional programming styles when migrating to F# instead of trying to enforce OOP pattern on a FP language.

- Enforce:
    - Semantically meaningful variable/function names
    - Strategic empty lines for logical grouping

- Apply recursive directory traversal for all file operations
- Format durations as HH:mm:ss (e.g., 02:30:45 = 2 hours, 30 minutes, 45 seconds)
- Implement color-coded logging with unabbreviated level-naming (e.g., `Info` not `Inf`)
- Throw exceptions for warning-level errors instead of logerror. 

### C# Specific
- Use .NET 10 features

- Apply type inference with `var` where explicit typing is redundant (e.g., `var count = 10;`)

- Implement collection expressions:
    List<Person> people = [];
    Dictionary<string, int> counts = ["a": 1, "b": 2];

- Declare dependencies using C#14 file-scoped syntax. Examples:
    #:package Spectre.Console@0.51.2-preview.0.1
    #:package MetaBrainz.MusicBrainz@6.1.0

- Define configuration via global constants at class (inside class) top:
    const string API_KEY = "example";
    const int MAX_RETRIES = 3;

- Always include publish properties:
    #:property PublishAot=false
    #:property PublishTrimmed=false

- Structure code within class blocks:
    public class Processor
    {
        const int BUFFER_SIZE = 4096;
        static readonly string[] VALID_EXTENSIONS = [".txt", ".csv"];
        
        // Implementation
    }

- Static import string class

- Access modifier hierarchy:
    1. `const` (compile-time constants)
    2. `static readonly` (runtime-initialized constants)
    3. `static` (shared state)

- Model data with parameterless records:
    record Product(string Id, decimal Price);

- Use `TimeSpan` for duration calculations

- Implement null safety:
    string name = GetName() ?? "Default";
    cache ??= [];

#### .NET Libraries
1. **CSV Handling**: CsvHelper (v33.1.0)
2. **Logging**: Spectre.Console (v0.51.2-preview.0.1)
3. **FFmpeg**: Xabe.FFmpeg (v6.0.2)
4. **Discogs API**: ParkSquare.Discogs (v8.0.0)
5. **Music Metadata**: MetaBrainz.MusicBrainz (v6.1.0)
6. **Google**:
    a. Google.Apis@1.72.0
    b. Google.Apis.Auth@1.72.0
    c. Google.Apis.YouTube.v3@1.70.0.3847
    d. Google.Apis.Sheets.v4@1.70.0.3819
6. **HTML Parsing**: AngleSharp (v1.3.0)
7. **Web Scraping**: Selenium WebDriver (v4.38.0)