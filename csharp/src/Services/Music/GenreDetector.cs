namespace CSharpScripts.Services.Music;

internal enum MusicGenreCategory
{
	Classical,
	Pop,
	Jazz,
	Unknown
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
			"modern classical"
		],
		comparer: StringComparer.OrdinalIgnoreCase
	);

	private static readonly FrozenSet<string> JazzTerms = FrozenSet.ToFrozenSet(
		["jazz", "bebop", "swing", "blues", "bossa nova", "latin jazz", "free jazz", "jazz fusion"],
		comparer: StringComparer.OrdinalIgnoreCase
	);

	internal static MusicGenreCategory Detect(MusicBrainzRelease release)
	{
		var hasConductor = false;
		var hasOrchestra = false;
		foreach (MusicBrainzCredit c in release.Credits)
		{
			if (MusicBrainzMapper.ConductorRoles.Contains(item: c.Role))
				hasConductor = true;
			if (MusicBrainzMapper.OrchestraRoles.Contains(item: c.Role))
				hasOrchestra = true;
			if (hasConductor && hasOrchestra)
				break;
		}

		if (hasConductor || hasOrchestra)
			return MusicGenreCategory.Classical;

		IEnumerable<string> allTerms = release.Genres.Concat(second: release.Tags);

		if (ClassicalTerms.Overlaps(other: allTerms))
			return MusicGenreCategory.Classical;

		if (JazzTerms.Overlaps(other: allTerms))
			return MusicGenreCategory.Jazz;

		return MusicGenreCategory.Pop;
	}

	internal static MusicGenreCategory Detect(RecordingInput record)
	{
		if (!IsNullOrEmpty(value: record.Composer) || !IsNullOrEmpty(value: record.Work))
			return MusicGenreCategory.Classical;

		if (!IsNullOrEmpty(value: record.Orchestra) || !IsNullOrEmpty(value: record.Conductor))
			return MusicGenreCategory.Classical;

		return MusicGenreCategory.Unknown;
	}

	internal static MusicGenreCategory DetectFromRecordings(List<RecordingInput> records)
	{
		if (records.Count == 0)
			return MusicGenreCategory.Unknown;

		Dictionary<MusicGenreCategory, int> counts = new()
		{
			[key: MusicGenreCategory.Classical] = 0,
			[key: MusicGenreCategory.Pop] = 0,
			[key: MusicGenreCategory.Jazz] = 0,
			[key: MusicGenreCategory.Unknown] = 0
		};

		foreach (RecordingInput record in records)
			counts[Detect(record: record)]++;

		return counts.MaxBy(static kv => kv.Value).Key;
	}
}
