open System
open System.IO
open System.Globalization
open System.Text.Json

module Json =
    let options =
        let o = JsonSerializerOptions()
        o.PropertyNameCaseInsensitive <- true
        o.WriteIndented <- true
        o

    let tryString (elem: JsonElement) (name: string) =
        match elem.TryGetProperty(name) with
        | true, p when p.ValueKind = JsonValueKind.String -> Some(p.GetString())
        | _ -> None

    let tryInt (elem: JsonElement) (name: string) =
        match elem.TryGetProperty(name) with
        | true, p when p.ValueKind = JsonValueKind.Number ->
            match p.TryGetInt32() with
            | true, v -> Some v
            | _ -> None
        | _ -> None

    let tryBool (elem: JsonElement) (name: string) =
        match elem.TryGetProperty(name) with
        | true, p when p.ValueKind = JsonValueKind.True -> Some true
        | true, p when p.ValueKind = JsonValueKind.False -> Some false
        | _ -> None

    let tryDate (elem: JsonElement) (name: string) =
        match tryString elem name with
        | Some s ->
            match DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind) with
            | true, dt -> Some dt
            | _ -> None
        | None -> None

module Environment =
    type Env =
        { Root: string
          StateRoot: string
          LastFmState: string
          LastFmScrobbles: string
          YouTubeState: string
          FSharpState: string }

    let current =
        let root =
            __SOURCE_DIRECTORY__
            |> Directory.GetParent
            |> fun p -> p.FullName

        let stateRoot = Path.Combine(root, "state")
        { Root = root
          StateRoot = stateRoot
          LastFmState = Path.Combine(stateRoot, "lastfm", "sync.json")
          LastFmScrobbles = Path.Combine(stateRoot, "lastfm", "scrobbles.json")
          YouTubeState = Path.Combine(stateRoot, "youtube", "sync.json")
          FSharpState = Path.Combine(stateRoot, "fsharp", "state.json") }

module Domain =
    type Service =
        | YouTube
        | LastFm

    type Command =
        | Status of Service list
        | Sync of Service list
        | Clean of Service list
        | Help of string option

    type LastFmSnapshot =
        { TotalFetched: int option
          LastUpdated: DateTime option
          FetchComplete: bool option }

    type YouTubeSnapshot =
        { PlaylistCount: int
          VideoCount: int
          LastUpdated: DateTime option
          FetchComplete: bool option }

    type Summary =
        { Service: string
          Lines: string list }

    type FSharpSnapshot =
        { Service: string
          CapturedAt: DateTime
          LastUpdated: string option
          Metrics: (string * int) list
          Flags: string list }

    type PersistedState =
        { CapturedAt: DateTime
          Snapshots: FSharpSnapshot list }

module Service =
    open Domain

    let all = [ YouTube; LastFm ]

    let ofString (raw: string) =
        match raw.Trim().ToLowerInvariant() with
        | "yt"
        | "youtube" -> Some YouTube
        | "lastfm"
        | "last.fm"
        | "lfm" -> Some LastFm
        | "all" -> None
        | _ -> None

    let name = function
        | YouTube -> "YouTube"
        | LastFm -> "Last.fm"

    let resolve inputs =
        match inputs with
        | [] -> all
        | xs -> xs

module StateReader =
    open Domain

    let private defaultYouTube =
        { PlaylistCount = 0
          VideoCount = 0
          LastUpdated = None
          FetchComplete = None }

    let private defaultLastFm =
        { TotalFetched = None
          LastUpdated = None
          FetchComplete = None }

    let loadYouTube path =
        if not (File.Exists path) then
            defaultYouTube
        else
            use doc = JsonDocument.Parse(File.ReadAllText path)
            let root = doc.RootElement

            let snapshots =
                match root.TryGetProperty("PlaylistSnapshots") with
                | true, s when s.ValueKind = JsonValueKind.Object -> s.EnumerateObject() |> Seq.toList
                | _ -> []

            let playlistCount = snapshots |> List.length

            let videoCount =
                snapshots
                |> List.sumBy (fun p ->
                    match p.Value.TryGetProperty("VideoIds") with
                    | true, ids when ids.ValueKind = JsonValueKind.Array -> ids.GetArrayLength()
                    | _ -> 0)

            { PlaylistCount = playlistCount
              VideoCount = videoCount
              LastUpdated = Json.tryDate root "LastUpdated"
              FetchComplete = Json.tryBool root "FetchComplete" }

    let loadLastFm path =
        if not (File.Exists path) then
            defaultLastFm
        else
            use doc = JsonDocument.Parse(File.ReadAllText path)
            let root = doc.RootElement

            { TotalFetched = Json.tryInt root "TotalFetched"
              LastUpdated = Json.tryDate root "LastUpdated"
              FetchComplete = Json.tryBool root "FetchComplete" }

module Formatter =
    open Domain
    open Service

    let private formatDate = function
        | None -> "unknown"
        | Some (dt: DateTime) -> dt.ToString("u", CultureInfo.InvariantCulture)

    let private formatFlag value =
        value |> Option.map string |> Option.defaultValue "unknown"

    let youTubeSummary (snapshot: YouTubeSnapshot) : Summary =
        { Service = name YouTube
          Lines =
            [ $"Playlists indexed: {snapshot.PlaylistCount}"
              $"Videos tracked: {snapshot.VideoCount}"
              $"Fetch complete: {formatFlag snapshot.FetchComplete}"
              $"Last updated: {snapshot.LastUpdated |> formatDate}" ] }

    let lastFmSummary (snapshot: LastFmSnapshot) : Summary =
        { Service = name LastFm
          Lines =
            [ $"Scrobbles stored: {snapshot.TotalFetched |> Option.defaultValue 0}"
              $"Fetch complete: {formatFlag snapshot.FetchComplete}"
              $"Last updated: {snapshot.LastUpdated |> formatDate}" ] }

    let render (summary: Summary) =
        printfn ""
        printfn "== %s ==" summary.Service
        summary.Lines |> List.iter (printfn " - %s")

module Persistence =
    open Domain

    let writeState (path: string) (snapshots: FSharpSnapshot list) =
        Path.GetDirectoryName(path)
        |> Directory.CreateDirectory
        |> ignore
        let state =
            { CapturedAt = DateTime.UtcNow
              Snapshots = snapshots }

        File.WriteAllText(path, JsonSerializer.Serialize(state, Json.options))

module Execution =
    open Domain
    open Service
    open StateReader
    open Formatter
    open Persistence
    open Environment

    let private buildSnapshot (service: Service) (youTubeState: YouTubeSnapshot) (lastFmState: LastFmSnapshot) =
        match service with
        | YouTube ->
            { Service = name YouTube
              CapturedAt = DateTime.UtcNow
              LastUpdated = youTubeState.LastUpdated |> Option.map (fun (dt: DateTime) -> dt.ToString("o", CultureInfo.InvariantCulture))
              Metrics =
                [ "playlists", youTubeState.PlaylistCount
                  "videos", youTubeState.VideoCount ]
              Flags =
                [ match youTubeState.FetchComplete with
                  | Some true -> "complete"
                  | Some false -> "incomplete"
                  | None -> "unknown" ] }
        | LastFm ->
            { Service = name LastFm
              CapturedAt = DateTime.UtcNow
              LastUpdated = lastFmState.LastUpdated |> Option.map (fun (dt: DateTime) -> dt.ToString("o", CultureInfo.InvariantCulture))
              Metrics = [ "scrobbles", lastFmState.TotalFetched |> Option.defaultValue 0 ]
              Flags =
                [ match lastFmState.FetchComplete with
                  | Some true -> "complete"
                  | Some false -> "incomplete"
                  | None -> "unknown" ] }

    let status (env: Env) services =
        let resolved = resolve services
        let yt = loadYouTube env.YouTubeState
        let lf = loadLastFm env.LastFmState

        resolved
        |> List.map (function
            | YouTube -> youTubeSummary yt
            | LastFm -> lastFmSummary lf)
        |> List.iter render

    let sync (env: Env) services =
        let resolved = resolve services
        let yt = loadYouTube env.YouTubeState
        let lf = loadLastFm env.LastFmState

        resolved
        |> List.map (fun s -> buildSnapshot s yt lf)
        |> writeState env.FSharpState

        printfn "Captured functional snapshots for: %s" (resolved |> List.map name |> String.concat ", ")

    let clean (env: Env) services =
        let resolved = resolve services

        let deletions =
            resolved
            |> List.collect (function
                | YouTube -> [ env.YouTubeState ]
                | LastFm -> [ env.LastFmState; env.LastFmScrobbles ])
            |> List.append [ env.FSharpState ]

        deletions
        |> List.distinct
        |> List.choose (fun path ->
            if File.Exists path then
                File.Delete path
                Some(Path.GetFileName(path))
            else
                None)
        |> function
            | [] -> printfn "Nothing to clean; state already empty."
            | removed ->
                printfn "Removed %d file(s): %s" removed.Length (String.concat ", " removed)

module Cli =
    open Domain
    open Service

    let parse argv =
        let toServices names =
            names
            |> List.choose (fun (s: string) ->
                match s.ToLowerInvariant() with
                | "all" -> None
                | _ -> Service.ofString s)

        match argv |> Array.toList with
        | [] -> Ok(Status [])
        | "status" :: rest -> Ok(Status(toServices rest))
        | "sync" :: rest -> Ok(Sync(toServices rest))
        | "clean" :: rest -> Ok(Clean(toServices rest))
        | "help" :: _ -> Ok(Help None)
        | unknown :: _ -> Error $"Unknown command '{unknown}'"

    let printHelp () =
        printfn "Scripts (F# only)"
        printfn "Usage:"
        printfn "  scripts [status|sync|clean] [youtube|lastfm|all]"
        printfn "Commands default to both services when no service is provided."
        printfn "Examples:"
        printfn "  scripts status"
        printfn "  scripts sync youtube"
        printfn "  scripts clean all"

[<EntryPoint>]
let main argv =
    let env = Environment.current

    match Cli.parse argv with
    | Error err ->
        printfn "%s" err
        Cli.printHelp ()
        1
    | Ok cmd ->
        match cmd with
        | Domain.Help _ ->
            Cli.printHelp ()
            0
        | Domain.Status services ->
            Execution.status env services
            0
        | Domain.Sync services ->
            Execution.sync env services
            0
        | Domain.Clean services ->
            Execution.clean env services
            0
