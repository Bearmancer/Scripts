using System.Globalization;
using CSharpScripts.Models;
using CsvHelper;
using CsvHelper.Configuration;

namespace CSharpScripts.Infrastructure;

internal static class ReleaseProgressCache
{
    private static string GetPath(string releaseId) =>
        Combine(path1: Paths.CacheDirectory, $"{releaseId}.csv");

    public static void AppendTrack(string releaseId, TrackInfo track)
    {
        CreateDirectory(path: Paths.CacheDirectory);
        string path = GetPath(releaseId: releaseId);

        bool writeHeader = !File.Exists(path: path);
        using StreamWriter writer = new(path: path, append: true);
        using CsvWriter csv = new(
            writer: writer,
            new CsvConfiguration(cultureInfo: CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = writeHeader,
            }
        );

        if (writeHeader)
        {
            csv.WriteHeader<TrackInfo>();
            csv.NextRecord();
        }
        csv.WriteRecord(record: track);
        csv.NextRecord();
    }

    public static List<TrackInfo> Load(string releaseId)
    {
        string path = GetPath(releaseId: releaseId);
        if (!File.Exists(path: path))
            return [];

        using StreamReader reader = new(path: path);
        using CsvReader csv = new(
            reader: reader,
            new CsvConfiguration(cultureInfo: CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
            }
        );

        return [.. csv.GetRecords<TrackInfo>()];
    }

    public static void Delete(string releaseId)
    {
        string path = GetPath(releaseId: releaseId);
        if (File.Exists(path: path))
            File.Delete(path: path);
    }
}
