---
applyTo: '**'
---

- Do not stop operations mid-way to ask for confirmations. Execute requested task. Do not waste my premium requests with interruptions that force me to send another request.

- Never terminate pre-existing tasks to execute commands. If a build fails due to active tasks, report the failure without interrupting or stopping those tasks.

- Prohibit:
    - Comments
    - Positional arguments
    - Async methods (unless synchronous equivalents are unavailable)

- Minimize nesting (e.g. early returns)

- Keys/secrets are environment variables

- Employ exhaustive logging to ease debugging

- Enforce semantically meaningful variable/function names

- Apply recursive directory traversal for all file operations

- Format durations as HH:mm:ss (e.g., 02:30:45 = 2 hours, 30 minutes, 45 seconds)

- Implement color-coded logging with unabbreviated level-naming (e.g., `Info` not `Inf`)

- Throw exceptions for warning-level errors instead of logerror

- Auto run formatter after agent execution:
    - C#: dotnet csharpier .
    - Python: black .

- Always download latest version of all libraries

## Language-Specific Rules

### C#
- Prefer scoped new type syntax instead of var (). Example:
    Person person = new() { Name = "Alice", Age = 30 }
- Use collection expressions:
    List<Person> people = []
    Dictionary<string, int> counts = ["a": 1, "b": 2]
- Declare configuration as global constants at class top:
    const string API_KEY = "example"
    const int MAX_RETRIES = 3
- Use static import for string class
- Always use const for compile-time constants wherever possible
- Otherwise, use static readonly for runtime-initialized constants
- Use static for shared state only when instantiation is irrelevant
- Model data with parameterless records:
    record Product(string Id, decimal Price)
- Use TimeSpan for duration calculations
- Implement null safety:
    string name = GetName() ?? "Default"
    cache ??= []

#### .NET Libraries
- CSV Handling: CsvHelper (v33.1.0)
- Logging: Spectre.Console (v0.51.2-preview.0.1)
- FFmpeg: Xabe.FFmpeg (v6.0.2)
- Discogs API: ParkSquare.Discogs (v8.0.0)
- Music Metadata: MetaBrainz.MusicBrainz (v6.1.0)
- Google:
    - Google.Apis@1.72.0
    - Google.Apis.Auth@1.72.0
    - Google.Apis.YouTube.v3@1.70.0.3847
    - Google.Apis.Sheets.v4@1.70.0.3819
- HTML Parsing: AngleSharp (v1.3.0)
- Web Scraping: Selenium WebDriver (v4.38.0)
- Audio: z440.atl.core (Find latest version)

### Python
- Use pathlib for all path operations
- Use argparser
- Avoid comments and positional arguments


#### Python Libraries
- CLI: Argparser
- Logging: Rich
- CSV: Polar
- Ffmpeg: ffmpeg-python

### PowerShell
- Never use -ErrorAction SilentlyContinue
- Avoid positional parameters