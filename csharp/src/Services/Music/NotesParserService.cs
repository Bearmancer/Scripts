namespace CSharpScripts.Services.Music;

internal static partial class NotesParserService
{
	internal static ParsedNotes Parse(string notes)
	{
		if (IsNullOrWhiteSpace(notes))
			return new ParsedNotes([], [], [], [], [], [], notes ?? "");

		var lines = notes.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);

		List<string> composers = [];
		List<string> conductors = [];
		List<string> orchestras = [];
		List<string> venues = [];
		List<RecordingDate> recordingDates = [];
		List<TrackAnnotation> trackAnnotations = [];

		foreach (var rawLine in lines)
		{
			var line = rawLine.Trim();

			foreach (Match m in ComposerPattern().Matches(line))
			{
				var name = m.Groups["name"].Value.Trim();
				if (!IsNullOrEmpty(name))
					composers.Add(name);
			}

			foreach (Match m in ConductorPattern().Matches(line))
			{
				var name = m.Groups["name"].Value.Trim();
				if (!IsNullOrEmpty(name))
					conductors.Add(name);
			}

			foreach (Match m in OrchestraPattern().Matches(line))
			{
				var name = m.Value.Trim();
				if (!IsNullOrEmpty(name))
					orchestras.Add(name);
			}

			var venue = DiscogsMapper.ExtractVenueFromLine(line);
			if (!IsNullOrEmpty(venue))
				venues.Add(venue);

			if (RecordingContextPattern().IsMatch(line))
			{
				foreach (Match dm in DatePattern().Matches(line))
				{
					var dateText = dm.Value.Trim();
					recordingDates.Add(new RecordingDate(line, TryParseDate(dateText)));
				}
			}

			foreach (Match m in TrackAnnotationPattern().Matches(line))
			{
				var trackRef = m.Groups["ref"].Value.Trim();
				var annotation = m.Groups["annotation"].Value.Trim();
				if (!IsNullOrEmpty(annotation))
					trackAnnotations.Add(new TrackAnnotation(trackRef, annotation));
			}
		}

		return new ParsedNotes(
			[.. Enumerable.Distinct(composers, StringComparer.OrdinalIgnoreCase)],
			[.. Enumerable.Distinct(conductors, StringComparer.OrdinalIgnoreCase)],
			[.. Enumerable.Distinct(orchestras, StringComparer.OrdinalIgnoreCase)],
			[.. Enumerable.Distinct(venues, StringComparer.OrdinalIgnoreCase)],
			recordingDates,
			trackAnnotations,
			notes
		);
	}

	private static DateOnly? TryParseDate(string text)
	{
		ReadOnlySpan<char> span = MemoryExtensions.AsSpan(text);

		if (
			DateOnly.TryParseExact(
				span,
				"MMMM yyyy",
				CultureInfo.InvariantCulture,
				DateTimeStyles.None,
				out DateOnly d1
			)
		)
			return d1;
		if (
			DateOnly.TryParseExact(
				span,
				"yyyy-MM-dd",
				CultureInfo.InvariantCulture,
				DateTimeStyles.None,
				out DateOnly d2
			)
		)
			return d2;
		if (
			DateOnly.TryParseExact(
				span,
				"d.M.yyyy",
				CultureInfo.InvariantCulture,
				DateTimeStyles.None,
				out DateOnly d3
			)
		)
			return d3;

		return null;
	}

	[GeneratedRegex(
		@"(?:Composed by|Music by|Written by)\s+(?<name>[A-Z][^,\n\.]+?)(?=\s*[,\n\.]|\s*$)",
		RegexOptions.IgnoreCase
	)]
	private static partial Regex ComposerPattern();

	[GeneratedRegex(
		@"(?:Conducted by|Direction[:\s]+)\s*(?<name>[A-Z][^,\n\.]+?)(?=\s*[,\n\.]|\s*$)",
		RegexOptions.IgnoreCase
	)]
	private static partial Regex ConductorPattern();

	[GeneratedRegex(
		@"\b(?:[A-Z][A-Za-z]+(?:\s+[A-Z][A-Za-z]+)*)\s+(?:Orchestra|Philharmonic|Philharmoniker|Symphony|Sinfoniker|Ensemble)\b"
	)]
	private static partial Regex OrchestraPattern();

	[GeneratedRegex(@"\b[Rr]ecorded\b")]
	private static partial Regex RecordingContextPattern();

	[GeneratedRegex(
		@"\b(?:January|February|March|April|May|June|July|August|September|October|November|December)\s+\d{4}|\d{4}-\d{2}-\d{2}|\d{1,2}\.\d{1,2}\.\d{4}\b"
	)]
	private static partial Regex DatePattern();

	[GeneratedRegex(
		@"(?<ref>(?:(?:CD|Disc|Side)\s+\d+,\s+)?(?:Track|Side)\s+[\d\-]+)\s*:\s*(?<annotation>[^\n]+)",
		RegexOptions.IgnoreCase
	)]
	private static partial Regex TrackAnnotationPattern();
}


