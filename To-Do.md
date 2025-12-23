1. Disable all pyright errors because of type errors caused by third party libraries
2. How to set up this as a profile -- the py and C# style configs and globalusings etc  -- is this a `runner`? `agent profile`?
3. Integrate autofill live progress bar to show live update when new fields get parsed
4. Why does Successfully installed CFFI-2.0.0 pycparser-2.23 sounddevice-0.5.3 whisper-ctranslate2-0.5.6
PS C:\Users\Lance> Invoke-Whisper '.\Elton John - Full Parkinson Interview HD - November 12th 2000.webm'
[17:09:26] Transcribing: Elton John - Full Parkinson Interview HD - November 12th 2000.webm
             Model: large-v3 | Language: (auto-detect)



not have any UI to show what is happening?

5. What does fill do by default when launching a search?
6. Refactor structure of MusicCommand - why region only some places? why is public sealed class separate from musicfill ?
7. Use better naming for regions
8. Migrate all usings to global -- like from MusicCommands
9. Fix -input being even accepted; only --input or -i
10. Check all invocations to only accept single letter for - and words for `--` -- also does dotnet even support something like `-input`?
11. You need to do search with fill for both Discogs AND MusicBrainz -- not just Discogs and then sort by confidence match
12. This is atrocious - 
[INFO] 17:11:29: Loaded 43 recordings from missing fields.tsv

Searching for missing fields ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ 100%


Symphony No. 7 - Bruckner, Anton
  Label:
    50% Victor, Marmorsaal, Stift St. Florian (Discogs)
    40% Victor, Victor Musical Industries, Inc., Victor Musical Industries, Inc., Marmorsaal, Stift St. Florian
(Discogs)
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
    50% Angel Records, Angel Records, Electric & Musical Industries (U.S.) Ltd., Mercure Ed. paris, Atelier Cassandre
(Discogs)
    50% Columbia, Columbia Graphophone Company Ltd., Columbia Graphophone Company Ltd., E.M.I. International Limited
(Discogs)
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

13. Fix to show what was inputted properly -- field of each data 
14. Fix search to supply data properly as per fields
15. Check `whisp` as an alias and migrate to making `whisp` be an alias for transcribing with distil-large-v3.5 and English
16. Modify to show progress properly of invoke whisper:

[17:20:47] Transcribing: Elton John - Full Parkinson Interview HD - November 12th 2000.webm
             Model: distil-large-v3.5 | Language: en
Detected language 'English' with probability 1.000000
 12%|███████▋                                                        | 478.3/3986.2826875 [02:48<22:43,  2.57seconds/s]

17. Add proper info for what numbers after the progress bar mean
18. Why use <?
19. Delineate of ETA vs elapsed
20. replace seconds/s
21. What is the purpose of `omnisharp`?
22. Find way to order and assess the reasons for pwsh load time to figure out what to enable/disable
23. Is showing if whisper-ctranslate2 missing inside save-youtubevideo missing?
24. SImilarly, does invoke-whisper suppress if a model is being downloaded?
25. Restore both
26. Find method to reverse engineer extension of video files that have missing extensions in their file name using either ffprobe or mediainfo cli
27. Segregate into folders based on extension and create new pwsh one liner for - D:\Google Drive\Games\Others\Miscellaneous
28. Finish implementation of missing fields to work for finding all info always
29. Show progress in filling of missing fields to indicate value parsed + which service is used instead of merely %
30. Create region markings for hierarchy files
31. 