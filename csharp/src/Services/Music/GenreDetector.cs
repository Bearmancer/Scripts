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
		StringComparer.OrdinalIgnoreCase
	);

	private static readonly FrozenSet<string> JazzTerms = FrozenSet.ToFrozenSet(
		["jazz", "bebop", "swing", "blues", "bossa nova", "latin jazz", "free jazz", "jazz fusion"],
		StringComparer.OrdinalIgnoreCase
	);

	internal static MusicGenreCategory Detect(MusicBrainzRelease release)
	{
		var hasConductor = false;
		var hasOrchestra = false;
		foreach (MusicBrainzCredit c in release.Credits)
		{
			if (MusicBrainzMapper.ConductorRoles.Contains(c.Role))
				hasConductor = true;
			if (MusicBrainzMapper.OrchestraRoles.Contains(c.Role))
				hasOrchestra = true;
			if (hasConductor && hasOrchestra)
				break;
		}

		if (hasConductor || hasOrchestra)
			return MusicGenreCategory.Classical;

		IEnumerable<string> allTerms = release.Genres.Concat(release.Tags);

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

		return Enumerable.MaxBy(counts, kv => kv.Value).Key;
	}
}
