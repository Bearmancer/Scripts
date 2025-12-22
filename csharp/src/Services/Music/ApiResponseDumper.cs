namespace CSharpScripts.Services.Music;

public static class ApiResponseDumper
{
    static readonly string BaseDir = Paths.DumpsDirectory;

    static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    #region MusicBrainz Dumps

    public static void DumpRelease(Guid releaseId, object release)
    {
        string dir = Combine(BaseDir, "musicbrainz", "releases", releaseId.ToString());
        CreateDirectory(dir);
        WriteJson(Combine(dir, "release.json"), release);
    }

    public static void DumpRecording(
        Guid releaseId,
        int disc,
        int track,
        Guid recordingId,
        object recording
    )
    {
        string dir = Combine(
            BaseDir,
            "musicbrainz",
            "releases",
            releaseId.ToString(),
            "recordings"
        );
        CreateDirectory(dir);
        string fileName = $"{disc}.{track:D2}-{recordingId}.json";
        WriteJson(Combine(dir, fileName), recording);
    }

    public static void DumpWork(Guid releaseId, Guid workId, object work)
    {
        string dir = Combine(BaseDir, "musicbrainz", "releases", releaseId.ToString(), "works");
        CreateDirectory(dir);
        WriteJson(Combine(dir, $"{workId}.json"), work);
    }

    public static void DumpStandaloneRecording(Guid recordingId, object recording)
    {
        string dir = Combine(BaseDir, "musicbrainz", "recordings");
        CreateDirectory(dir);
        WriteJson(Combine(dir, $"{recordingId}.json"), recording);
    }

    public static void DumpStandaloneWork(Guid workId, object work)
    {
        string dir = Combine(BaseDir, "musicbrainz", "works");
        CreateDirectory(dir);
        WriteJson(Combine(dir, $"{workId}.json"), work);
    }

    #endregion

    #region Discogs Dumps

    public static void DumpDiscogsRelease(int releaseId, object release)
    {
        string dir = Combine(BaseDir, "discogs", "releases", releaseId.ToString());
        CreateDirectory(dir);
        WriteJson(Combine(dir, "release.json"), release);
    }

    public static void DumpDiscogsMaster(int masterId, object master)
    {
        string dir = Combine(BaseDir, "discogs", "masters");
        CreateDirectory(dir);
        WriteJson(Combine(dir, $"{masterId}.json"), master);
    }

    #endregion

    #region Missing Field Logging

    static readonly string CsvDirectory = Combine(Paths.StateDirectory, "csv");

    static readonly string MissingFieldsCsvPath = Combine(CsvDirectory, "missing-fields.csv");

    static bool headerWritten;

    public static void LogMissingFields(
        string releaseId,
        int disc,
        int track,
        string title,
        List<string> missingFields
    )
    {
        if (missingFields.Count == 0)
            return;

        CreateDirectory(CsvDirectory);

        // Write header if this is a new file
        if (!headerWritten && !File.Exists(MissingFieldsCsvPath))
        {
            AppendAllText(MissingFieldsCsvPath, "ReleaseId,Disc,Track,Title,MissingFields\n");
            headerWritten = true;
        }

        // Escape CSV fields
        string escapedTitle =
            title.Contains(',') || title.Contains('"')
                ? $"\"{title.Replace("\"", "\"\"")}\""
                : title;
        string fields = Join("; ", missingFields);

        string row = $"{releaseId},{disc},{track},{escapedTitle},{fields}\n";
        AppendAllText(MissingFieldsCsvPath, row);
    }

    /// <summary>
    /// Validates mandatory fields for a classical track and returns list of missing ones.
    /// Mandatory: Composer, Work/Title, Duration, RecordingYear
    /// Optional: Soloists
    /// </summary>
    public static List<string> ValidateMandatoryFields(
        string? composer,
        string? work,
        string? title,
        TimeSpan? duration,
        int? recordingYear
    )
    {
        List<string> missing = [];

        if (IsNullOrWhiteSpace(composer))
            missing.Add("Composer");

        if (IsNullOrWhiteSpace(work) && IsNullOrWhiteSpace(title))
            missing.Add("Work/Title");

        if (duration is null || duration.Value == TimeSpan.Zero)
            missing.Add("Duration");

        if (recordingYear is null)
            missing.Add("Recording Year");

        return missing;
    }

    #endregion

    static void WriteJson(string path, object obj)
    {
        string json = JsonSerializer.Serialize(obj, JsonOptions);
        WriteAllText(path, json);
    }
}
