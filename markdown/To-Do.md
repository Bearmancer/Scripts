1. Disable all pyright errors because of type errors caused by third party libraries
2. How to set up this as a profile -- the py and C# style configs and globalusings etc -- is this a `runner`? `agent profile`?
3. Integrate autofill live progress bar to show live update when new fields get parsed
4. Why does Successfully installed not have any UI to show what is happening?
```pwsh
CFFI-2.0.0 pycparser-2.23 sounddevice-0.5.3 whisper-ctranslate2-0.5.6
PS C:\Users\Lance> Invoke-Whisper '.\Elton John - Full Parkinson Interview HD - November 12th 2000.webm'
[17:09:26] Transcribing: Elton John - Full Parkinson Interview HD - November 12th 2000.webm
Model: large-v3 | Language: (auto-detect)
```

5. What does fill do by default when launching a search?
6. Refactor structure of MusicCommand - why region only some places? why is public sealed class separate from musicfill?
7. Use better naming for regions
8. Migrate all usings to global -- like from MusicCommands
9. Fix -input being even accepted; only --input or -i
10. Check all invocations to only accept single letter for - and words for `--` -- also does dotnet even support something like `-input`?
11. You need to do search with fill for both Discogs AND MusicBrainz -- not just Discogs and then sort by confidence match
12. This is atrocious -
```cs
[INFO] 17:11:29: Loaded 43 recordings from missing fields.tsv

Searching for missing fields ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ 100%

Symphony No. 7 - Bruckner, Anton
Label:
50% Victor, Marmorsaal, Stift St. Florian (Discogs)
40% Victor, Victor Musical Industries, Inc., Victor Musical Industries, Inc., Marmorsaal, Stift St. Florian (Discogs)
Catalog #:
50% KVX 5501-2 (Discogs)
40% VDC-1214 (Discogs)

Les Préludes - Liszt, Franz
Label:
40% His Master's Voice, The Gramophone Co. Ltd., E.M.I. International Limited, Mardons (Discogs)
Catalog #:
40% ALP 1220 (Discogs)

Symphony No. 9 - Schubert, Franz
Label:
70% His Master's Voice Digital, Abbey Road Studios, His Master's Voice, EMI Records Ltd., EMI Electrola (Discogs)
Catalog #:
70% ASD 1436621 (Discogs)

Symphony No. 1 - Shostakovich, Dmitri
Label:
50% Angel Records, Angel Records, Electric & Musical Industries (U.S.) Ltd., Mercure Ed. paris, Atelier Cassandre (Discogs)
50% Columbia, Columbia Graphophone Company Ltd., Columbia Graphophone Company Ltd., E.M.I. International Limited (Discogs)
40% Music For Pleasure, E.M.I. Records (Discogs)
Catalog #:
50% 35361 (Discogs)
50% 33CX 1440 (Discogs)
40% MFP 2080 (Discogs)

Symphony No. 1 - Shostakovich, Dmitri
Label:
60% EMI Classics, EMI Records Ltd., EMI Records Ltd. (Discogs)
Catalog #:
60% 7243 5 55361 2 9 (Discogs)

Symphony No. 5 - Sibelius, Jean
Label:
60% Columbia Masterworks (Discogs)
40% RCA Red Seal, RCA Records, RCA Records, RCA Records Pressing Plant, Indianapolis (Discogs)
Catalog #:
60% ML 5045 (Discogs)
40% ARL1-2906 (Discogs)

Kaiserwalz - Strauss II, Johann
Label:
50% Angel Records, Angel Master Series, Capitol Industries-EMI, Inc., EMI Germany (Discogs)
Catalog #:
50% AM-34733 (Discogs)

Marche Slave - Tchaikovsky, Pyotr Ilyich
Label:
70% Columbia Masterworks, Columbia Records, Columbia Records Pressing Plant, Santa Maria (Discogs)
50% Columbia Masterworks, Customatrix, Columbia Records Pressing Plant, Santa Maria (Discogs)
40% CBS, CBS, CBS Great Performances (Discogs)
Catalog #:
70% MS 6477 (Discogs)
50% MS 6827 (Discogs)
40% MY36723 (Discogs)

Romeo and Juliet - Tchaikovsky, Pyotr Ilyich
Label:
50% RCA Victor (Discogs)
40% RCA (Discogs)
40% RCA Victor Red Seal (Discogs)
Catalog #:
50% LM-6028 (Discogs)
```

13. Fix to show what was inputted properly -- field of each data
14. Fix search to supply data properly as per fields
15. Check `whisp` as an alias and migrate to making `whisp` be an alias for transcribing with distil-large-v3.5 and English
16. Modify to show progress properly of invoke whisper:
```pwsh
[17:20:47] Transcribing: Elton John - Full Parkinson Interview HD - November 12th 2000.webm
             Model: distil-large-v3.5 | Language: en
Detected language 'English' with probability 1.000000
 12%|███████▋                                                        | 478.3/3986.2826875 [02:48<22:43,  2.57seconds/s]
```
17. Add proper info for what numbers after the progress bar mean
18. Why use <?
19. Delineate of ETA vs elapsed
20. replace seconds/s
21. What is the purpose of `omnisharp`?
22. Find way to order and assess the reasons for pwsh load time to figure out what to enable/disable
23. Is showing if whisper-ctranslate2 missing inside save-youtubevideo missing?
24. Similarly, does invoke-whisper suppress if a model is being downloaded?
25. Restore both
26. Find method to reverse engineer extension of video files that have missing extensions in their file name using either ffprobe or mediainfo cli
27. Segregate into folders based on extension and create new pwsh one liner for - D:\Google Drive\Games\Others\Miscellaneous
28. Finish implementation of missing fields to work for finding all info always
29. Show progress in filling of missing fields to indicate value parsed + which service is used instead of merely %
30. Create region markings for hierarchy files
31. What I am asking of you is to use basedpyright to disable errors caused by things not in our control such as untyped libraries
32. Tell me how to use these default values out of the box with GitHub Copilot when creating new projects
33. Confused --- yes progress bar reuse the razzle-dazzle version
34. Fix number of regions + assess reordering of methods + restructure music commands
35. How to make rider realize all MD points are numbered correctly and use of code blocks does not indicate a new numbered list
36. How to fix copilot always being on older version without forcing to download it every time
37. Fix all warnings including collection initialization syntax
38. Fix found to show all fields that you filled in alongside elapsed at the end but more importantly show name of recording before the items you found
39. New format for finding fields:
```text
[<found fields>: -- new line list per field with found at top]
Found:
[<newly found fields>]
```
40. Ensure writing in real time of all found fields instead of waiting until the end and when resuming do not start search all over again --- although perhaps implicitly it wouldn't?
41. Create logic to always prefer details for very first pressing for label fields
42. Find way to match label with catalog -- catalog number alongside label:

Label Suggestions:
40% Deutsche Grammophon, Galleria, Polydor International GmbH, Neef (Discogs)
40% Tring International PLC, CTS Studios, The Royal Philharmonic Collection (Discogs)
40% Philips, De Klassieken, Phonodisc B.V., Phonodisc B.V. (Discogs)
Catalog # Suggestions:
40% 415 835-1 (Discogs)
40% TRP012 (Discogs)
40% 6598 572 (Discogs)

Lacks clarity if catalog numbers correspond to the label ones

43. Create shortening of things like Deutsche Grammophon to DG automatically when filling in
44. Create new auto-filled TSV file vertically aligned with highest confidence values without prompt (retain printing to screen)
45. Create separate plan using native AI tool -- what is it called? Your implementation plan is created not inside dir but inside program file and is formatted differently and supports things like review
46. Create separate plan for integrating and moving Python scrobble inside toolkit using typer after overhaul as Typer-based; ignore C# and pwsh duplication of concern --- this py last.fm is only a token not to be invoked elsewhere
47. Explain why .toml file exists
48. Find way to find unapproved pwsh verbs natively and if not scan manually
49. Create plan for migrating all verbs
50. Use pydoc to list all python signatures in a new file
51. Read file to create new plan of each function in a 1:1 manner without summarizing -- focus on using .NET design philosophy and if functional can be used fully -- foregoing need of class instantiation altogether
52. Migrate to using named args only when there are too many arguments in a single method call + stylistic assess
53. Find best way to specify scheme of named args to AI
54. Rewrite to never use spectre calls directly and instead enforce using Console.cs such as:
```cs
var panel = new Panel(
    new Markup(
        $"[bold green]✓ Tab completion installed successfully![/]\n\n"
            + $"[dim]Profile:[/]\n[link=file:///{psProfilePath}]{psProfilePath}[/]\n\n"
            + $"[yellow]Action Required:[/]\nRestart PowerShell or run: [bold]. $PROFILE[/]"
    )
)
{
    Border = BoxBorder.Rounded,
    Padding = new Spectre.Console.Padding(1, 1),
    Header = new PanelHeader("[blue]System Configuration[/]"),
};
```
55. The thing to remember is to prevent all markup errors by using Console.cs
57. How to auto-add numbering when pasting lines of text does not auto create / auto-continue numbering 
58. Remove all comments and XML docs
59. Migration plan py -> C# not pwsh -> C# -- use terminal to create a list of all method signatures
60. Check regions in all files individually ensuring not to create regions if a file is too small or if a single region is only a few lines -- assess for cases where there is more than 1 region because a file is large as opposed to just adding regions always by default:

```pwsh
1. C:\Users\Lance\Dev\csharp\obj\Debug\net10.0\.NETCoreApp,Version=v10.0.AssemblyAttributes.cs
2. C:\Users\Lance\Dev\csharp\obj\Debug\net10.0\CSharpScripts.AssemblyInfo.cs
3. C:\Users\Lance\Dev\csharp\obj\Debug\net10.0\CSharpScripts.GlobalUsings.g.cs
4. C:\Users\Lance\Dev\csharp\src\CLI\CleanCommands.cs
5. C:\Users\Lance\Dev\csharp\src\CLI\CompletionCommands.cs
6. C:\Users\Lance\Dev\csharp\src\CLI\MailCommands.cs
7. C:\Users\Lance\Dev\csharp\src\CLI\MusicFillCommand.cs
8. C:\Users\Lance\Dev\csharp\src\CLI\MusicSearchCommand.cs
9. C:\Users\Lance\Dev\csharp\src\CLI\SyncCommands.cs
10. C:\Users\Lance\Dev\csharp\src\CLI\ValidationAttributes.cs
11. C:\Users\Lance\Dev\csharp\src\Infrastructure\Config.cs
12. C:\Users\Lance\Dev\csharp\src\Infrastructure\Console.cs
13. C:\Users\Lance\Dev\csharp\src\Infrastructure\Logger.cs
14. C:\Users\Lance\Dev\csharp\src\Infrastructure\Paths.cs
15. C:\Users\Lance\Dev\csharp\src\Infrastructure\ReleaseProgressCache.cs
16. C:\Users\Lance\Dev\csharp\src\Infrastructure\Resilience.cs
17. C:\Users\Lance\Dev\csharp\src\Infrastructure\StateManager.cs
18. C:\Users\Lance\Dev\csharp\src\Infrastructure\SyncProgressRenderer.cs
19. C:\Users\Lance\Dev\csharp\src\Infrastructure\SyncProgressTracker.cs
20. C:\Users\Lance\Dev\csharp\src\Models\Discogs.cs
21. C:\Users\Lance\Dev\csharp\src\Models\Mail.cs
22. C:\Users\Lance\Dev\csharp\src\Models\Music.cs
23. C:\Users\Lance\Dev\csharp\src\Models\MusicBrainz.cs
24. C:\Users\Lance\Dev\csharp\src\Models\YouTube.cs
25. C:\Users\Lance\Dev\csharp\src\Orchestrators\ScrobbleSyncOrchestrator.cs
26. C:\Users\Lance\Dev\csharp\src\Orchestrators\YouTubePlaylistOrchestrator.cs
27. C:\Users\Lance\Dev\csharp\src\Services\Mail\IDisposableMailService.cs
28. C:\Users\Lance\Dev\csharp\src\Services\Mail\MailTmService.cs
29. C:\Users\Lance\Dev\csharp\src\Services\Music\DiscogsService.cs
30. C:\Users\Lance\Dev\csharp\src\Services\Music\IMusicService.cs
31. C:\Users\Lance\Dev\csharp\src\Services\Music\LanguageDetector.cs
32. C:\Users\Lance\Dev\csharp\src\Services\Music\MusicBrainzService.cs
33. C:\Users\Lance\Dev\csharp\src\Services\Music\MusicExporter.cs
34. C:\Users\Lance\Dev\csharp\src\Services\Sync\Google\GoogleSheetsService.cs
35. C:\Users\Lance\Dev\csharp\src\Services\Sync\LastFm\LastFmService.cs
36. C:\Users\Lance\Dev\csharp\src\Services\Sync\YouTube\YouTubeChangeDetector.cs
37. C:\Users\Lance\Dev\csharp\src\Services\Sync\YouTube\YouTubeService.cs
38. C:\Users\Lance\Dev\csharp\src\GlobalUsings.cs
39. C:\Users\Lance\Dev\csharp\src\Program.cs
40. C:\Users\Lance\Dev\Templates\CSharp\src\GlobalUsings.cs
```

Implement py migration for last.fm scrobble and integrate and delete the separate folder afterwards
41. Fix auto-closing of terminal for all sync operations if they succeed -- at the moment they do not ---
42. Check value returned by cs vs what is expected in pwsh and actions taken 
43. Check task creator too to see what is being passed 
44. Suggest other improvements to namedargumentconvention ai instruction set list
45. Explain decision to manually write each field as opposed to simply passing record as seen in:
```cs
var best = suggestions.GetBest();
                    csv.WriteField(record.Composer);
                    csv.WriteField(record.Work);
                    csv.WriteField(record.Orchestra);
                    csv.WriteField(record.Conductor);
                    csv.WriteField(record.Performers);
                    csv.WriteField(record.Label);
                    csv.WriteField(best?.Label ?? "");
                    csv.WriteField(best?.Confidence.ToString() ?? "");
                    csv.WriteField(record.Year);
                    csv.WriteField(best?.Year ?? "");
                    csv.WriteField(best?.Confidence.ToString() ?? "");
                    csv.WriteField(record.CatalogNumber);
                    csv.WriteField(best?.CatalogNumber ?? "");
                    csv.WriteField(best?.Confidence.ToString() ?? "");
                    csv.WriteField(record.Rating);
                    csv.WriteField(record.Comment);
                    csv.NextRecord();
```
47. Modify prproject.toml to not show warnings for untyped libraries
48. Fix all problems by invoking terminal cmd to show all basedpyright after changing it
49. Decide best place to keep filloutput row both within directory structure and within a file
50. Read @powershell_enhancements.md
51. Isolate modules within it 
52. Find way to integrate said modules into my pwsh module toolkit
53. Integrate lazy loading of my module in pwsh profile
54. Determine new paths for all tools like carapace argc and fzf first
55. Search for files currently of all since they were installed
56. Move to new path where it is centralized
57. Fix $profile to use paths that align with current Dev dir being the place where module is
59. All paths referred in .cs and .ps1 need to be tested and updated
60. Ensure youtube downloading invokes `whisp` (after update) automatically 
61. First recreate lazy-loading of personal module in pwsh to use pscompletion+psreadline+psfzf+argc for dotnet and winget+carapace elsewhere --- run benchmarks before and after each module to see performance hit
62. Look at docs to ensure lazy loading but retaining being able to dynamically fill values in both carapace and argc
63. List all cmds supported by argc by fetching the manifest and put that inside a new cmd that allows loading of argc -- and that way the entire list is visible inside pscompletion
64. Use pscompletion menu as setting
65. Retain psfzf's tab but mapped to ctrl+space
66. Suppress python outdate library warning inside whisper
67. Best way to add whisper-ctranslate2 CLI autocomplete --- psc, psreadline, carapace, fzf, argc -- do any of them have it out of the box?
68. Force pwsh to UTF-8 always
