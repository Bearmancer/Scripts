# Refactoring Action Plan v2

## Can IMusic and IClassical be combined? If so, how would one account for their different schemas?

### Interface Schema

```
┌─────────────────────────────────────────────────────────────────┐
│                        IMusicService                            │
│  - SourceName: string                                           │
│  - GetReleaseByIdAsync(id) → UnifiedRelease                     │
│  - SearchAsync(query) → List<UnifiedSearchResult>               │
│  - SearchByArtistAsync(artist) → List<UnifiedSearchResult>      │
│  - SearchByAlbumAsync(album) → List<UnifiedSearchResult>        │
└─────────────────────────────────────────────────────────────────┘
                              ▲
                              │ implements
              ┌───────────────┴───────────────┐
              │                               │
    ┌─────────┴─────────┐           ┌─────────┴─────────┐
    │  DiscogsService   │           │ MusicBrainzService │
    │  + ParseBoxSet    │           │  + ParseBoxSet     │
    │    (via credits)  │           │    (via Work rels) │
    └───────────────────┘           └────────────────────┘


┌─────────────────────────────────────────────────────────────────┐
│                   IClassicalMusicService                        │
│  - ParseBoxSetAsync(releaseId, options) → List<BoxSetTrack>     │
│  - GetWorkHierarchyAsync(workId) → WorkHierarchy                │
└─────────────────────────────────────────────────────────────────┘
                              ▲
                              │ implements
              ┌───────────────┴───────────────┐
              │                               │
    ┌─────────┴─────────┐           ┌─────────┴─────────┐
    │  DiscogsService   │           │ MusicBrainzService │
    │  (extracts from   │           │  (follows Work →   │
    │   ExtraArtists)   │           │   Recording links) │
    └───────────────────┘           └────────────────────┘


┌─────────────────────────────────────────────────────────────────┐
│                   IDisposableMailService                        │
│  - CreateAccountAsync() → MailAccount                           │
│  - GetInboxAsync() → List<MailMessage>                          │
│  - ReadMessageAsync(id) → MailMessage                           │
│  - ForgetSessionAsync()                                         │
└─────────────────────────────────────────────────────────────────┘
                              ▲
                              │ implements
        ┌─────────────────────┼─────────────────────┐
        │                     │                     │
┌───────┴───────┐    ┌────────┴────────┐   ┌───────┴───────┐
│ MailTmService │    │ GuerrillaService │   │ MailDropService│
└───────────────┘    └──────────────────┘   └────────────────┘
```

---

## 📁 Final Directory Structure

```
csharp/src/
├── Models/
│   ├── Discogs.cs                       # Discogs DTOs (existing)
│   ├── MusicBrainz.cs                   # MusicBrainz DTOs (existing)
│   ├── Mail.cs                          # NEW: MailAccount, MailMessage
│   ├── Unified.cs                       # NEW: UnifiedRelease, UnifiedTrack, UnifiedSearchResult
│   └── YouTube.cs                       # YouTube DTOs (existing)
│
├── Services/
│   ├── Mail/
│   │   └── IDisposableMailService.cs    # interface
│   │   ├── MailTmService.cs             # Implements IDisposableMailService
│   │   ├── GuerrillaMailService.cs      # NEW: Implements IDisposableMailService
│   │   └── MailDropService.cs           # NEW: Implements IDisposableMailService
│   │
│   ├── Music/
│   │   ├── IMusicService.cs                 # General music metadata interface
│   │   ├── IClassicalMusicService.cs        # Classical-specific (box sets, works) interface
│   │   ├── DiscogsService.cs            # Implements IMusicService + IClassicalMusicService
│   │   ├── MusicBrainzService.cs        # Implements IMusicService + IClassicalMusicService
│   │   └── (OrmandyBoxParser.cs)        # DELETE: Logic absorbed into above services
│   │
│   └── Sync/                            # Unchanged
│
├── CLI/
│   ├── MailCommands.cs                  # Auto-refresh, clipboard, selection
│   ├── MusicCommands.cs                 # Unified search by name/artist/album/ID
│   ├── CleanCommands.cs
│   ├── SyncCommands.cs
│   └── (TestCommands.cs)                # DELETE
│
├── Orchestrators/                       # Unchanged
├── Infrastructure/                      # Unchanged
├── GlobalUsings.cs
└── Program.cs
```

---

## 🔧 Interface Definitions

### `Interfaces/IMusicService.cs`
```csharp
namespace CSharpScripts.Interfaces;

public interface IMusicService
{
    string SourceName { get; }
    
    Task<UnifiedRelease?> GetReleaseByIdAsync(string id);
    Task<List<UnifiedSearchResult>> SearchAsync(string query, int maxResults = 10);
    Task<List<UnifiedSearchResult>> SearchByArtistAsync(string artist, int maxResults = 10);
    Task<List<UnifiedSearchResult>> SearchByAlbumAsync(string album, int maxResults = 10);
}
```

### `Interfaces/IClassicalMusicService.cs`
```csharp
namespace CSharpScripts.Interfaces;

public interface IClassicalMusicService
{
    Task<List<BoxSetTrackMetadata>> ParseBoxSetAsync(string releaseId, BoxSetParseOptions options);
}
```

### `Interfaces/IDisposableMailService.cs`
```csharp
namespace CSharpScripts.Interfaces;

public interface IDisposableMailService
{
    Task<MailAccount> CreateAccountAsync();
    Task<List<MailMessage>> GetInboxAsync();
    Task<MailMessage> ReadMessageAsync(string messageId);
    Task ForgetSessionAsync();
}
```
### Both services have separate model files that reflect their own schema more closely


### `Models/Mail.cs`
```csharp
namespace CSharpScripts.Models;

public record MailAccount(
    string Address,
    DateTime CreatedAt // there are no passwords
);

public record MailMessage(
    string Id,
    string From,
    string Subject,
    string Body,
    DateTime ReceivedAt,
    bool IsRead
);
```

---

## 🚀 Execution Plan

### Phase 1: Create Interface Infrastructure
| Step | Task                                         |
| ---- | -------------------------------------------- |
| 1.1  | Create `src/Interfaces/` folder              |
| 1.2  | Create `IMusicService.cs`                    |
| 1.3  | Create `IClassicalMusicService.cs`           |
| 1.4  | Create `IDisposableMailService.cs`           |
| 1.6  | Create `Models/Mail.cs` with mail DTOs       |
| 1.7  | Update `GlobalUsings.cs` with new namespaces |

### Phase 2: Update Music Services
| Step | Task                                                                                            |
| ---- | ----------------------------------------------------------------------------------------------- |
| 2.1  | Update `DiscogsService.cs` to implement both interfaces                                         |
| 2.2  | Update `MusicBrainzService.cs` to implement both interfaces                                     |
| 2.3  | Move OrmandyBoxParser logic into `MusicBrainzService.ParseBoxSetAsync`                          |
| 2.4  | Add equivalent box set parsing to `DiscogsService.ParseBoxSetAsync`                             |
| 2.5  | Fix JsonSerializer caching (use `StateManager.JsonIndented`)                                    |
| 2.6  | Delete `OrmandyBoxParser.cs`                                                                    |
| 2.7  | Assess `MusicMetadataService.cs` usefulness → likely DELETE (facade not needed with interfaces) |

### Phase 3: Create Mail Services
| Step | Task                                                                   |
| ---- | ---------------------------------------------------------------------- |
| 3.1  | Create `GuerrillaMailService.cs` implementing `IDisposableMailService` |
| 3.2  | Create `MailDropService.cs` implementing `IDisposableMailService`      |
| 3.3  | Update `MailTmService.cs` to implement `IDisposableMailService`        |

### Phase 4: Update CLI
| Step | Task                                                                 |
| ---- | -------------------------------------------------------------------- |
| 4.1  | Rewrite `MailCommands.cs` with auto-refresh, clipboard, selection    |
| 4.2  | Update `MusicCommands.cs` with unified search (name/artist/album/ID) |
| 4.3  | Delete `TestCommands.cs`                                             |
| 4.4  | Update `Program.cs` command registration                             |

### Phase 5: Verification
| Step | Task                        |
| ---- | --------------------------- |
| 5.1  | Run `csharpier format .`    |
| 5.2  | Run `dotnet build`          |
| 5.3  | Verify 0 warnings, 0 errors |

---


1. DELETE **MusicMetadataService**: Delete or keep as unified facade?

2. Respect differences within **Box set parsing of both services when designing interfaces and classes and records and method signatures
   - Both can produce `BoxSetTrackMetadata`, but with different fidelity

---

## ⚠️ Critical Reminders

1. **DO NOT run `git filter-repo`** — destroys stashes
2. Use `.mailmap` for visual author unification (non-destructive)
3. Interfaces are **separate files** in `Interfaces/` folder
4. Both services implement **both** `IMusicService` AND `IClassicalMusicService`

---

## Ready?

Reply **"proceed"** to start with Phase 1.
