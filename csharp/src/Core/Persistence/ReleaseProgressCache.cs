namespace CSharpScripts.Core;

internal static class ReleaseProgressCache
{
	private static readonly CsvConfiguration CsvConfig = new(CultureInfo.InvariantCulture);

	private static string GetPath(string releaseId) =>
		Path.Combine(Paths.CacheDirectory, $"{releaseId}.csv");

	public static void AppendTrack(string releaseId, TrackInfo track)
	{
		Directory.CreateDirectory(Paths.CacheDirectory);
		var path = GetPath(releaseId);

		var writeHeader = !File.Exists(path);
		using StreamWriter writer = new(path, append: true);
		using CsvWriter csv = new(writer, CsvConfig with { HasHeaderRecord = writeHeader });

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
		var path = GetPath(releaseId);
		if (!File.Exists(path))
			return [];

		using StreamReader reader = new(path);
		using CsvReader csv = new(reader, CsvConfig with { HasHeaderRecord = true });

		return [.. csv.GetRecords<TrackInfo>()];
	}

	public static void Delete(string releaseId)
	{
		var path = GetPath(releaseId: releaseId);
		if (File.Exists(path: path))
			File.Delete(path: path);
	}
}


