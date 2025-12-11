## Scripts (F# only)

This repository has been rebuilt around a functional F# console app that inspects and manages the local state for YouTube playlists and Last.fm scrobbles. Side effects are pushed to the boundaries (file IO), while the core logic stays immutable and composable.

### Running

```bash
dotnet run --project Scripts/Scripts.fsproj           # Status for all services
dotnet run --project Scripts/Scripts.fsproj status yt # Only YouTube
dotnet run --project Scripts/Scripts.fsproj sync all  # Capture a fresh snapshot
dotnet run --project Scripts/Scripts.fsproj clean     # Remove cached state
```

### Commands

- `status [youtube|lastfm|all]` – read the existing JSON state and render summaries.
- `sync [youtube|lastfm|all]` – build immutable snapshots and write them to `state/fsharp/state.json`.
- `clean [youtube|lastfm|all]` – delete state files for the requested services (plus the F# snapshot).

### Philosophy

- Small, composable modules (`Service`, `StateReader`, `Formatter`, `Execution`) with explicit data types.
- Pure transformations first; IO lives at the edges.
- JSON parsing is tolerant and ignores missing fields to keep the pipeline resilient.
