

// using System.Globalization;
// using System.Text;
// using CsvHelper;
// using CsvHelper.Configuration;
// using MetaBrainz.MusicBrainz;
// using MetaBrainz.MusicBrainz.Interfaces.Entities;
// using Spectre.Console;

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
// {
//     public bool HasMissingCriticalFields =>
//         IsUnknown(Composer)
//         || IsUnknown(Orchestra)
//         || IsUnknown(RecordingYear)
//         || IsUnknown(RecordingPlace);

//     public static bool IsUnknown(string value) =>
//         string.IsNullOrWhiteSpace(value) || value == "Unknown";

//     public string FormattedDuration =>
//         $"{(int)Duration.TotalHours:D2}:{Duration.Minutes:D2}:{Duration.Seconds:D2}";
// }

// record DebugWorkInfo(string WorkTitle, string Issue, string RawData);

// class WorkMetadataMap : ClassMap<WorkMetadata>
// {
//     public WorkMetadataMap()
//     {
//         Map(m => m.Composer);
//         Map(m => m.WorkTitle);
//         Map(m => m.Orchestra);
//         Map(m => m.Soloists);
//         Map(m => m.RecordingYear);
//         Map(m => m.RecordingPlace);
//         Map(m => m.DiscNumber);
//         Map(m => m.Duration).TypeConverter<TimeSpanConverter>();
//     }
// }

// class TimeSpanConverter : CsvHelper.TypeConversion.ITypeConverter
// {
//     public object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
//     {
//         TimeSpan.TryParseExact(text, @"hh\:mm\:ss", CultureInfo.InvariantCulture, out var result);
//         return result;
//     }

//     public string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
//     {
//         if (value is TimeSpan ts)
//             return $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
//         return "00:00:00";
//     }
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
//     const int RETRY_DELAY_MS = 1100;
//     const int MAX_RETRIES = 3;

//     static readonly string[] ComposerTypes =
//     {
//         "composer",
//         "writer",
//         "lyricist",
//         "librettist",
//         "arranger",
//     };
//     static readonly string[] OrchestraTypes =
//     {
//         "orchestra",
//         "ensemble",
//         "choir",
//         "chorus",
//         "performing orchestra",
//     };
//     static readonly string[] SoloistTypes =
//     {
//         "instrument",
//         "vocal",
//         "vocals",
//         "performer",
//         "performance",
//         "solo",
//     };
//     static readonly string[] VenueTypes =
//     {
//         "recorded at",
//         "recorded in",
//         "performance",
//         "live performance",
//     };
//     static readonly string[] EventTypes =
//     {
//         "recorded at",
//         "recorded in",
//         "performance",
//         "recorded",
//         "recording of",
//     };

//     static async Task Main()
//     {
//         AnsiConsole.Write(new FigletText("Ormandy Scraper").LeftJustified().Color(Color.Green));

//         var client = new Query("SpectreHierarchyScraper", "2.0", "kanishknishar@outlook.com");
//         await ProcessRelease(client);

//         AnsiConsole.MarkupLine($"[green]Complete! CSV exported to {OUTPUT_CSV.EscapeMarkup()}[/]");
//         AnsiConsole.MarkupLine($"[yellow]Debug info saved to {DEBUG_FILE.EscapeMarkup()}[/]");
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
//             "Release lookup"
//         );

//         if (release?.Media == null)
//         {
//             AnsiConsole.MarkupLine("[red]No media found[/]");
//             return;
//         }

//         var works = new Dictionary<Guid, (WorkMetadata metadata, int trackCount)>();
//         var debugInfo = new List<DebugWorkInfo>();
//         var totalTracks = release.Media.Sum(m => m.Tracks?.Count ?? 0);

//         await WriteWorksToCSV(release, client, works, debugInfo, totalTracks);
//         await SaveDebugInfo(debugInfo);
//     }

//     static async Task WriteWorksToCSV(
//         IRelease release,
//         Query client,
//         Dictionary<Guid, (WorkMetadata metadata, int trackCount)> works,
//         List<DebugWorkInfo> debugInfo,
//         int totalTracks
//     )
//     {
//         using var sw = new StreamWriter(OUTPUT_CSV, false, Encoding.UTF8);
//         using var csv = new CsvWriter(
//             sw,
//             new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true }
//         );
//         csv.Context.RegisterClassMap<WorkMetadataMap>();

//         await AnsiConsole
//             .Live(BuildTable(works, 0, totalTracks))
//             .StartAsync(async ctx =>
//             {
//                 int processed = 0;
//                 foreach (var medium in release.Media)
//                 {
//                     foreach (var track in medium.Tracks ?? Enumerable.Empty<ITrack>())
//                     {
//                         processed++;
//                         await ProcessTrack(
//                             track,
//                             medium.Position,
//                             release,
//                             client,
//                             works,
//                             debugInfo,
//                             csv
//                         );
//                         await sw.FlushAsync();
//                         UpdateDisplay(ctx, works, processed, totalTracks, track.Title ?? "");
//                     }
//                 }
//             });
//     }

//     static async Task ProcessTrack(
//         ITrack track,
//         int discNo,
//         IRelease release,
//         Query client,
//         Dictionary<Guid, (WorkMetadata metadata, int trackCount)> works,
//         List<DebugWorkInfo> debugInfo,
//         CsvWriter csv
//     )
//     {
//         var rec = await FetchCompleteRecording(client, track.Recording);
//         var directWork = ExtractWorkFromRecording(rec);

//         if (directWork == null)
//             return;

//         var parentWork = await FindUltimateParentWork(client, directWork);
//         var workId = parentWork.Id;
//         var trackDuration = track.Length ?? TimeSpan.Zero;

//         if (works.TryGetValue(workId, out var existing))
//         {
//             var updatedMetadata = UpdateExistingWork(existing.metadata, rec, trackDuration);
//             works[workId] = (updatedMetadata, existing.trackCount + 1);
//         }
//         else
//         {
//             var newMetadata = await BuildWorkMetadata(
//                 parentWork,
//                 rec,
//                 release,
//                 track,
//                 discNo,
//                 client
//             );
//             works[workId] = (newMetadata, 1);
//             CheckForMissingFields(newMetadata, rec, debugInfo);
//         }

//         // Use track name for single-track works (excerpts/overtures)
//         var finalMetadata =
//             works[workId].trackCount == 1
//                 ? works[workId].metadata with
//                 {
//                     WorkTitle = track.Title ?? works[workId].metadata.WorkTitle,
//                 }
//                 : works[workId].metadata;

//         csv.WriteRecord(finalMetadata);
//         await csv.NextRecordAsync();
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
//         List<DebugWorkInfo> debugInfo
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
//         Dictionary<Guid, (WorkMetadata metadata, int trackCount)> works,
//         int processed,
//         int total,
//         string currentTrack
//     )
//     {
//         ctx.UpdateTarget(BuildTable(works, processed, total, currentTrack));
//     }

//     static Table BuildTable(
//         Dictionary<Guid, (WorkMetadata metadata, int trackCount)> works,
//         int processed,
//         int total,
//         string currentTrack = ""
//     )
//     {
//         var table = new Table().Border(TableBorder.Rounded).BorderColor(Color.Grey).Expand();
//         table.AddColumn(new TableColumn("Composer"));
//         table.AddColumn(new TableColumn("Work Title"));
//         table.AddColumn(new TableColumn("Orchestra"));
//         table.AddColumn(new TableColumn("Soloists"));
//         table.AddColumn(new TableColumn("Year"));
//         table.AddColumn(new TableColumn("Venue"));
//         table.AddColumn(new TableColumn("Disc"));
//         table.AddColumn(new TableColumn("Duration"));

//         var sortedWorks = works
//             .Values.Select(w => w.metadata)
//             .OrderBy(w => w.DiscNumber)
//             .ThenBy(w => w.WorkTitle);

//         foreach (var w in sortedWorks)
//         {
//             var color = w.HasMissingCriticalFields ? "red" : "white";
//             table.AddRow(
//                 $"[{color}]{w.Composer.FormatField()}[/]",
//                 $"[{color}]{w.WorkTitle.FormatField()}[/]",
//                 $"[{color}]{w.Orchestra.FormatField()}[/]",
//                 $"[{color}]{w.Soloists.FormatField("None", true)}[/]",
//                 $"[{color}]{w.RecordingYear.FormatField()}[/]",
//                 $"[{color}]{w.RecordingPlace.FormatField()}[/]",
//                 $"[{color}]{w.DiscNumber.FormatField()}[/]",
//                 $"[{color}]{w.FormattedDuration}[/]"
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
//             caption += $"\n[yellow]Current:[/] {currentTrack}";

//         table.Caption(caption);
//     }

//     static async Task<IWork> FindUltimateParentWork(Query client, IWork work)
//     {
//         var currentWork = work;
//         var visited = new HashSet<Guid> { work.Id };

//         while (true)
//         {
//             var fullWork = await FetchWithRetry(
//                 () => client.LookupWorkAsync(currentWork.Id, Include.WorkRelationships),
//                 "Work lookup"
//             );

//             if (fullWork?.Relationships == null)
//                 break;

//             var parentWork = FindParentWork(fullWork, currentWork, visited);
//             if (parentWork == null)
//                 break;

//             visited.Add(parentWork.Id);
//             currentWork = parentWork;
//         }

//         return currentWork;
//     }

//     static IWork? FindParentWork(IWork fullWork, IWork currentWork, HashSet<Guid> visited)
//     {
//         return fullWork
//             .Relationships?.Where(r =>
//                 string.Equals(r.Type, "parts", StringComparison.OrdinalIgnoreCase)
//                 && r.Target is IWork
//             )
//             .Select(r => (IWork)r.Target!)
//             .Where(targetWork =>
//                 !visited.Contains(targetWork.Id) && IsValidParentWork(currentWork, targetWork)
//             )
//             .FirstOrDefault();
//     }

//     static bool IsValidParentWork(IWork currentWork, IWork targetWork)
//     {
//         var currentTitle = currentWork.Title ?? "";
//         var targetTitle = targetWork.Title ?? "";
//         return !targetTitle.Contains(currentTitle, StringComparison.OrdinalIgnoreCase)
//             || targetTitle.Length < currentTitle.Length;
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
//         var fullWork = await FetchWithRetry(
//             () => client.LookupWorkAsync(work.Id, Include.ArtistRelationships),
//             "Work composer lookup"
//         );

//         return new WorkMetadata(
//             ExtractRelationTarget(fullWork?.Relationships, ComposerTypes) ?? "Unknown",
//             work.Title ?? "Unknown",
//             ExtractOrchestra(rec, release),
//             ExtractSoloists(rec),
//             ExtractRecordingYear(rec) ?? "Unknown",
//             ExtractRelationTarget(rec.Relationships, VenueTypes) ?? "Unknown",
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
//                 "Recording lookup"
//             ) ?? rec!;
//     }

//     static IWork? ExtractWorkFromRecording(IRecording recording)
//     {
//         return recording
//                 .Relationships?.Where(r => r.Target is IWork)
//                 .OrderBy(r => r.Type == "performance" ? 0 : 1)
//                 .FirstOrDefault()
//                 ?.Target as IWork;
//     }

//     static string ExtractRelationTarget(IReadOnlyList<IRelationship>? relations, string[] types)
//     {
//         if (relations == null)
//             return "";

//         var rel = relations.FirstOrDefault(r =>
//             types.Any(t => (r.Type ?? "").Contains(t, StringComparison.OrdinalIgnoreCase))
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
//         return credits
//             ?.FirstOrDefault(c =>
//                 types.Any(t =>
//                     c.Artist?.Name?.Contains(t, StringComparison.OrdinalIgnoreCase) == true
//                 )
//             )
//             ?.Artist?.Name;
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

//     static string ConsolidateYears(string existingYears, string newYear)
//     {
//         if (string.IsNullOrWhiteSpace(newYear) || newYear == "Unknown")
//             return existingYears;
//         if (string.IsNullOrWhiteSpace(existingYears) || existingYears == "Unknown")
//             return newYear;
//         if (existingYears.Contains(newYear))
//             return existingYears;

//         var years = ExtractYearsFromString(existingYears);
//         if (int.TryParse(newYear, out var newYearInt))
//             years.Add(newYearInt);

//         return FormatYearRanges(years);
//     }

//     static HashSet<int> ExtractYearsFromString(string yearString)
//     {
//         var years = new HashSet<int>();
//         var parts = yearString.Split(new[] { ',', '-' }, StringSplitOptions.RemoveEmptyEntries);

//         foreach (var part in parts)
//         {
//             if (int.TryParse(part.Trim(), out var year))
//                 years.Add(year);
//         }

//         return years;
//     }

//     static string FormatYearRanges(HashSet<int> years)
//     {
//         if (years.Count == 0)
//             return "Unknown";
//         if (years.Count == 1)
//             return years.First().ToString();

//         var sortedYears = years.OrderBy(y => y).ToList();
//         var result = new List<string>();
//         var rangeStart = sortedYears[0];
//         var rangeEnd = sortedYears[0];

//         for (int i = 1; i < sortedYears.Count; i++)
//         {
//             if (sortedYears[i] == rangeEnd + 1)
//             {
//                 rangeEnd = sortedYears[i];
//             }
//             else
//             {
//                 result.Add(FormatYearRange(rangeStart, rangeEnd));
//                 rangeStart = rangeEnd = sortedYears[i];
//             }
//         }

//         result.Add(FormatYearRange(rangeStart, rangeEnd));
//         return string.Join(", ", result);
//     }

//     static string FormatYearRange(int start, int end)
//     {
//         if (start == end)
//             return start.ToString();
//         if (end == start + 1)
//             return $"{start}, {end}";
//         return $"{start}-{end}";
//     }

//     static async Task<T?> FetchWithRetry<T>(Func<Task<T>> op, string label)
//         where T : class
//     {
//         for (int i = 1; i <= MAX_RETRIES; i++)
//         {
//             try
//             {
//                 await Task.Delay(RETRY_DELAY_MS);
//                 return await op();
//             }
//             catch when (i < MAX_RETRIES)
//             {
//                 await Task.Delay(RETRY_DELAY_MS * i);
//             }
//         }
//         return null;
//     }
// }
