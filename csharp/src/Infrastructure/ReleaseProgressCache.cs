namespace CSharpScripts.Infrastructure;

internal static class ReleaseProgressCache
{
	private static string GetPath(string releaseId) =>
		Combine(Paths.CacheDirectory, $"{releaseId}.csv");

	public static void AppendTrack(string releaseId, TrackInfo track)
	{
		CreateDirectory(Paths.CacheDirectory);
		var path = GetPath(releaseId);

		var writeHeader = !File.Exists(path);
		using StreamWriter writer = new(path, append: true);
		using CsvWriter csv = new(
			writer,
			new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = writeHeader }
		);

		if (writeHeader)
		{
			csv.WriteHeader<TrackInfo>();
			csv.NextRecord();
		}
		csv.WriteRecord(track);
		csv.NextRecord();
	}

	public static List<TrackInfo> Load(string releaseId)
	{
		var path = GetPath(releaseId);
		if (!File.Exists(path))
			return [];

		using StreamReader reader = new(path);
		using CsvReader csv = new(
			reader,
			new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true }
		);

		return [.. csv.GetRecords<TrackInfo>()];
	}

	public static void Delete(string releaseId)
	{
		var path = GetPath(releaseId);
		if (File.Exists(path))
			File.Delete(path);
	}
}
