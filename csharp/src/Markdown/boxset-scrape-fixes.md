# Box Set Scrape - Bug Fixes and Enhancements

## ✅ All Completed

### Core Fixes
1. **Wrong Composer Attribution** - Now fetches composer from Work→Artist(composer) relationship
2. **Wrong Year (2021 → 1944)** - Now uses Recording.RecordingDate from recording relationships (not box set release year)
3. **Work Name Extraction** - Extracts parent work from movement titles (e.g., "Symphonie espagnole in D minor, op. 21: I. Allegro" → "Symphonie espagnole in D minor, op. 21")
4. **Conductor/Orchestra Missing** - Now extracted from Recording→Artist relationships (type "conductor", "orchestra")
5. **Recording Venue** - Now extracted from Recording→Place relationships (type "recorded at")
6. **Recording Date** - Extracted from relationship Begin dates

### Table Display
- Split `#` into separate `Disc` and `Track` columns
- Added `Work`, `Conductor`, `Orchestra` columns
- Renamed `RecYear` to `Year` (shows recording date, not box set year)
- Full track titles (no truncation)

### API Enhancements
- Added `Include.ArtistRelationships` and `Include.PlaceRelationships` to Recording lookup
- MusicBrainzRecording model now includes Conductor, Orchestra, RecordingVenue, RecordingDate
![alt text](image.png)
### Caching Optimizations
- **Work Context Cache** - Consecutive tracks of same Work reuse cached recording metadata
- **Work Composer Cache** - Same Work ID across different recordings only fetches composer once

### Testing
- 15 unit tests for ExtractParentWorkName and FindRomanNumeralIndex
- 15 integration tests validating against Ormandy Columbia Legacy box set data
- Tests verify: Work extraction, composer, conductor, orchestra, year (1944 not 2021), venue

## Build & Tests
- **Build**: ✅ Succeeded
- **Tests**: ✅ 59 passed, 0 failed

## Expected Output for Track 1.01 (Symphonie espagnole)
| Field     | Expected Value                         |
| --------- | -------------------------------------- |
| Work      | Symphonie espagnole in D minor, op. 21 |
| Composer  | Édouard Lalo                           |
| Conductor | Eugene Ormandy                         |
| Orchestra | The Philadelphia Orchestra             |
| Year      | 1944                                   |
| Venue     | Academy of Music in Philadelphia       |
