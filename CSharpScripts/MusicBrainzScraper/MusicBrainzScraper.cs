// using System.Collections.Concurrent;
// using System.Globalization;
// using System.Text.Json;
// using System.Text.RegularExpressions;
// using CsvHelper;
// using CsvHelper.Configuration;
// using MetaBrainz.MusicBrainz;
// using MetaBrainz.MusicBrainz.Interfaces.Entities;
// using Spectre.Console;

// {
//     public bool HasMissingCriticalFields =>
//         new[] { Composer, Orchestra, RecordingYear, RecordingPlace }.Any(IsUnknofwn);
//     public string FormattedDuration => Duration.ToString(@"hh\:mm\:ss");

//     public static bool IsUnknown(string v) => string.IsNullOrWhiteSpace(v) || v == "Unknown";
// }

// record DebugWorkInfo(string WorkTitle, string Issue, string RawData);

// class WorkMetadataMap : ClassMap<WorkMetadata>
// {
//     public WorkMetadataMap() => AutoMap(CultureInfo.InvariantCulture);
// }

// static class Extensions
// {
//     public static string FormatField(
//         this string? value,
//         string defaultValue = "Unknown",
//         bool allowEmpty = false
//     )
//     {
//         var isEmptyOrUnknown = string.IsNullOrWhiteSpace(value) || value == defaultValue;
//         if (isEmptyOrUnknown && allowEmpty)
//             return value?.EscapeMarkup() ?? "";
//         return isEmptyOrUnknown ? defaultValue : value!.EscapeMarkup();
//     }
// }

// class SpectreWorkHierarchyScraper
// {
//     const string ORMANDY_MBID = "9abb63b6-5fe0-4bec-a76d-43aad9349d0f";
//     const string OUTPUT_CSV = "ormandy_works_complete.csv";
//     const string DEBUG_FILE = "debug_missing_fields.txt";
//     const string JSON_DIR = "json_dumps";
//     const int RETRY_DELAY_MS = 1100;
//     const int MAX_RETRIES = 3;
//     const int MAX_DISCS_TO_PROCESS = 5;

//     static readonly string[] ComposerTypes =
//     [
//         "composer",
//         "writer",
//         "lyricist",
//         "librettist",
//         "arranger",
//     ];
//     static readonly string[] OrchestraTypes =
//     [
//         "orchestra",
//         "ensemble",
//         "choir",
//         "chorus",
//         "performing orchestra",
//     ];
//     static readonly string[] SoloistTypes =
//     [
//         "instrument",
//         "vocal",
//         "vocals",
//         "performer",
//         "performance",
//         "solo",
//     ];
//     static readonly string[] VenueTypes =
//     [
//         "recorded at",
//         "recorded in",
//         "performance",
//         "live performance",
//     ];
//     static readonly string[] EventTypes =
//     [
//         "recorded at",
//         "recorded in",
//         "performance",
//         "recorded",
//         "recording of",
//     ];
//     static readonly ConcurrentBag<object> JsonIssues = new();

//     static async Task Main()
//     {
//         Directory.CreateDirectory(JSON_DIR);
//         AnsiConsole.Write(new FigletText("Ormandy Scraper").LeftJustified().Color(Color.Green));
//         var client = new Query("SpectreHierarchyScraper", "2.0", "kanishknishar@outlook.com");
//         await ProcessRelease(client);
//         AnsiConsole.MarkupLine($"[green]Complete! CSV exported to {OUTPUT_CSV.EscapeMarkup()}[/]");
//         AnsiConsole.MarkupLine($"[yellow]Debug info saved to {DEBUG_FILE.EscapeMarkup()}[/]");
//         if (!JsonIssues.IsEmpty)
//             AnsiConsole.MarkupLine(
//                 $"[cyan]JSON forensic dump saved in {JSON_DIR.EscapeMarkup()}[/]"
//             );
//     }

//     static async Task ProcessRelease(Query client)
//     {
//         var release = await FetchWithRetry(
//             () =>
//                 client.LookupReleaseAsync(
//                     Guid.Parse(ORMANDY_MBID),
//                     Include.Recordings
//                         | Include.ArtistCredits
//                         | Include.RecordingLevelRelationships
//                         | Include.WorkLevelRelationships
//                         | Include.WorkRelationships
//                         | Include.EventRelationships
//                         | Include.PlaceRelationships
//                 ),
//             "Release lookup",
//             null!
//         );
//         if (release?.Media == null)
//         {
//             AnsiConsole.MarkupLine("[red]No media found[/]");
//             return;
//         }

//         var works =
//             new ConcurrentDictionary<
//                 Guid,
//                 (WorkMetadata metadata, int trackCount, ConcurrentBag<string> trackTitles)
//             >();
//         var debugInfo = new ConcurrentBag<DebugWorkInfo>();
//         var totalTracks = release.Media.Sum(m => m.Tracks?.Count ?? 0);
//         var processed = 0;
//         var processedLock = new object();

//         await AnsiConsole
//             .Live(BuildTable(works, 0, totalTracks))
//             .StartAsync(async ctx =>
//             {
//                 var trackTasks = new List<(ITrack track, int discNo)>();
//                 foreach (var medium in release.Media.Take(MAX_DISCS_TO_PROCESS))
//                 foreach (var track in medium.Tracks ?? Enumerable.Empty<ITrack>())
//                     trackTasks.Add((track, medium.Position));

//                 await Parallel.ForEachAsync(
//                     trackTasks,
//                     async (ti, ct) =>
//                     {
//                         await ProcessTrack(ti.track, ti.discNo, release, client, works, debugInfo);
//                         lock (processedLock)
//                             processed++;
//                         UpdateDisplay(ctx, works, processed, totalTracks, ti.track.Title ?? "");
//                     }
//                 );
//             });

//         var standardDict = works.ToDictionary(
//             kvp => kvp.Key,
//             kvp =>
//                 (
//                     kvp.Value.metadata,
//                     kvp.Value.trackCount,
//                     kvp.Value.trackTitles.ToList() as IList<string>
//                 )
//         );
//         await WriteWorksToCSV(standardDict);
//         await SaveDebugInfo(debugInfo.ToList());
//         await SaveJsonIssues(release);
//     }

//     static async Task ProcessTrack(
//         ITrack track,
//         int discNo,
//         IRelease release,
//         Query client,
//         ConcurrentDictionary<
//             Guid,
//             (WorkMetadata metadata, int trackCount, ConcurrentBag<string> trackTitles)
//         > works,
//         ConcurrentBag<DebugWorkInfo> debugInfo
//     )
//     {
//         var rec = await FetchCompleteRecording(client, track.Recording);
//         var directWork = ExtractWorkFromRecording(rec);
//         if (directWork == null)
//         {
//             JsonIssues.Add(
//                 new
//                 {
//                     Reason = "No work extracted",
//                     Recording = rec,
//                     TrackTitle = track.Title,
//                 }
//             );
//             return;
//         }

//         var parentWork = await FindUltimateParentWork(client, directWork);
//         if (parentWork == null)
//         {
//             JsonIssues.Add(
//                 new
//                 {
//                     Reason = "Parent work resolution failed",
//                     Recording = rec,
//                     Work = directWork,
//                     TrackTitle = track.Title,
//                 }
//             );
//             return;
//         }

//         var workId = parentWork.Id;
//         var trackDuration = track.Length ?? TimeSpan.Zero;

//         var newMeta = await BuildWorkMetadata(parentWork, rec, release, track, discNo, client);
//         works.AddOrUpdate(
//             workId,
//             _ =>
//             {
//                 CheckForMissingFields(newMeta, rec, debugInfo);
//                 return (newMeta, 1, new ConcurrentBag<string> { track.Title ?? "" });
//             },
//             (_, existing) =>
//             {
//                 var updated = UpdateExistingWork(existing.metadata, rec, trackDuration);
//                 existing.trackTitles.Add(track.Title ?? "");
//                 return (updated, existing.trackCount + 1, existing.trackTitles);
//             }
//         );
//     }

//     static async Task WriteWorksToCSV(
//         Dictionary<Guid, (WorkMetadata metadata, int trackCount, IList<string> trackTitles)> works
//     )
//     {
//         using var sw = new StreamWriter(OUTPUT_CSV, false, Encoding.UTF8);
//         using var csv = new CsvWriter(
//             sw,
//             new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true }
//         );
//         csv.Context.RegisterClassMap<WorkMetadataMap>();
//         csv.WriteHeader<WorkMetadata>();
//         await csv.NextRecordAsync();

//         foreach (
//             var entry in works
//                 .Values.OrderBy(e => e.metadata.DiscNumber)
//                 .ThenBy(e => e.metadata.WorkTitle)
//         )
//         {
//             var title =
//                 entry.trackCount == 1
//                     ? entry.trackTitles.FirstOrDefault() ?? entry.metadata.WorkTitle
//                     : entry.metadata.WorkTitle;
//             var finalMeta = entry.metadata with { WorkTitle = title };
//             csv.WriteRecord(finalMeta);
//             await csv.NextRecordAsync();
//         }
//     }

//     static async Task SaveJsonIssues(IRelease release)
//     {
//         if (JsonIssues.IsEmpty)
//             return;
//         var sessionFile = Path.Combine(JSON_DIR, $"session_{DateTime.Now:yyyyMMdd_HHmmss}.json");
//         var payload = new
//         {
//             ReleaseId = release.Id,
//             ReleaseTitle = release.Title,
//             Issues = JsonIssues.ToList(),
//         };
//         var json = JsonSerializer.Serialize(
//             payload,
//             new JsonSerializerOptions { WriteIndented = true }
//         );
//         await File.WriteAllTextAsync(sessionFile, json, Encoding.UTF8);
//         var desktopLog = Path.Combine(
//             Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
//             "scraper_debug_log.txt"
//         );
//         var logLine =
//             $"{DateTime.Now:O} - {JsonIssues.Count} issues recorded -> {sessionFile} - reasons: {string.Join("; ", JsonIssues.Take(20).Select(i => (i as dynamic).Reason))}{Environment.NewLine}";
//         await File.AppendAllTextAsync(desktopLog, logLine);
//     }

//     static WorkMetadata UpdateExistingWork(
//         WorkMetadata existing,
//         IRecording rec,
//         TimeSpan trackDuration
//     )
//     {
//         var newDuration = existing.Duration + trackDuration;
//         var newYear = ConsolidateYears(
//             existing.RecordingYear,
//             ExtractRecordingYear(rec) ?? "Unknown"
//         );
//         return existing with { Duration = newDuration, RecordingYear = newYear };
//     }

//     static void CheckForMissingFields(
//         WorkMetadata wm,
//         IRecording rec,
//         ConcurrentBag<DebugWorkInfo> debugInfo
//     )
//     {
//         if (!wm.HasMissingCriticalFields)
//             return;
//         var issues = new List<string>();
//         if (WorkMetadata.IsUnknown(wm.Composer))
//             issues.Add("Missing Composer");
//         if (WorkMetadata.IsUnknown(wm.Orchestra))
//             issues.Add("Missing Orchestra");
//         if (WorkMetadata.IsUnknown(wm.RecordingYear))
//             issues.Add("Missing Recording Year");
//         if (WorkMetadata.IsUnknown(wm.RecordingPlace))
//             issues.Add("Missing Recording Venue");
//         var rawData =
//             $"Recording ID: {rec.Id}, Artists: {string.Join("; ", rec.ArtistCredit?.Select(ac => ac.Artist?.Name) ?? new[] { "None" })}, Relations: {rec.Relationships?.Count ?? 0}";
//         debugInfo.Add(new DebugWorkInfo(wm.WorkTitle, string.Join(", ", issues), rawData));
//         JsonIssues.Add(
//             new
//             {
//                 Reason = "Critical fields missing",
//                 Issues = issues,
//                 Recording = rec,
//             }
//         );
//     }

//     static async Task SaveDebugInfo(List<DebugWorkInfo> debugInfo)
//     {
//         using var writer = new StreamWriter(DEBUG_FILE, false, Encoding.UTF8);
//         await writer.WriteLineAsync("Works with Missing Critical Fields");
//         await writer.WriteLineAsync(new string('=', 50));
//         await writer.WriteLineAsync();
//         foreach (var info in debugInfo.OrderBy(d => d.WorkTitle))
//         {
//             await writer.WriteLineAsync($"Work: {info.WorkTitle}");
//             await writer.WriteLineAsync($"Issues: {info.Issue}");
//             await writer.WriteLineAsync($"Raw Data: {info.RawData}");
//             await writer.WriteLineAsync();
//         }
//         await writer.WriteLineAsync($"Total works with missing fields: {debugInfo.Count}");
//     }

//     static void UpdateDisplay(
//         LiveDisplayContext ctx,
//         ConcurrentDictionary<
//             Guid,
//             (WorkMetadata metadata, int trackCount, ConcurrentBag<string> trackTitles)
//         > works,
//         int processed,
//         int total,
//         string currentTrack
//     )
//     {
//         ctx.UpdateTarget(BuildTable(works, processed, total, currentTrack));
//     }

//     static Table BuildTable(
//         ConcurrentDictionary<
//             Guid,
//             (WorkMetadata metadata, int trackCount, ConcurrentBag<string> trackTitles)
//         > works,
//         int processed,
//         int total,
//         string currentTrack = ""
//     )
//     {
//         var table = new Table().Border(TableBorder.Rounded).BorderColor(Color.Grey).Expand();
//         table.AddColumn("Composer");
//         table.AddColumn("Work Title");
//         table.AddColumn("Orchestra");
//         table.AddColumn("Soloists");
//         table.AddColumn("Year");
//         table.AddColumn("Venue");
//         table.AddColumn("Disc");
//         table.AddColumn("Duration");
//         var sorted = works
//             .Values.Select(w => w.metadata)
//             .OrderBy(w => w.DiscNumber)
//             .ThenBy(w => w.WorkTitle);
//         foreach (var w in sorted)
//         {
//             var rowColor = w.HasMissingCriticalFields ? Color.Red : Color.White;
//             table.AddRow(
//                 new Markup(w.Composer.FormatField()),
//                 new Markup(w.WorkTitle.FormatField()),
//                 new Markup(w.Orchestra.FormatField()),
//                 new Markup(w.Soloists.FormatField("None", true)),
//                 new Markup(w.RecordingYear.FormatField()),
//                 new Markup(w.RecordingPlace.FormatField()),
//                 new Markup(w.DiscNumber.FormatField()),
//                 new Markup(w.FormattedDuration)
//             );
//         }
//         AddProgressCaption(table, processed, total, works.Count, currentTrack);
//         return table;
//     }

//     static void AddProgressCaption(
//         Table table,
//         int processed,
//         int total,
//         int workCount,
//         string currentTrack
//     )
//     {
//         var progress = total > 0 ? (double)processed / total * 100 : 0;
//         var progressBar = new string('█', Math.Min(40, (int)(progress * 40 / 100)));
//         var emptyBar = new string('░', 40 - progressBar.Length);
//         var caption =
//             $"[green]{progressBar}[/][grey]{emptyBar}[/] {processed}/{total} tracks | {workCount} unique works";
//         if (!string.IsNullOrEmpty(currentTrack))
//             caption += $"\n[yellow]Current:[/] {currentTrack.EscapeMarkup()}";
//         table.Caption(caption);
//     }

//     static async Task<IWork> FindUltimateParentWork(Query client, IWork work)
//     {
//         var current = work;
//         var visited = new HashSet<Guid> { work.Id };
//         while (true)
//         {
//             var full = await FetchWithRetry(
//                 () => client.LookupWorkAsync(current.Id, Include.WorkRelationships),
//                 "Work lookup",
//                 current
//             );
//             if (full?.Relationships == null)
//                 break;
//             var parent = FindParentWork(full, current, visited);
//             if (parent == null)
//                 break;
//             visited.Add(parent.Id);
//             current = parent;
//         }
//         return current;
//     }

//     static IWork? FindParentWork(IWork fullWork, IWork currentWork, HashSet<Guid> visited)
//     {
//         return fullWork
//             .Relationships?.Where(r =>
//                 string.Equals(r.Type, "parts", StringComparison.OrdinalIgnoreCase)
//                 && r.Target is IWork
//             )
//             .Select(r => (IWork)r.Target!)
//             .FirstOrDefault(t => !visited.Contains(t.Id));
//     }

//     static IWork? ExtractWorkFromRecording(IRecording rec)
//     {
//         if (rec.Relationships == null)
//             return null;
//         var rel = rec
//             .Relationships.Where(r => r.Target is IWork)
//             .OrderBy(r =>
//                 string.Equals(r.Type, "performance", StringComparison.OrdinalIgnoreCase) ? 0 : 1
//             )
//             .FirstOrDefault();
//         return rel?.Target as IWork;
//     }

//     static string ExtractRelationTarget(IReadOnlyList<IRelationship>? relations, string[] types)
//     {
//         if (relations == null)
//             return "";
//         var rel = relations.FirstOrDefault(r =>
//             types.Any(t => string.Equals(r.Type, t, StringComparison.OrdinalIgnoreCase))
//         );
//         return rel?.Target switch
//         {
//             IArtist a => a.Name ?? "",
//             IPlace p => p.Name ?? "",
//             _ => "",
//         };
//     }

//     static string? ExtractFromArtistCredits(IReadOnlyList<INameCredit>? credits, string[] types)
//     {
//         if (credits == null)
//             return null;
//         var byType = credits.FirstOrDefault(c =>
//             types.Any(t => string.Equals(c.Artist?.Type, t, StringComparison.OrdinalIgnoreCase))
//         );
//         if (byType?.Artist?.Name != null)
//             return byType.Artist.Name;
//         foreach (var c in credits)
//             if (
//                 !string.IsNullOrEmpty(c.Artist?.Name)
//                 && types.Any(t =>
//                     Regex.IsMatch(
//                         c.Artist.Name,
//                         $"\\b{Regex.Escape(t)}\\b",
//                         RegexOptions.IgnoreCase
//                     )
//                 )
//             )
//                 return c.Artist.Name;
//         return null;
//     }

//     static string ExtractSoloists(IRecording rec)
//     {
//         var soloists =
//             rec.Relationships?.Where(r =>
//                     SoloistTypes.Any(t =>
//                         (r.Type ?? "").IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0
//                     )
//                     && r.Target is IArtist
//                 )
//                 .Select(r => ((IArtist)r.Target!).Name)
//                 .Distinct()
//                 .ToList() ?? new();
//         return soloists.Any() ? string.Join(", ", soloists) : "None";
//     }

//     static string? ExtractRecordingYear(IRecording rec)
//     {
//         return rec
//             .Relationships?.FirstOrDefault(r =>
//                 EventTypes.Any(t => string.Equals(r.Type, t, StringComparison.OrdinalIgnoreCase))
//             )
//             ?.Begin?.Year?.ToString();
//     }

//     static string ConsolidateYears(string existing, string newYear)
//     {
//         if (string.IsNullOrWhiteSpace(newYear) || newYear == "Unknown")
//             return existing;
//         if (string.IsNullOrWhiteSpace(existing) || existing == "Unknown")
//             return newYear;
//         if (existing.Contains(newYear))
//             return existing;
//         var years = ExtractYearsFromString(existing);
//         if (int.TryParse(newYear, out var y))
//             years.Add(y);
//         return FormatYearRanges(years);
//     }

//     static HashSet<int> ExtractYearsFromString(string s)
//     {
//         var years = new HashSet<int>();
//         foreach (var part in s.Split([',', '-'], StringSplitOptions.RemoveEmptyEntries))
//             if (int.TryParse(part.Trim(), out var y))
//                 years.Add(y);
//         return years;
//     }

//     static string FormatYearRanges(HashSet<int> years)
//     {
//         if (years.Count == 0)
//             return "Unknown";
//         if (years.Count == 1)
//             return years.First().ToString();
//         var sorted = years.OrderBy(y => y).ToList();
//         var result = new List<string>();
//         var start = sorted[0];
//         var end = sorted[0];
//         for (int i = 1; i < sorted.Count; i++)
//         {
//             if (sorted[i] == end + 1)
//                 end = sorted[i];
//             else
//             {
//                 result.Add(FormatYearRange(start, end));
//                 start = end = sorted[i];
//             }
//         }
//         result.Add(FormatYearRange(start, end));
//         return string.Join(", ", result);
//     }

//     static string FormatYearRange(int start, int end) =>
//         start == end ? start.ToString()
//         : end == start + 1 ? $"{start}, {end}"
//         : $"{start}-{end}";

//     static async Task<T?> FetchWithRetry<T>(Func<Task<T>> op, string label, T? defaultValue = null)
//         where T : class
//     {
//         for (int i = 1; i <= MAX_RETRIES; i++)
//         {
//             try
//             {
//                 await Task.Delay(i == 1 ? 0 : RETRY_DELAY_MS * (i - 1));
//                 return await op();
//             }
//             catch (HttpRequestException) when (i < MAX_RETRIES) { }
//             catch when (i < MAX_RETRIES) { }
//         }
//         AnsiConsole.MarkupLine($"[red]Failed {label} after {MAX_RETRIES} retries.[/]");
//         return defaultValue;
//     }

//     static async Task<IRecording> FetchCompleteRecording(Query client, IRecording? rec)
//     {
//         if (rec?.Relationships?.Any() == true)
//             return rec;
//         return await FetchWithRetry(
//                 () =>
//                     client.LookupRecordingAsync(
//                         rec!.Id,
//                         Include.WorkRelationships
//                             | Include.ArtistRelationships
//                             | Include.RecordingRelationships
//                             | Include.EventRelationships
//                             | Include.PlaceRelationships
//                             | Include.ArtistCredits
//                     ),
//                 "Recording lookup",
//                 rec!
//             ) ?? rec!;
//     }

//     static async Task<WorkMetadata> BuildWorkMetadata(
//         IWork work,
//         IRecording rec,
//         IRelease release,
//         ITrack track,
//         int discNo,
//         Query client
//     )
//     {
//         var composer = ExtractRelationTarget(rec.Relationships, ComposerTypes);
//         if (string.IsNullOrEmpty(composer))
//         {
//             composer = ExtractRelationTarget(work.Relationships, ComposerTypes);
//         }
//         if (string.IsNullOrEmpty(composer))
//         {
//             var fullWork = await FetchWithRetry(
//                 () => client.LookupWorkAsync(work.Id, Include.ArtistRelationships),
//                 "Work composer lookup",
//                 work
//             );
//             composer = ExtractRelationTarget(fullWork?.Relationships, ComposerTypes) ?? "Unknown";
//         }

//         var workTitle = work.Title ?? "Unknown";
//         var orchestra = ExtractOrchestra(rec, release);
//         var soloists = ExtractSoloists(rec);
//         var year = ExtractRecordingYear(rec) ?? release.Date?.Year?.ToString() ?? "Unknown";
//         var place = ExtractRelationTarget(rec.Relationships, VenueTypes) ?? "Unknown";

//         return new WorkMetadata(
//             composer,
//             workTitle,
//             orchestra,
//             soloists,
//             year,
//             place,
//             discNo.ToString("D3"),
//             track.Length ?? TimeSpan.Zero
//         );
//     }

//     static string ExtractOrchestra(IRecording rec, IRelease release)
//     {
//         return ExtractRelationTarget(rec.Relationships, OrchestraTypes)
//             ?? ExtractFromArtistCredits(rec.ArtistCredit, OrchestraTypes)
//             ?? ExtractFromArtistCredits(release.ArtistCredit, OrchestraTypes)
//             ?? "Unknown";
//     }
// }

// record WorkMetadata(
//     string Composer,
//     string WorkTitle,
//     string Orchestra,
//     string Soloists,
//     string RecordingYear,
//     string RecordingPlace,
//     string DiscNumber,
//     TimeSpan Duration
// )