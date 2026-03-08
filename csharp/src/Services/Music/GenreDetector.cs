namespace CSharpScripts.Services.Music;

internal enum MusicGenreCategory
{
	Classical,
	Pop,
	Jazz,
	Unknown,
}

internal static class GenreDetector
{
	private static readonly FrozenSet<string> ClassicalTerms = FrozenSet.ToFrozenSet(
		[
			"classical",
			"opera",
			"baroque",
			"romantic",
			"contemporary classical",
			"chamber music",
			"orchestral",
			"symphony",
			"concerto",
			"sonata",
			"choral",
			"early music",
			"renaissance",
			"medieval",
			"modern classical",
		],
		comparer: StringComparer.OrdinalIgnoreCase
	);

	private static readonly FrozenSet<string> JazzTerms = FrozenSet.ToFrozenSet(
		["jazz", "bebop", "swing", "blues", "bossa nova", "latin jazz", "free jazz", "jazz fusion"],
		comparer: StringComparer.OrdinalIgnoreCase
	);

	internal static MusicGenreCategory Detect(MusicBrainzRelease release)
	{
		var hasConductor = release.Credits.Any(c =>
			MusicBrainzMapper.ConductorRoles.Contains(c.Role)
		);
		var hasOrchestra = release.Credits.Any(c =>
			MusicBrainzMapper.OrchestraRoles.Contains(c.Role)
		);

		if (hasConductor || hasOrchestra)
			return MusicGenreCategory.Classical;

		List<string> allTerms = [.. release.Genres, .. release.Tags];

		if (ClassicalTerms.Overlaps(allTerms))
			return MusicGenreCategory.Classical;

		if (JazzTerms.Overlaps(allTerms))
			return MusicGenreCategory.Jazz;

		return MusicGenreCategory.Pop;
	}

	internal static MusicGenreCategory Detect(RecordingInput record)
	{
		if (!IsNullOrEmpty(record.Composer) || !IsNullOrEmpty(record.Work))
			return MusicGenreCategory.Classical;

		if (!IsNullOrEmpty(record.Orchestra) || !IsNullOrEmpty(record.Conductor))
			return MusicGenreCategory.Classical;

		return MusicGenreCategory.Unknown;
	}

	internal static MusicGenreCategory DetectFromRecordings(List<RecordingInput> records)
	{
		if (records.Count == 0)
			return MusicGenreCategory.Unknown;

		Dictionary<MusicGenreCategory, int> counts = new()
		{
			[MusicGenreCategory.Classical] = 0,
			[MusicGenreCategory.Pop] = 0,
			[MusicGenreCategory.Jazz] = 0,
			[MusicGenreCategory.Unknown] = 0,
		};

		foreach (RecordingInput record in records)
			counts[Detect(record)]++;

		return counts.MaxBy(kv => kv.Value).Key;
	}
}
