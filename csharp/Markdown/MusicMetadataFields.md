# Music Metadata Fields Reference

This document lists all available metadata fields from MusicBrainz and Discogs APIs.

---

## MusicBrainz Fields

### Artist

| Field            | Type        | Description                                       | Searchable |
| ---------------- | ----------- | ------------------------------------------------- | ---------- |
| `MbId`           | GUID        | MusicBrainz identifier                            | Yes        |
| `Name`           | string      | Artist name                                       | Yes        |
| `SortName`       | string      | Name for alphabetical sorting                     | No         |
| `Type`           | string      | Person, Group, Orchestra, Choir, Character, Other | Yes        |
| `Disambiguation` | string      | Brief note to distinguish                         | No         |
| `Country`        | string      | ISO 3166-1 alpha-2 code                           | Yes        |
| `Area`           | string      | Geographic region                                 | Yes        |
| `BeginDate`      | PartialDate | Formation/birth date                              | Yes        |
| `EndDate`        | PartialDate | Dissolution/death date                            | Yes        |
| `Aliases`        | string[]    | Alternative names                                 | Partial    |
| `Tags`           | string[]    | User-submitted tags                               | Yes        |
| `Genres`         | string[]    | Official genres                                   | Yes        |
| `IPIs`           | string[]    | Interested Party Information codes                | No         |
| `ISNIs`          | string[]    | International Standard Name Identifiers           | No         |

### Release

| Field          | Type           | Description                                  | Searchable |
| -------------- | -------------- | -------------------------------------------- | ---------- |
| `MbId`         | GUID           | MusicBrainz identifier                       | Yes        |
| `Title`        | string         | Release title                                | Yes        |
| `Status`       | string         | Official, Promotion, Bootleg, Pseudo-Release | Yes        |
| `Date`         | PartialDate    | Release date                                 | Yes        |
| `Country`      | string         | Release country                              | Yes        |
| `Barcode`      | string         | UPC/EAN barcode                              | Yes        |
| `Packaging`    | string         | CD, Vinyl, Digital, Box, etc.                | No         |
| `Quality`      | string         | Data quality indicator                       | No         |
| `ArtistCredit` | ArtistCredit[] | Artists and join phrases                     | Partial    |
| `LabelInfo`    | LabelInfo[]    | Labels and catalog numbers                   | Yes        |
| `Media`        | Medium[]       | Format, track lists, discs                   | Partial    |
| `Tags`         | string[]       | User-submitted tags                          | Yes        |
| `Genres`       | string[]       | Official genres                              | Yes        |

### Release Group

| Field              | Type           | Description                                          | Searchable |
| ------------------ | -------------- | ---------------------------------------------------- | ---------- |
| `MbId`             | GUID           | MusicBrainz identifier                               | Yes        |
| `Title`            | string         | Release group title                                  | Yes        |
| `PrimaryType`      | string         | Album, Single, EP, Broadcast, Other                  | Yes        |
| `SecondaryTypes`   | string[]       | Compilation, Soundtrack, Spokenword, Interview, etc. | Yes        |
| `FirstReleaseDate` | PartialDate    | Earliest release date                                | Yes        |
| `ArtistCredit`     | ArtistCredit[] | Artists and join phrases                             | Partial    |
| `Tags`             | string[]       | User-submitted tags                                  | Yes        |
| `Genres`           | string[]       | Official genres                                      | Yes        |

### Recording

| Field              | Type           | Description                            | Searchable |
| ------------------ | -------------- | -------------------------------------- | ---------- |
| `MbId`             | GUID           | MusicBrainz identifier                 | Yes        |
| `Title`            | string         | Recording title                        | Yes        |
| `Length`           | int (ms)       | Duration in milliseconds               | Yes        |
| `FirstReleaseDate` | PartialDate    | First release date                     | Yes        |
| `ArtistCredit`     | ArtistCredit[] | Artists and join phrases               | Partial    |
| `ISRCs`            | string[]       | International Standard Recording Codes | Yes        |
| `Tags`             | string[]       | User-submitted tags                    | Yes        |
| `Genres`           | string[]       | Official genres                        | Yes        |

### Work

| Field       | Type       | Description                               | Searchable |
| ----------- | ---------- | ----------------------------------------- | ---------- |
| `MbId`      | GUID       | MusicBrainz identifier                    | Yes        |
| `Title`     | string     | Work title                                | Yes        |
| `Type`      | string     | Song, Symphony, Opera, etc.               | Yes        |
| `Language`  | string     | Lyrics language                           | Yes        |
| `ISWCs`     | string[]   | International Standard Musical Work Codes | Yes        |
| `Relations` | Relation[] | Composer, lyricist, arranger, etc.        | Partial    |

---

## Discogs Fields

### Artist

| Field            | Type        | Description                   | Searchable |
| ---------------- | ----------- | ----------------------------- | ---------- |
| `Id`             | int         | Discogs identifier            | Yes        |
| `Name`           | string      | Artist name                   | Yes        |
| `RealName`       | string      | Legal/birth name              | No         |
| `Profile`        | string      | Biography text                | No         |
| `NameVariations` | string[]    | Alternative spellings         | No         |
| `Aliases`        | ArtistRef[] | Linked alias artists          | No         |
| `Members`        | ArtistRef[] | Group members                 | No         |
| `Groups`         | ArtistRef[] | Groups this artist belongs to | No         |
| `Urls`           | string[]    | External links                | No         |

### Release

| Field          | Type        | Description                         | Searchable |
| -------------- | ----------- | ----------------------------------- | ---------- |
| `Id`           | int         | Discogs release identifier          | Yes        |
| `MasterId`     | int         | Master release identifier           | Yes        |
| `Title`        | string      | Release title                       | Yes        |
| `Year`         | int         | Release year                        | Yes        |
| `Country`      | string      | Release country                     | Yes        |
| `Genres`       | string[]    | Genres (e.g., Rock, Electronic)     | Yes        |
| `Styles`       | string[]    | Sub-genres (e.g., Prog Rock, House) | Yes        |
| `Barcodes`     | string[]    | UPC/EAN barcodes                    | Yes        |
| `CatNos`       | string[]    | Catalog numbers                     | Yes        |
| `Labels`       | LabelRef[]  | Record labels                       | Yes        |
| `Artists`      | ArtistRef[] | Release artists                     | Yes        |
| `ExtraArtists` | ArtistRef[] | Credits (producers, etc.)           | No         |
| `Formats`      | Format[]    | Media formats and descriptions      | Partial    |
| `Tracklist`    | Track[]     | Track listing                       | Partial    |
| `Notes`        | string      | Release notes                       | No         |

### Master Release

| Field         | Type        | Description               | Searchable |
| ------------- | ----------- | ------------------------- | ---------- |
| `Id`          | int         | Master release identifier | Yes        |
| `Title`       | string      | Master title              | Yes        |
| `Year`        | int         | Original year             | Yes        |
| `MainRelease` | int         | Primary release ID        | No         |
| `Artists`     | ArtistRef[] | Artists                   | Yes        |
| `Genres`      | string[]    | Genres                    | Yes        |
| `Styles`      | string[]    | Styles                    | Yes        |
| `Tracklist`   | Track[]     | Canonical track listing   | No         |
| `NumForSale`  | int         | Items on marketplace      | No         |
| `LowestPrice` | decimal     | Lowest marketplace price  | No         |

### Label

| Field         | Type       | Description        | Searchable |
| ------------- | ---------- | ------------------ | ---------- |
| `Id`          | int        | Discogs identifier | Yes        |
| `Name`        | string     | Label name         | Yes        |
| `Profile`     | string     | Label description  | No         |
| `ContactInfo` | string     | Contact details    | No         |
| `ParentLabel` | LabelRef   | Parent company     | No         |
| `Sublabels`   | LabelRef[] | Child labels       | No         |
| `Urls`        | string[]   | External links     | No         |

---

## Search Query Fields

### MusicBrainz Search Fields

**Included in search queries by default:**
- `artist`, `release`, `recording`, `releasegroup`, `label`, `work`
- `alias` (artist aliases)
- `tag`, `genre`
- `country`, `date`, `barcode`, `catno`
- `type`, `status`, `format`

**Available but not included by default:**
- `comment` (disambiguation)
- `arid`, `reid`, `rgid` (entity IDs)
- `isrc`, `iswc`

### Discogs Search Fields

**Available search parameters:**
- `query` (free text)
- `type` (release, master, artist, label)
- `title`
- `artist`
- `release_title`
- `genre`, `style`
- `country`
- `year`
- `format`
- `catno` (catalog number)
- `barcode`
- `label`

**Cannot be searched directly:**
- Release notes, credits
- Secondary artist roles
- Track-level data

---

## Default Display Fields (by mode)

### Pop Mode (Default)

| Source      | Columns                                  |
| ----------- | ---------------------------------------- |
| Discogs     | Artist, Title, Year, Label, Format, ID   |
| MusicBrainz | Artist, Title, Date, Label, Format, MBID |

### Classical Mode

| Source      | Columns                                       |
| ----------- | --------------------------------------------- |
| Discogs     | Composer, Work, Performers, Year, Label, ID   |
| MusicBrainz | Composer, Work, Performers, Date, Label, MBID |

---

## Field Standardization Assessment

### Fields with Direct Mapping

| Unified Name | MusicBrainz             | Discogs         |
| ------------ | ----------------------- | --------------- |
| `artist`     | ArtistCredit[0].Name    | Artists[0].Name |
| `title`      | Title                   | Title           |
| `year`       | Date.Year               | Year            |
| `country`    | Country                 | Country         |
| `label`      | LabelInfo[0].Label.Name | Labels[0].Name  |
| `barcode`    | Barcode                 | Barcodes[0]     |
| `genre`      | Genres[]                | Genres[]        |
| `id`         | MbId                    | Id              |

### Fields Requiring Transformation

| Unified Name     | MusicBrainz                | Discogs           | Notes                  |
| ---------------- | -------------------------- | ----------------- | ---------------------- |
| `format`         | Media[0].Format            | Formats[0].Name   | Different vocabulary   |
| `catalog_number` | LabelInfo[0].CatalogNumber | CatNos[0]         | -                      |
| `style`          | Tags[]                     | Styles[]          | MB uses free-form tags |
| `url`            | Generated from MbId        | Generated from Id | Domain-specific        |

### Fields Unique to Each Service

**MusicBrainz only:**
- `ISRC`, `ISWC`, `IPI`, `ISNI`
- `Disambiguation`, `SortName`
- `Work` (composition-level entity)
- `Quality` (data quality)

**Discogs only:**
- `RealName` (artist)
- `MasterId` (canonical release)
- `NumForSale`, `LowestPrice` (marketplace)
- `Styles` (sub-genre taxonomy)
