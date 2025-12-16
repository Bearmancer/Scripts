namespace CSharpScripts.Models;

/// <summary>
/// Comprehensive list of all music metadata fields available from MusicBrainz and Discogs APIs.
/// Covers: Artist, Release, Recording, Track, Label, ReleaseGroup (Album/EP/Single/Compilation), Master, and more.
/// </summary>
public static class MusicMetadataSchema
{
    /// <summary>
    /// Get all metadata fields organized by entity type and source.
    /// </summary>
    public static Dictionary<string, EntitySchema> GetAllSchemas() =>
        new()
        {
            ["Artist"] = GetArtistSchema(),
            ["Release"] = GetReleaseSchema(),
            ["Recording"] = GetRecordingSchema(),
            ["Track"] = GetTrackSchema(),
            ["Label"] = GetLabelSchema(),
            ["ReleaseGroup"] = GetReleaseGroupSchema(),
            ["Master"] = GetMasterSchema(),
            ["Format"] = GetFormatSchema(),
            ["Credit"] = GetCreditSchema(),
            ["Image"] = GetImageSchema(),
            ["Video"] = GetVideoSchema(),
            ["Identifier"] = GetIdentifierSchema(),
            ["Community"] = GetCommunitySchema(),
        };

    public static EntitySchema GetArtistSchema() =>
        new(
            EntityName: "Artist",
            Description: "Musician, band, composer, conductor, orchestra, or other music entity",
            Fields:
            [
                // MusicBrainz Fields
                new("Id", "GUID", "MusicBrainz", "Unique identifier (MBID)"),
                new("Name", "string", "MusicBrainz", "Primary name"),
                new(
                    "SortName",
                    "string",
                    "MusicBrainz",
                    "Name formatted for sorting (e.g., 'Beatles, The')"
                ),
                new(
                    "Type",
                    "string",
                    "MusicBrainz",
                    "Type: Person, Group, Orchestra, Choir, Character, Other"
                ),
                new(
                    "Gender",
                    "string",
                    "MusicBrainz",
                    "Gender: Male, Female, Non-binary, Other, Not applicable"
                ),
                new("Country", "string", "MusicBrainz", "ISO 3166-1 country code"),
                new(
                    "Area",
                    "string",
                    "MusicBrainz",
                    "Geographic area (more specific than country)"
                ),
                new(
                    "Disambiguation",
                    "string",
                    "MusicBrainz",
                    "Comment to distinguish from similar artists"
                ),
                new(
                    "BeginDate",
                    "PartialDate",
                    "MusicBrainz",
                    "Birth date (person) or formation date (group)"
                ),
                new(
                    "EndDate",
                    "PartialDate",
                    "MusicBrainz",
                    "Death date (person) or dissolution date (group)"
                ),
                new("Ended", "bool", "MusicBrainz", "True if artist is no longer active"),
                new(
                    "Aliases",
                    "List<string>",
                    "MusicBrainz",
                    "Alternative names, translations, misspellings"
                ),
                new("IPI", "string", "MusicBrainz", "Interested Parties Information code"),
                new(
                    "ISNI",
                    "List<string>",
                    "MusicBrainz",
                    "International Standard Name Identifiers"
                ),
                new("Tags", "List<string>", "MusicBrainz", "Community-applied tags"),
                new("Genres", "List<string>", "MusicBrainz", "Official genre classifications"),
                new("Annotation", "string", "MusicBrainz", "Free-form notes about the artist"),
                new("Rating", "double", "MusicBrainz", "Community rating (0-5)"),
                new("RatingVotes", "int", "MusicBrainz", "Number of rating votes"),
                // Discogs Fields
                new("Id", "int", "Discogs", "Unique numeric identifier"),
                new("Name", "string", "Discogs", "Primary artist name"),
                new("RealName", "string", "Discogs", "Real name (for stage names)"),
                new("Profile", "string", "Discogs", "Biography/description text"),
                new("NameVariations", "List<string>", "Discogs", "Spelling variations of the name"),
                new(
                    "Aliases",
                    "List<string>",
                    "Discogs",
                    "Other artist entries that are the same person"
                ),
                new("Images", "List<Image>", "Discogs", "Artist photos"),
                new("Urls", "List<string>", "Discogs", "External links (website, social media)"),
                new("ResourceUrl", "string", "Discogs", "API URL for this resource"),
                new(
                    "DataQuality",
                    "string",
                    "Discogs",
                    "Data quality rating: Needs Vote, Complete, Correct, etc."
                ),
                new("Members", "List<ArtistRef>", "Discogs", "Group members (for bands)"),
                new("Groups", "List<ArtistRef>", "Discogs", "Groups this artist belongs to"),
            ]
        );

    public static EntitySchema GetReleaseSchema() =>
        new(
            EntityName: "Release",
            Description: "A specific physical or digital issue of an album, EP, single, compilation, box set, or other release",
            Fields:
            [
                // MusicBrainz Fields
                new("Id", "GUID", "MusicBrainz", "Unique identifier (MBID)"),
                new("Title", "string", "MusicBrainz", "Release title"),
                new("Artist", "string", "MusicBrainz", "Primary artist name"),
                new(
                    "ArtistCredit",
                    "string",
                    "MusicBrainz",
                    "Full artist credit with join phrases"
                ),
                new(
                    "Date",
                    "PartialDate",
                    "MusicBrainz",
                    "Release date (may be partial: year only, year-month, or full)"
                ),
                new("Country", "string", "MusicBrainz", "ISO 3166-1 release country"),
                new(
                    "Status",
                    "string",
                    "MusicBrainz",
                    "Official, Promotion, Bootleg, Pseudo-Release, Withdrawn, Cancelled"
                ),
                new("Barcode", "string", "MusicBrainz", "UPC/EAN barcode"),
                new("Asin", "string", "MusicBrainz", "Amazon Standard Identification Number"),
                new("Quality", "string", "MusicBrainz", "Data quality: Low, Normal, High"),
                new(
                    "Packaging",
                    "string",
                    "MusicBrainz",
                    "Packaging type: Jewel Case, Digipak, Cardboard, etc."
                ),
                new(
                    "Disambiguation",
                    "string",
                    "MusicBrainz",
                    "Comment to distinguish similar releases"
                ),
                new("Language", "string", "MusicBrainz", "ISO 639-3 language code"),
                new(
                    "Script",
                    "string",
                    "MusicBrainz",
                    "ISO 15924 script code (e.g., Latn, Cyrl, Jpan)"
                ),
                new("ReleaseGroupId", "GUID", "MusicBrainz", "Parent release group MBID"),
                new("ReleaseGroupTitle", "string", "MusicBrainz", "Release group title"),
                new(
                    "ReleaseGroupType",
                    "string",
                    "MusicBrainz",
                    "Album, Single, EP, Broadcast, Other"
                ),
                new("Media", "List<Medium>", "MusicBrainz", "Discs/media in the release"),
                new(
                    "Credits",
                    "List<Credit>",
                    "MusicBrainz",
                    "Artist relationships: producer, engineer, composer, etc."
                ),
                new("Labels", "List<LabelInfo>", "MusicBrainz", "Labels with catalog numbers"),
                new("Tags", "List<string>", "MusicBrainz", "Community-applied tags"),
                new("Genres", "List<string>", "MusicBrainz", "Official genre classifications"),
                new("Annotation", "string", "MusicBrainz", "Free-form notes"),
                new("CoverArtArchive", "object", "MusicBrainz", "Cover art availability info"),
                new("DiscIds", "List<string>", "MusicBrainz", "CD TOC disc IDs"),
                // Discogs Fields
                new("Id", "int", "Discogs", "Unique numeric identifier (release ID)"),
                new("Title", "string", "Discogs", "Release title"),
                new("Year", "int", "Discogs", "Release year"),
                new("Country", "string", "Discogs", "Release country"),
                new("Released", "string", "Discogs", "Release date string"),
                new("ReleasedFormatted", "string", "Discogs", "Human-readable release date"),
                new("MasterId", "int", "Discogs", "Parent master release ID"),
                new("MasterUrl", "string", "Discogs", "API URL to master release"),
                new("Status", "string", "Discogs", "Accepted, Draft, Deleted, Rejected"),
                new("DataQuality", "string", "Discogs", "Needs Vote, Complete, Correct, etc."),
                new("Notes", "string", "Discogs", "Release notes and information"),
                new("Uri", "string", "Discogs", "Discogs website URL"),
                new("ResourceUrl", "string", "Discogs", "API URL for this resource"),
                new("Artists", "List<ArtistRef>", "Discogs", "Primary artists"),
                new(
                    "ExtraArtists",
                    "List<ArtistRef>",
                    "Discogs",
                    "Credits: producer, engineer, etc."
                ),
                new("Labels", "List<Label>", "Discogs", "Labels with catalog numbers"),
                new(
                    "Companies",
                    "List<Company>",
                    "Discogs",
                    "Related companies: pressing plant, studio, etc."
                ),
                new("Genres", "List<string>", "Discogs", "Genre classifications"),
                new("Styles", "List<string>", "Discogs", "Style/subgenre classifications"),
                new("Tracks", "List<Track>", "Discogs", "Tracklist"),
                new("Formats", "List<Format>", "Discogs", "Physical formats: Vinyl, CD, etc."),
                new(
                    "Identifiers",
                    "List<Identifier>",
                    "Discogs",
                    "Barcodes, matrix numbers, ISRC, etc."
                ),
                new("Images", "List<Image>", "Discogs", "Release artwork"),
                new("Videos", "List<Video>", "Discogs", "Related videos"),
                new("Community", "Community", "Discogs", "Have/Want counts, ratings, contributors"),
                new(
                    "EstimatedWeight",
                    "int",
                    "Discogs",
                    "Estimated weight in grams (for shipping)"
                ),
            ]
        );

    public static EntitySchema GetRecordingSchema() =>
        new(
            EntityName: "Recording",
            Description: "A unique audio recording (performance of a work). MusicBrainz-specific; Discogs has tracks on releases only.",
            Fields:
            [
                // MusicBrainz Fields
                new("Id", "GUID", "MusicBrainz", "Unique identifier (MBID)"),
                new("Title", "string", "MusicBrainz", "Recording title"),
                new("Artist", "string", "MusicBrainz", "Primary artist name"),
                new(
                    "ArtistCredit",
                    "string",
                    "MusicBrainz",
                    "Full artist credit with join phrases"
                ),
                new("Length", "TimeSpan", "MusicBrainz", "Duration in milliseconds"),
                new(
                    "FirstReleaseDate",
                    "PartialDate",
                    "MusicBrainz",
                    "Date of first release containing this recording"
                ),
                new("IsVideo", "bool", "MusicBrainz", "True if this is a video recording"),
                new(
                    "Disambiguation",
                    "string",
                    "MusicBrainz",
                    "Comment to distinguish similar recordings"
                ),
                new(
                    "Isrcs",
                    "List<string>",
                    "MusicBrainz",
                    "International Standard Recording Codes"
                ),
                new("Tags", "List<string>", "MusicBrainz", "Community-applied tags"),
                new("Genres", "List<string>", "MusicBrainz", "Official genre classifications"),
                new("Rating", "double", "MusicBrainz", "Community rating (0-5)"),
                new("RatingVotes", "int", "MusicBrainz", "Number of rating votes"),
                new("Annotation", "string", "MusicBrainz", "Free-form notes"),
                new("Works", "List<Work>", "MusicBrainz", "Linked works (compositions)"),
                new(
                    "Releases",
                    "List<Release>",
                    "MusicBrainz",
                    "Releases containing this recording"
                ),
            ]
        );

    public static EntitySchema GetTrackSchema() =>
        new(
            EntityName: "Track",
            Description: "A track on a specific medium/disc of a release",
            Fields:
            [
                // MusicBrainz Fields
                new("Id", "GUID", "MusicBrainz", "Unique identifier (MBID)"),
                new(
                    "Title",
                    "string",
                    "MusicBrainz",
                    "Track title (may differ from recording title)"
                ),
                new("Position", "int", "MusicBrainz", "Track number on the medium"),
                new(
                    "Number",
                    "string",
                    "MusicBrainz",
                    "Track number as printed (e.g., 'A1', '1-2')"
                ),
                new(
                    "Length",
                    "TimeSpan",
                    "MusicBrainz",
                    "Duration (may differ from recording length)"
                ),
                new("RecordingId", "GUID", "MusicBrainz", "Associated recording MBID"),
                new(
                    "ArtistCredit",
                    "string",
                    "MusicBrainz",
                    "Track-specific artist credit if different from recording"
                ),
                // Discogs Fields
                new("Position", "string", "Discogs", "Track position (e.g., 'A1', '1-2', '3')"),
                new("Title", "string", "Discogs", "Track title"),
                new("Duration", "string", "Discogs", "Duration as string (e.g., '3:45')"),
                new("Type", "string", "Discogs", "Type: track, heading, index"),
                new(
                    "Artists",
                    "List<ArtistRef>",
                    "Discogs",
                    "Track-specific artists (if different from release)"
                ),
                new("ExtraArtists", "List<ArtistRef>", "Discogs", "Track-specific credits"),
            ]
        );

    public static EntitySchema GetLabelSchema() =>
        new(
            EntityName: "Label",
            Description: "Record label, imprint, or distribution company",
            Fields:
            [
                // MusicBrainz Fields
                new("Id", "GUID", "MusicBrainz", "Unique identifier (MBID)"),
                new("Name", "string", "MusicBrainz", "Label name"),
                new("SortName", "string", "MusicBrainz", "Name for sorting purposes"),
                new(
                    "Type",
                    "string",
                    "MusicBrainz",
                    "Original Production, Bootleg Production, Reissue Production, Distribution, Holding, Rights Society"
                ),
                new("LabelCode", "int", "MusicBrainz", "LC code (e.g., LC 0149)"),
                new("Area", "string", "MusicBrainz", "Geographic area"),
                new("BeginDate", "PartialDate", "MusicBrainz", "Founding date"),
                new("EndDate", "PartialDate", "MusicBrainz", "Closure date"),
                new("Ended", "bool", "MusicBrainz", "True if label is defunct"),
                new(
                    "Disambiguation",
                    "string",
                    "MusicBrainz",
                    "Comment to distinguish similar labels"
                ),
                new("Aliases", "List<string>", "MusicBrainz", "Alternative names"),
                new("IPI", "string", "MusicBrainz", "Interested Parties Information code"),
                new(
                    "ISNI",
                    "List<string>",
                    "MusicBrainz",
                    "International Standard Name Identifiers"
                ),
                new("Tags", "List<string>", "MusicBrainz", "Community-applied tags"),
                new("Annotation", "string", "MusicBrainz", "Free-form notes"),
                new("Rating", "double", "MusicBrainz", "Community rating (0-5)"),
                new("RatingVotes", "int", "MusicBrainz", "Number of rating votes"),
                new(
                    "CatalogNumber",
                    "string",
                    "MusicBrainz",
                    "Catalog number (when linked to release)"
                ),
                // Discogs Fields
                new("Id", "int", "Discogs", "Unique numeric identifier"),
                new("Name", "string", "Discogs", "Label name"),
                new("Profile", "string", "Discogs", "Description/history"),
                new("ContactInfo", "string", "Discogs", "Contact information"),
                new("ParentLabel", "LabelRef", "Discogs", "Parent label if this is a sublabel"),
                new("Sublabels", "List<LabelRef>", "Discogs", "Child labels"),
                new("Urls", "List<string>", "Discogs", "External links"),
                new("Images", "List<Image>", "Discogs", "Label logos"),
                new("DataQuality", "string", "Discogs", "Data quality rating"),
                new(
                    "CatalogNumber",
                    "string",
                    "Discogs",
                    "Catalog number (when linked to release)"
                ),
                new("EntityType", "string", "Discogs", "Label type identifier"),
                new("EntityTypeName", "string", "Discogs", "Label type name"),
                new("ResourceUrl", "string", "Discogs", "API URL for this resource"),
            ]
        );

    public static EntitySchema GetReleaseGroupSchema() =>
        new(
            EntityName: "ReleaseGroup",
            Description: "Groups all versions of an album, EP, single, or compilation. MusicBrainz concept; Discogs uses Master for similar purpose.",
            Fields:
            [
                // MusicBrainz Fields (Release Groups)
                new("Id", "GUID", "MusicBrainz", "Unique identifier (MBID)"),
                new("Title", "string", "MusicBrainz", "Release group title"),
                new("Artist", "string", "MusicBrainz", "Primary artist name"),
                new(
                    "ArtistCredit",
                    "string",
                    "MusicBrainz",
                    "Full artist credit with join phrases"
                ),
                new("PrimaryType", "string", "MusicBrainz", "Album, Single, EP, Broadcast, Other"),
                new(
                    "SecondaryTypes",
                    "List<string>",
                    "MusicBrainz",
                    "Compilation, Soundtrack, Spokenword, Interview, Audiobook, Audio drama, Live, Remix, DJ-mix, Mixtape/Street"
                ),
                new("FirstReleaseDate", "PartialDate", "MusicBrainz", "Date of first release"),
                new("ReleaseCount", "int", "MusicBrainz", "Number of releases in this group"),
                new("Disambiguation", "string", "MusicBrainz", "Comment for disambiguation"),
                new("Tags", "List<string>", "MusicBrainz", "Community-applied tags"),
                new("Genres", "List<string>", "MusicBrainz", "Official genre classifications"),
                new("Rating", "double", "MusicBrainz", "Community rating (0-5)"),
                new("RatingVotes", "int", "MusicBrainz", "Number of rating votes"),
                new("Annotation", "string", "MusicBrainz", "Free-form notes"),
                new("Releases", "List<Release>", "MusicBrainz", "All releases in this group"),
                new("CoverArt", "object", "MusicBrainz", "Cover art availability"),
            ]
        );

    public static EntitySchema GetMasterSchema() =>
        new(
            EntityName: "Master",
            Description: "Discogs Master Release - groups all versions of a release. Similar to MusicBrainz Release Group.",
            Fields:
            [
                // Discogs Fields (Master)
                new("Id", "int", "Discogs", "Unique numeric identifier (master ID)"),
                new("Title", "string", "Discogs", "Master release title"),
                new("Year", "int", "Discogs", "Original release year"),
                new("MainReleaseId", "int", "Discogs", "ID of the main/canonical release"),
                new(
                    "MostRecentReleaseId",
                    "int",
                    "Discogs",
                    "ID of the most recently added version"
                ),
                new("MainReleaseUrl", "string", "Discogs", "API URL to main release"),
                new("MostRecentReleaseUrl", "string", "Discogs", "API URL to most recent release"),
                new("VersionsUrl", "string", "Discogs", "API URL to list all versions"),
                new("ResourceUrl", "string", "Discogs", "API URL for this resource"),
                new("Uri", "string", "Discogs", "Discogs website URL"),
                new("DataQuality", "string", "Discogs", "Data quality rating"),
                new("Artists", "List<ArtistRef>", "Discogs", "Primary artists"),
                new("Genres", "List<string>", "Discogs", "Genre classifications"),
                new("Styles", "List<string>", "Discogs", "Style/subgenre classifications"),
                new("Tracks", "List<Track>", "Discogs", "Canonical tracklist"),
                new("Images", "List<Image>", "Discogs", "Artwork"),
                new("Videos", "List<Video>", "Discogs", "Related videos"),
                new(
                    "QuantityForSale",
                    "int",
                    "Discogs",
                    "Number of copies for sale in marketplace"
                ),
                new("LowestPrice", "decimal", "Discogs", "Lowest marketplace price"),
            ]
        );

    public static EntitySchema GetFormatSchema() =>
        new(
            EntityName: "Format",
            Description: "Physical or digital format of a release",
            Fields:
            [
                // Discogs Fields
                new(
                    "Name",
                    "string",
                    "Discogs",
                    "Format name: Vinyl, CD, Cassette, File, DVD, Blu-ray, etc."
                ),
                new("Quantity", "string", "Discogs", "Number of items (e.g., '2' for double LP)"),
                new("Text", "string", "Discogs", "Free-form description"),
                new(
                    "Descriptions",
                    "List<string>",
                    "Discogs",
                    "Format descriptors: LP, Album, EP, Single, Compilation, Limited Edition, Reissue, Remastered, Mono, Stereo, 45 RPM, 33 RPM, 180g, Colored, Picture Disc, Shaped, Gatefold, etc."
                ),
                // MusicBrainz Fields
                new(
                    "Format",
                    "string",
                    "MusicBrainz",
                    "Format on Medium: CD, Vinyl, Digital Media, Cassette, DVD-Video, etc."
                ),
                new("Position", "int", "MusicBrainz", "Medium position in release"),
                new(
                    "Title",
                    "string",
                    "MusicBrainz",
                    "Medium title (e.g., 'Disc 1: Original Album')"
                ),
                new("TrackCount", "int", "MusicBrainz", "Number of tracks on this medium"),
                new("DiscId", "string", "MusicBrainz", "CD TOC disc ID"),
            ]
        );

    public static EntitySchema GetCreditSchema() =>
        new(
            EntityName: "Credit",
            Description: "Artist credit/relationship on a release or recording",
            Fields:
            [
                // Common/MusicBrainz Roles
                new("Name", "string", "Both", "Artist name"),
                new("Role", "string", "Both", "Relationship type/role"),
                new("ArtistId", "GUID/int", "Both", "Artist identifier"),
                // MusicBrainz Relationship Types
                new(
                    "Attributes",
                    "string",
                    "MusicBrainz",
                    "Role attributes (e.g., 'piano' for 'instrument')"
                ),
                // Common Role Types Include:
                new("composer", "Role", "MusicBrainz", "Wrote the music"),
                new("lyricist", "Role", "MusicBrainz", "Wrote the lyrics"),
                new("writer", "Role", "MusicBrainz", "Wrote both music and lyrics"),
                new("arranger", "Role", "MusicBrainz", "Created arrangement"),
                new("orchestrator", "Role", "MusicBrainz", "Created orchestration"),
                new("conductor", "Role", "MusicBrainz", "Conducted the performance"),
                new("performing orchestra", "Role", "MusicBrainz", "Orchestra that performed"),
                new("performer", "Role", "MusicBrainz", "General performer"),
                new("instrument", "Role", "MusicBrainz", "Played specific instrument"),
                new("vocal", "Role", "MusicBrainz", "Provided vocals"),
                new("producer", "Role", "Both", "Produced the recording"),
                new("engineer", "Role", "Both", "Audio engineer"),
                new("mix", "Role", "Both", "Mixed the recording"),
                new("mastering", "Role", "Both", "Mastered the recording"),
                new("recording", "Role", "Both", "Recording engineer"),
                new("programming", "Role", "Both", "Programmed electronic elements"),
                new("remixer", "Role", "Both", "Created remix version"),
                new("DJ-mix", "Role", "Both", "Created DJ mix"),
                new("liner notes", "Role", "Both", "Wrote liner notes"),
                new("photography", "Role", "Both", "Photographer"),
                new("art direction", "Role", "Both", "Art direction"),
                new("design", "Role", "Both", "Design work"),
                new("illustration", "Role", "Both", "Illustration"),
                // Discogs-specific Fields
                new("Tracks", "string", "Discogs", "Track numbers this credit applies to"),
                new("Anv", "string", "Discogs", "Artist name variation used on this credit"),
                new("Join", "string", "Discogs", "Join phrase to next artist"),
            ]
        );

    public static EntitySchema GetImageSchema() =>
        new(
            EntityName: "Image",
            Description: "Cover art, photos, or other images",
            Fields:
            [
                // MusicBrainz (Cover Art Archive)
                new("Front", "bool", "MusicBrainz", "Is this the front cover?"),
                new("Back", "bool", "MusicBrainz", "Is this the back cover?"),
                new(
                    "Types",
                    "List<string>",
                    "MusicBrainz",
                    "Image types: Front, Back, Booklet, Medium, Tray, Obi, Spine, Track, Liner, Sticker, Poster, Watermark, Raw/Unedited, Matrix/Runout, Other"
                ),
                new("Approved", "bool", "MusicBrainz", "Has the image been approved?"),
                new("Edit", "int", "MusicBrainz", "Edit ID that added this image"),
                new("ImageUrl", "string", "MusicBrainz", "Full-size image URL"),
                new("ThumbnailUrl", "string", "MusicBrainz", "Thumbnail URL"),
                // Discogs Fields
                new("Type", "string", "Discogs", "Image type: primary, secondary"),
                new("Uri", "string", "Discogs", "Full-size image URL"),
                new("Uri150", "string", "Discogs", "150px thumbnail URL"),
                new("Width", "int", "Discogs", "Image width"),
                new("Height", "int", "Discogs", "Image height"),
                new("ResourceUrl", "string", "Discogs", "API URL for image"),
            ]
        );

    public static EntitySchema GetVideoSchema() =>
        new(
            EntityName: "Video",
            Description: "Video links associated with releases",
            Fields:
            [
                new("Uri", "string", "Discogs", "Video URL (usually YouTube)"),
                new("Title", "string", "Discogs", "Video title"),
                new("Description", "string", "Discogs", "Video description"),
                new("Duration", "int", "Discogs", "Duration in seconds"),
                new("Embed", "bool", "Discogs", "Whether embedding is allowed"),
            ]
        );

    public static EntitySchema GetIdentifierSchema() =>
        new(
            EntityName: "Identifier",
            Description: "Barcodes, matrix numbers, and other identifiers",
            Fields:
            [
                new(
                    "Type",
                    "string",
                    "Discogs",
                    "Identifier type: Barcode, Matrix / Runout, ISRC, Rights Society, Label Code, ASIN, Mastering SID Code, Mould SID Code, Depósito Legal, SPARS Code, Other"
                ),
                new("Value", "string", "Discogs", "The identifier value"),
                new("Description", "string", "Discogs", "Additional description"),
            ]
        );

    public static EntitySchema GetCommunitySchema() =>
        new(
            EntityName: "Community",
            Description: "Community data from Discogs",
            Fields:
            [
                new("Have", "int", "Discogs", "Number of users who own this release"),
                new("Want", "int", "Discogs", "Number of users who want this release"),
                new("Rating", "double", "Discogs", "Average rating (0-5)"),
                new("RatingCount", "int", "Discogs", "Number of ratings"),
                new("Status", "string", "Discogs", "Submission status"),
                new("DataQuality", "string", "Discogs", "Data quality rating"),
                new("Submitter", "string", "Discogs", "Username who submitted"),
                new("Contributors", "List<string>", "Discogs", "Users who contributed"),
            ]
        );

    /// <summary>
    /// Get a summary of all available fields as a flat list.
    /// </summary>
    public static List<MetadataFieldSummary> GetAllFieldsSummary()
    {
        List<MetadataFieldSummary> fields = [];
        foreach ((string entityName, EntitySchema schema) in GetAllSchemas())
        {
            foreach (MetadataField field in schema.Fields)
            {
                fields.Add(
                    new MetadataFieldSummary(
                        Entity: entityName,
                        Field: field.Name,
                        Type: field.Type,
                        Source: field.Source,
                        Description: field.Description
                    )
                );
            }
        }
        return fields;
    }
}

/// <summary>
/// Schema for a music metadata entity.
/// </summary>
public record EntitySchema(string EntityName, string Description, List<MetadataField> Fields);

/// <summary>
/// Individual metadata field definition.
/// </summary>
public record MetadataField(string Name, string Type, string Source, string Description);

/// <summary>
/// Flattened field summary for display.
/// </summary>
public record MetadataFieldSummary(
    string Entity,
    string Field,
    string Type,
    string Source,
    string Description
);
