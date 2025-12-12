using MetaBrainz.MusicBrainz;
using MetaBrainz.MusicBrainz.Interfaces.Entities;

namespace CSharpScripts.Services.Music;

public record OrmandyTrack(
    int DiscNumber,
    int TrackNumber,
    string? Composer,
    string Title,
    string? WorkTitle,
    Guid? WorkId,
    int? RecordingYear,
    string? Orchestra,
    string? Conductor,
    List<string> Soloists,
    string? Venue,
    List<string> RecordingDates,
    TimeSpan? Length = null,
    string? ParentWorkTitle = null,
    Guid? ParentWorkId = null,
    List<string>? MetadataIssues = null
);

public record BoxSetCache(
    string Name,
    Guid MusicBrainzId,
    DateTime FetchedAt,
    int TotalDiscs,
    int TotalTracks,
    List<OrmandyTrack> Tracks
);

record ParsedWork(
    Guid Id,
    string Title,
    string? Composer,
    Guid? ParentWorkId,
    string? ParentWorkTitle
);

record TrackParseResult(bool Success, OrmandyTrack? Track, string? SkipReason);

public sealed class OrmandyBoxParser
{
    static readonly Guid MusicBrainzReleaseId = Guid.Parse("9abb63b6-5fe0-4bec-a76d-43aad9349d0f");
    const string BOX_SET_NAME = "Ormandy Columbia Legacy";

    const int EXPECTED_START_YEAR = 1944;
    const int EXPECTED_END_YEAR = 1958;

    readonly Query Query = new("LanceUtilities", "1.0", "user@example.com");
    readonly List<string> LogEntries = [];
    readonly Dictionary<Guid, ParsedWork> WorkCache = [];

    void Log(string message) => LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] {message}");

    void LogRelationship(string indent, IRelationship rel)
    {
        string target =
            rel.Artist?.Name ?? rel.Work?.Title ?? rel.Place?.Name ?? rel.Recording?.Title ?? "?";
        string dates = rel.Begin?.NearestDate is DateTime b ? $" [{b:yyyy-MM-dd}]" : "";
        string attrs = rel.Attributes?.Any() == true ? $" ({Join(", ", rel.Attributes)})" : "";
        Log($"{indent}{rel.Type}: {target}{dates}{attrs}");
    }

    void WriteLogFile()
    {
        CreateDirectory(Paths.LogDirectory);
        string logPath = Combine(Paths.LogDirectory, "ormandy.log");
        AppendAllLines(logPath, LogEntries);
        Print($"[dim]Log: {logPath}[/]");
    }

    void WriteDumpFile(List<OrmandyTrack> tracks)
    {
        string dumpDir = Combine(Paths.LogDirectory, "dumps", "ormandy");
        CreateDirectory(dumpDir);
        foreach (OrmandyTrack track in tracks)
        {
            string fileName = $"{track.DiscNumber:D2}-{track.TrackNumber:D2}.json";
            string dumpPath = Combine(dumpDir, fileName);
            string json = JsonSerializer.Serialize(
                track,
                new JsonSerializerOptions { WriteIndented = true }
            );
            WriteAllText(dumpPath, json);
        }
        Print($"[dim]Dumps: {dumpDir} ({tracks.Count} tracks)[/]");
    }

    static void Print(string markup) => AnsiConsole.MarkupLine(markup);

    static void PrintStatus(string label, string? value, string color = "white")
    {
        string display = value ?? "[red]MISSING[/]";
        if (value is not null)
            display = $"[{color}]{Markup.Escape(value)}[/]";
        AnsiConsole.MarkupLine($"  [dim]{label, -14}[/] {display}");
    }

    static bool Confirm(string message) => AnsiConsole.Confirm(message, defaultValue: true);

    static string? Prompt(string message)
    {
        string response = AnsiConsole.Prompt(new TextPrompt<string>(message).AllowEmpty());
        return IsNullOrWhiteSpace(response) ? null : response;
    }

    async Task<ParsedWork?> LookupWorkAsync(Guid workId, CancellationToken ct)
    {
        if (WorkCache.TryGetValue(workId, out ParsedWork? cached))
        {
            Log($"      Work cache hit: {cached.Title}");
            return cached;
        }

        Log($"      Work lookup: https://musicbrainz.org/work/{workId}");

        IWork? fullWork = await Query.LookupWorkAsync(
            workId,
            Include.ArtistRelationships | Include.WorkRelationships,
            ct
        );

        if (fullWork is null)
        {
            Log($"      Work not found: {workId}");
            return null;
        }

        string? composer = null;
        Guid? parentWorkId = null;
        string? parentWorkTitle = null;

        if (fullWork.Relationships is { } rels)
        {
            Log($"      Work relationships ({rels.Count()}):");
            foreach (IRelationship rel in rels)
            {
                LogRelationship("        ", rel);

                if (
                    rel.Type?.Equals("composer", StringComparison.OrdinalIgnoreCase) == true
                    && rel.Artist is { } artist
                )
                {
                    composer = artist.Name;
                    Log($"          → composer: {composer}");
                }

                if (
                    rel.Type?.Equals("parts", StringComparison.OrdinalIgnoreCase) == true
                    && rel.Work is { } parentWork
                    && rel.Direction == "backward"
                )
                {
                    parentWorkId = parentWork.Id;
                    parentWorkTitle = parentWork.Title;
                    Log($"          → parent work: {parentWorkTitle}");
                }
            }
        }

        if (composer is null && parentWorkId.HasValue)
        {
            Log($"      No composer on work, checking parent work...");
            ParsedWork? parent = await LookupWorkAsync(parentWorkId.Value, ct);
            if (parent?.Composer is not null)
            {
                composer = parent.Composer;
                Log($"          → composer from parent: {composer}");
            }
            await Task.Delay(1100, ct);
        }

        ParsedWork result = new(
            fullWork.Id,
            fullWork.Title ?? "Unknown",
            composer,
            parentWorkId,
            parentWorkTitle
        );
        WorkCache[workId] = result;
        return result;
    }

    async Task<TrackParseResult> ParseTrackAsync(ITrack track, int discNumber, CancellationToken ct)
    {
        int trackNumber = track.Position ?? 0;
        string trackTitle = track.Title ?? track.Recording?.Title ?? "Unknown";
        TimeSpan? trackLength = track.Length;
        string trackInfo = $"{discNumber}.{trackNumber}: {trackTitle}";

        Log("");
        Log($"TRACK {trackInfo}");
        Log($"  Track ID: {track.Id}");
        Log($"  Length: {(trackLength.HasValue ? trackLength.Value.ToString(@"mm\:ss") : "N/A")}");

        IRecording? recording = track.Recording;
        if (recording is null)
        {
            Log($"  ERROR: No recording linked");
            return new(false, null, "No recording");
        }

        string recordingUrl = $"https://musicbrainz.org/recording/{recording.Id}";
        Log($"  Recording: {recordingUrl}");

        IRecording? fullRecording = await Query.LookupRecordingAsync(
            recording.Id,
            Include.ArtistCredits
                | Include.ArtistRelationships
                | Include.WorkRelationships
                | Include.Annotation,
            null,
            null,
            ct
        );

        if (fullRecording is null)
        {
            Print($"[yellow]Recording not found: {recording.Id}[/]");
            if (!Confirm("Skip this track and continue?"))
                return new(false, null, "Recording not found - user aborted");
            return new(false, null, "Recording not found");
        }

        string? composer = null;
        string? orchestra = null;
        string? conductor = null;
        string? venue = null;
        List<string> soloists = [];
        List<string> recordingDates = [];
        int? recordingYear = null;
        Guid? workId = null;
        string? workTitle = null;
        Guid? parentWorkId = null;
        string? parentWorkTitle = null;

        if (fullRecording.ArtistCredit is { } credits)
        {
            foreach (INameCredit credit in credits)
            {
                string name = credit.Artist?.Name ?? "";
                if (
                    !IsNullOrWhiteSpace(name)
                    && (name.Contains("Orchestra") || name.Contains("Philharmonic"))
                )
                    orchestra ??= name;
            }
        }

        Log($"  RELATIONSHIPS ({fullRecording.Relationships?.Count() ?? 0}):");

        if (fullRecording.Relationships is { } rels)
        {
            foreach (IRelationship rel in rels)
            {
                LogRelationship("    ", rel);

                if (rel.Begin?.NearestDate is DateTime beginDate)
                {
                    string dateStr = beginDate.ToString("yyyy-MM-dd");
                    if (!recordingDates.Contains(dateStr))
                        recordingDates.Add(dateStr);
                    recordingYear ??= beginDate.Year;
                    Log($"      → date: {dateStr}");
                }

                if (
                    rel.Type?.Equals("recorded at", StringComparison.OrdinalIgnoreCase) == true
                    && rel.Place is { } place
                )
                {
                    venue = place.Name;
                    Log($"      → venue: {venue}");
                }

                if (rel.Work is { } work)
                {
                    workId = work.Id;
                    workTitle = work.Title;

                    ParsedWork? parsedWork = await LookupWorkAsync(work.Id, ct);
                    if (parsedWork is not null)
                    {
                        composer ??= parsedWork.Composer;
                        parentWorkId = parsedWork.ParentWorkId;
                        parentWorkTitle = parsedWork.ParentWorkTitle;
                    }
                    await Task.Delay(1100, ct);
                }

                if (rel.Artist is { } artist)
                {
                    string role = rel.Type?.ToLowerInvariant() ?? "";

                    Action? action = role switch
                    {
                        "conductor" or "director" => () =>
                        {
                            conductor = artist.Name;
                            Log($"      → conductor: {conductor}");
                        },
                        "orchestra" or "performing orchestra" or "performer" => () =>
                        {
                            orchestra = artist.Name;
                            Log($"      → orchestra: {orchestra}");
                        },
                        var r when IsSoloistRole(r) => () =>
                        {
                            string instrument = rel.Attributes?.FirstOrDefault() ?? role;
                            string soloistEntry = $"{artist.Name} ({instrument})";
                            if (!soloists.Contains(soloistEntry))
                            {
                                soloists.Add(soloistEntry);
                                Log($"      → soloist: {soloistEntry}");
                            }
                        },
                        _ => null,
                    };
                    action?.Invoke();
                }
            }
        }

        List<string> issues = [];

        if (composer is null)
            issues.Add("[CRITICAL] Missing composer");
        if (workTitle is null)
            issues.Add("[CRITICAL] Missing work title");
        if (parentWorkId is null && workId is not null)
            issues.Add("[CRITICAL] Missing parent work");
        if (recordingYear is null)
            issues.Add("[CRITICAL] Missing recording year");
        else if (recordingYear < EXPECTED_START_YEAR || recordingYear > EXPECTED_END_YEAR)
            issues.Add(
                $"[CRITICAL] Year {recordingYear} outside range {EXPECTED_START_YEAR}-{EXPECTED_END_YEAR}"
            );

        if (orchestra is null)
            issues.Add("[PERFORMER] Missing orchestra");
        if (conductor is null)
            issues.Add("[PERFORMER] Missing conductor");

        Log($"  FINAL:");
        Log($"    Composer:    {composer ?? "[MISSING]"}");
        Log($"    Work:        {workTitle ?? "[none]"}");
        Log($"    Parent:      {parentWorkTitle ?? "[none]"}");
        Log($"    Year:        {recordingYear?.ToString() ?? "[MISSING]"}");
        Log($"    Orchestra:   {orchestra ?? "[MISSING]"}");
        Log($"    Conductor:   {conductor ?? "[MISSING]"}");
        if (issues.Count > 0)
            Log($"    Issues:      {issues.Count} found");

        AnsiConsole.WriteLine();
        Print($"[cyan]Track {discNumber}.{trackNumber}[/] {Markup.Escape(trackTitle)}");
        PrintStatus("Composer", composer, composer is not null ? "green" : "red");
        PrintStatus("Work", workTitle, "blue");
        if (parentWorkTitle is not null)
            PrintStatus("Parent Work", parentWorkTitle, "blue");
        PrintStatus("Year", recordingYear?.ToString(), recordingYear.HasValue ? "green" : "red");
        PrintStatus("Orchestra", orchestra, orchestra is not null ? "green" : "yellow");
        PrintStatus("Conductor", conductor, conductor is not null ? "green" : "yellow");
        if (soloists.Count > 0)
            PrintStatus("Soloists", Join(", ", soloists), "cyan");

        OrmandyTrack newTrack = new(
            DiscNumber: discNumber,
            TrackNumber: trackNumber,
            Composer: composer,
            Title: trackTitle,
            WorkTitle: workTitle,
            WorkId: workId,
            RecordingYear: recordingYear,
            Orchestra: orchestra,
            Conductor: conductor,
            Soloists: soloists,
            Venue: venue,
            RecordingDates: recordingDates,
            Length: trackLength,
            ParentWorkTitle: parentWorkTitle,
            ParentWorkId: parentWorkId,
            MetadataIssues: issues.Count > 0 ? issues : null
        );

        string status = issues.Count switch
        {
            0 => "[green]  ✓ Added[/]",
            _ => $"[yellow]  ⚠ Added with {issues.Count} issue(s)[/]",
        };
        Log($"  ✓ PARSED (issues: {issues.Count})");
        Print(status);

        return new(true, newTrack, null);
    }

    public async Task<List<OrmandyTrack>> ParseAsync(
        bool forceRefresh = false,
        CancellationToken cancellationToken = default
    )
    {
        if (!forceRefresh)
        {
            BoxSetCache? cached = StateManager.LoadBoxSetCache<BoxSetCache>(BOX_SET_NAME);
            if (cached is not null)
            {
                TimeSpan age = DateTime.UtcNow - cached.FetchedAt;
                Print(
                    $"[green]Loaded {cached.Tracks.Count} tracks from cache[/] [dim](age: {age.TotalDays:F1} days)[/]"
                );
                Print("[dim]Use --refresh to re-fetch from MusicBrainz[/]");
                return cached.Tracks;
            }
        }
        else
        {
            Print("[yellow]Force refresh - ignoring cache[/]");
        }

        Log("═══════════════════════════════════════════════════════════════════════════════");
        Log($"ORMANDY BOX SET PARSE - {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        Log($"MusicBrainz Release: https://musicbrainz.org/release/{MusicBrainzReleaseId}");
        Log($"Expected year range: {EXPECTED_START_YEAR}-{EXPECTED_END_YEAR}");
        Log("═══════════════════════════════════════════════════════════════════════════════");

        Print("[blue]Fetching release from MusicBrainz...[/]");

        IRelease? release = await Query.LookupReleaseAsync(
            MusicBrainzReleaseId,
            Include.ArtistCredits
                | Include.Recordings
                | Include.Media
                | Include.Labels
                | Include.ArtistRelationships
                | Include.RecordingRelationships
                | Include.WorkRelationships
                | Include.Annotation,
            cancellationToken
        );

        if (release is null)
        {
            Print($"[red]Release not found: {MusicBrainzReleaseId}[/]");
            if (!Confirm("Abort parsing?"))
                return [];
            return [];
        }

        string releaseTitle = release.Title ?? "Unknown";
        string releaseArtist = release.ArtistCredit?.FirstOrDefault()?.Artist?.Name ?? "Unknown";
        int discCount = release.Media?.Count() ?? 0;

        Log($"Release: {releaseTitle}");
        Log($"Artist: {releaseArtist}");
        Log($"Discs: {discCount}");

        Print($"[green]✓[/] {Markup.Escape(releaseTitle)}");
        Print($"  [dim]Artist:[/] {Markup.Escape(releaseArtist)}");
        Print($"  [dim]Discs:[/]  {discCount}");
        Print(
            $"  [dim]Years:[/]  {EXPECTED_START_YEAR}-{EXPECTED_END_YEAR} (will prompt if outside)"
        );
        AnsiConsole.WriteLine();

        if (release.Media is null)
        {
            Print("[red]Release has no media[/]");
            return [];
        }

        int totalTracks = release.Media.Sum(m => m.TrackCount);
        List<OrmandyTrack> tracks = [];
        int parsedCount = 0;
        List<string> failureLogs = [];

        try
        {
            await AnsiConsole
                .Progress()
                .AutoClear(false)
                .HideCompleted(false)
                .Columns(
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new SpinnerColumn()
                )
                .StartAsync(async ctx =>
                {
                    ProgressTask progressTask = ctx.AddTask(
                        "[yellow]Parsing[/]",
                        maxValue: totalTracks
                    );

                    foreach (IMedium medium in release.Media)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        int discNumber = medium.Position;
                        Log(
                            "───────────────────────────────────────────────────────────────────────────────"
                        );
                        Log(
                            $"DISC {discNumber}: {medium.Title ?? "Untitled"} ({medium.TrackCount} tracks)"
                        );
                        Log(
                            "───────────────────────────────────────────────────────────────────────────────"
                        );

                        AnsiConsole.Write(
                            new Rule(
                                $"[yellow]Disc {discNumber}[/] {Markup.Escape(medium.Title ?? "")}"
                            ).LeftJustified()
                        );

                        if (medium.Tracks is null)
                        {
                            Log($"  WARNING: No tracks");
                            continue;
                        }

                        foreach (ITrack track in medium.Tracks)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            TrackParseResult result = await ParseTrackAsync(
                                track,
                                discNumber,
                                cancellationToken
                            );

                            if (result.Track is not null)
                            {
                                tracks.Add(result.Track);
                                parsedCount++;

                                // Track issues for logging
                                if (result.Track.MetadataIssues is { Count: > 0 } issues)
                                {
                                    foreach (string issue in issues)
                                        failureLogs.Add(
                                            $"Disc {discNumber} Track {track.Position}: {issue}"
                                        );
                                }

                                string composerDisplay = result.Track.Composer ?? "?";
                                progressTask.Description =
                                    $"[cyan]{result.Track.DiscNumber}.{result.Track.TrackNumber}[/] [dim]{Markup.Escape(TruncateTitle(composerDisplay, 20))}[/]";
                            }

                            progressTask.Increment(1);
                            await Task.Delay(1100, cancellationToken);
                        }
                    }
                });
        }
        finally
        {
            WriteLogFile();
            if (tracks.Count > 0)
                WriteDumpFile(tracks);

            if (failureLogs.Count > 0)
            {
                // Write issues file with sections
                List<string> criticalIssues = failureLogs
                    .Where(l => l.Contains("[CRITICAL]"))
                    .ToList();
                List<string> performerIssues = failureLogs
                    .Where(l => l.Contains("[PERFORMER]"))
                    .ToList();

                List<string> issueReport = ["═══ METADATA ISSUES REPORT ═══", ""];

                if (criticalIssues.Count > 0)
                {
                    issueReport.Add("─── CRITICAL ISSUES ───");
                    issueReport.AddRange(criticalIssues);
                    issueReport.Add("");
                }

                if (performerIssues.Count > 0)
                {
                    issueReport.Add("─── PERFORMER ISSUES ───");
                    issueReport.AddRange(performerIssues);
                    issueReport.Add("");
                }

                issueReport.Add($"═══ TOTAL: {failureLogs.Count} issues ═══");

                string issuePath = Combine(Paths.LogDirectory, "ormandy_issues.log");
                WriteAllLines(issuePath, issueReport);
                Print($"[yellow]Wrote {failureLogs.Count} issues to {issuePath}[/]");
            }
        }

        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[green]Summary[/]").LeftJustified());
        Print($"[green]✓ Parsed:[/] {parsedCount}/{totalTracks}");

        int issueCount = tracks.Count(t => t.MetadataIssues is { Count: > 0 });
        if (issueCount > 0)
            Print($"[yellow]⚠ Tracks with issues:[/] {issueCount}");

        int workCount = tracks
            .Where(t => t.WorkId.HasValue)
            .Select(t => t.WorkId)
            .Distinct()
            .Count();
        int parentWorkCount = tracks
            .Where(t => t.ParentWorkId.HasValue)
            .Select(t => t.ParentWorkId)
            .Distinct()
            .Count();
        Print($"[blue]Works:[/] {workCount} unique, {parentWorkCount} parent works");

        Log("");
        Log("═══════════════════════════════════════════════════════════════════════════════");
        Log($"SUMMARY: {parsedCount}/{totalTracks} parsed, {issueCount} with issues");
        Log($"Works: {workCount} unique, {parentWorkCount} parent");
        Log("═══════════════════════════════════════════════════════════════════════════════");

        BoxSetCache cache = new(
            Name: BOX_SET_NAME,
            MusicBrainzId: MusicBrainzReleaseId,
            FetchedAt: DateTime.UtcNow,
            TotalDiscs: discCount,
            TotalTracks: totalTracks,
            Tracks: tracks
        );
        StateManager.SaveBoxSetCache(BOX_SET_NAME, cache);
        Print($"[dim]Cached to state/boxsets/{BOX_SET_NAME}.json[/]");

        return tracks;
    }

    static string TruncateTitle(string title, int maxLength) =>
        title.Length <= maxLength ? title : title[..(maxLength - 3)] + "...";

    static bool IsSoloistRole(string role) =>
        role
            is "piano"
                or "violin"
                or "viola"
                or "cello"
                or "double bass"
                or "flute"
                or "oboe"
                or "clarinet"
                or "bassoon"
                or "horn"
                or "trumpet"
                or "trombone"
                or "tuba"
                or "harp"
                or "organ"
                or "harpsichord"
                or "soloist"
                or "vocal"
                or "soprano"
                or "mezzo-soprano"
                or "alto"
                or "tenor"
                or "baritone"
                or "bass"
                or "instrument";

    public void Display(List<OrmandyTrack> tracks)
    {
        AnsiConsole.WriteLine();

        var byParentWork = tracks
            .GroupBy(t => t.ParentWorkId ?? t.WorkId ?? Guid.Empty)
            .OrderBy(g =>
                tracks
                    .Where(t => (t.ParentWorkId ?? t.WorkId) == g.Key)
                    .Min(t => t.DiscNumber * 1000 + t.TrackNumber)
            );

        foreach (var workGroup in byParentWork)
        {
            OrmandyTrack first = workGroup.First();
            string workHeader =
                first.ParentWorkTitle ?? first.WorkTitle ?? first.Composer ?? "Unknown";

            AnsiConsole.Write(new Rule($"[blue]{Markup.Escape(workHeader)}[/]").LeftJustified());
            Print($"  [dim]Composer:[/] {Markup.Escape(first.Composer ?? "Unknown")}");

            foreach (
                OrmandyTrack track in workGroup.OrderBy(t => t.DiscNumber * 1000 + t.TrackNumber)
            )
            {
                string movement = track.WorkTitle ?? track.Title;
                string soloists =
                    track.Soloists.Count > 0 ? $" [cyan][{Join(", ", track.Soloists)}][/]" : "";
                Print(
                    $"    [dim]{track.DiscNumber}.{track.TrackNumber}[/] {Markup.Escape(movement)} [dim]({track.RecordingYear})[/]{soloists}"
                );
            }
            AnsiConsole.WriteLine();
        }

        int tracksWithIssues = tracks.Count(t => t.MetadataIssues is { Count: > 0 });
        if (tracksWithIssues > 0)
            Print($"[yellow]* {tracksWithIssues} tracks have metadata issues[/]");

        var yearStats = tracks.GroupBy(t => t.RecordingYear).OrderBy(g => g.Key);
        Print($"[dim]Years: {Join(", ", yearStats.Select(g => $"{g.Key} ({g.Count()})"))}[/]");
    }

    public string CreateAndWriteToSheet(List<OrmandyTrack> tracks)
    {
        string clientId =
            GetEnvironmentVariable("GOOGLE_CLIENT_ID")
            ?? throw new InvalidOperationException("GOOGLE_CLIENT_ID not set");
        string clientSecret =
            GetEnvironmentVariable("GOOGLE_CLIENT_SECRET")
            ?? throw new InvalidOperationException("GOOGLE_CLIENT_SECRET not set");

        GoogleSheetsService sheets = new(clientId, clientSecret);
        string sheetId = sheets.CreateSpreadsheet("Ormandy Columbia Legacy");

        List<object> headers =
        [
            "Disc",
            "Track",
            "Composer",
            "Has Issues",
            "Title",
            "Work",
            "Parent Work",
            "Year",
            "Orchestra",
            "Conductor",
            "Soloists",
            "Venue",
            "Recording Dates",
        ];
        sheets.EnsureSubsheetExists(sheetId, "Ormandy", headers);

        IList<IList<object>> rows = tracks
            .Select<OrmandyTrack, IList<object>>(t =>
                [
                    t.DiscNumber,
                    t.TrackNumber,
                    t.Composer ?? "",
                    t.MetadataIssues?.Count > 0 ? "Yes" : "",
                    t.Title,
                    t.WorkTitle ?? "",
                    t.ParentWorkTitle ?? "",
                    t.RecordingYear?.ToString() ?? "",
                    t.Orchestra ?? "",
                    t.Conductor ?? "",
                    Join(", ", t.Soloists),
                    t.Venue ?? "",
                    Join(", ", t.RecordingDates),
                ]
            )
            .ToList();

        sheets.AppendRows(sheetId, "Ormandy", rows);

        Print($"[green]Wrote {tracks.Count} tracks to sheet[/]");
        Print($"[link={GoogleSheetsService.GetSpreadsheetUrl(sheetId)}]Open spreadsheet[/]");

        return sheetId;
    }
}
