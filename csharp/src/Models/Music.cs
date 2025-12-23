namespace CSharpScripts.Models;

#region Enums

public enum MusicSource
{
    Discogs,
    MusicBrainz,
}

#endregion

#region Release Data

public record ReleaseInfo(
    MusicSource Source,
    string Id,
    string Title,
    string? Artist,
    string? Label,
    string? CatalogNumber,
    int? Year,
    string? Notes,
    int DiscCount,
    int TrackCount,
    TimeSpan? TotalDuration
);

public record ReleaseData(ReleaseInfo Info, List<TrackInfo> Tracks);

#endregion

#region Track Data

public record TrackInfo(
    int DiscNumber,
    int TrackNumber,
    string Title,
    TimeSpan? Duration,
    int? RecordingYear,
    string? Composer,
    string? WorkName,
    string? Conductor,
    string? Orchestra,
    List<string> Soloists,
    string? Artist,
    string? RecordingVenue,
    string? RecordingId = null
)
{
    public List<string> GetMissingFields()
    {
        List<string> missing = [];

        if (IsNullOrWhiteSpace(value: Composer))
            missing.Add(item: "Composer");

        if (IsNullOrWhiteSpace(value: WorkName) && IsNullOrWhiteSpace(value: Title))
            missing.Add(item: "Work/Title");

        if (Duration is null || Duration.Value == TimeSpan.Zero)
            missing.Add(item: "Duration");

        if (RecordingYear is null)
            missing.Add(item: "Recording Year");

        return missing;
    }
}

public record WorkSummary(
    int Disc,
    int FirstTrack,
    int LastTrack,
    string Work,
    string? Composer,
    List<int> Years,
    string? Conductor,
    string? Orchestra,
    List<string> Soloists,
    TimeSpan TotalDuration
)
{
    public string TrackRange =>
        FirstTrack == LastTrack ? FirstTrack.ToString() : $"{FirstTrack}-{LastTrack}";

    public string YearDisplay =>
        Years.Count switch
        {
            0 => "",
            1 => Years[index: 0].ToString(),
            _ when Years.Max() - Years.Min() <= 2 && Years.Count == Years.Max() - Years.Min() + 1 =>
                $"{Years.Min()}-{Years.Max() % 100:D2}",
            _ => Join(separator: ", ", Years.Distinct().OrderBy(y => y)),
        };
}

#endregion

#region Search

public record SearchResult(
    MusicSource Source,
    string Id,
    string Title,
    string? Artist,
    int? Year,
    string? Format,
    string? Label,
    string? ReleaseType,
    int? Score = null,
    string? Country = null,
    string? CatalogNumber = null,
    string? Status = null,
    string? Disambiguation = null,
    List<string>? Genres = null,
    List<string>? Styles = null
);

#endregion
