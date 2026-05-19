namespace CSharpScripts.Services.Music;

internal static partial class NotesParserService
{
	internal static ParsedNotes Parse(string notes)
	{
		if (IsNullOrWhiteSpace(value: notes))
			return new ParsedNotes([], [], [], [], [], [], notes ?? "");

		var lines = notes.Split(['\n', '\r'], options: StringSplitOptions.RemoveEmptyEntries);

		List<string> composers = [];
		List<string> conductors = [];
		List<string> orchestras = [];
		List<string> venues = [];
		List<RecordingDate> recordingDates = [];
		List<TrackAnnotation> trackAnnotations = [];

		foreach (var rawLine in lines)
		{
			var line = rawLine.Trim();

			foreach (Match m in ComposerPattern().Matches(input: line))
			{
				var name = m.Groups[groupname: "name"].Value.Trim();
				if (!IsNullOrEmpty(value: name))
					composers.Add(item: name);
			}

			foreach (Match m in ConductorPattern().Matches(input: line))
			{
				var name = m.Groups[groupname: "name"].Value.Trim();
				if (!IsNullOrEmpty(value: name))
					conductors.Add(item: name);
			}

			foreach (Match m in OrchestraPattern().Matches(input: line))
			{
				var name = m.Value.Trim();
				if (!IsNullOrEmpty(value: name))
					orchestras.Add(item: name);
			}

			var venue = DiscogsMapper.ExtractVenueFromLine(line: line);
			if (!IsNullOrEmpty(value: venue))
				venues.Add(item: venue);

			if (RecordingContextPattern().IsMatch(input: line))
			{
				foreach (Match dm in DatePattern().Matches(input: line))
				{
					var dateText = dm.Value.Trim();
					recordingDates.Add(new RecordingDate(Description: line, TryParseDate(text: dateText)));
				}
			}

			foreach (Match m in TrackAnnotationPattern().Matches(input: line))
			{
				var trackRef = m.Groups[groupname: "ref"].Value.Trim();
				var annotation = m.Groups[groupname: "annotation"].Value.Trim();
				if (!IsNullOrEmpty(value: annotation))
					trackAnnotations.Add(new TrackAnnotation(TrackReference: trackRef, Annotation: annotation));
			}
		}

		return new ParsedNotes(
			[.. composers.Distinct(comparer: StringComparer.OrdinalIgnoreCase)],
			[.. conductors.Distinct(comparer: StringComparer.OrdinalIgnoreCase)],
			[.. orchestras.Distinct(comparer: StringComparer.OrdinalIgnoreCase)],
			[.. venues.Distinct(comparer: StringComparer.OrdinalIgnoreCase)],
			RecordingDates: recordingDates,
			TrackAnnotations: trackAnnotations,
			RawNotes: notes
		);
	}

	private static DateOnly? TryParseDate(string text)
	{
		ReadOnlySpan<char> span = text.AsSpan();

		if (
			DateOnly.TryParseExact(
				s: span,
				format: "MMMM yyyy",
				provider: CultureInfo.InvariantCulture,
				style: DateTimeStyles.None,
				out DateOnly d1
			)
		)
			return d1;
		if (
			DateOnly.TryParseExact(
				s: span,
				format: "yyyy-MM-dd",
				provider: CultureInfo.InvariantCulture,
				style: DateTimeStyles.None,
				out DateOnly d2
			)
		)
			return d2;
		if (
			DateOnly.TryParseExact(
				s: span,
				format: "d.M.yyyy",
				provider: CultureInfo.InvariantCulture,
				style: DateTimeStyles.None,
				out DateOnly d3
			)
		)
			return d3;

		return null;
	}

	[GeneratedRegex(
		pattern: @"(?:Composed by|Music by|Written by)\s+(?<name>[A-Z][^,\n\.]+?)(?=\s*[,\n\.]|\s*$)",
		options: RegexOptions.IgnoreCase
	)]
	private static partial Regex ComposerPattern();

	[GeneratedRegex(
		pattern: @"(?:Conducted by|Direction[:\s]+)\s*(?<name>[A-Z][^,\n\.]+?)(?=\s*[,\n\.]|\s*$)",
		options: RegexOptions.IgnoreCase
	)]
	private static partial Regex ConductorPattern();

	[GeneratedRegex(
		pattern: @"\b(?:[A-Z][A-Za-z]+(?:\s+[A-Z][A-Za-z]+)*)\s+(?:Orchestra|Philharmonic|Philharmoniker|Symphony|Sinfoniker|Ensemble)\b"
	)]
	private static partial Regex OrchestraPattern();

	[GeneratedRegex(pattern: @"\b[Rr]ecorded\b")]
	private static partial Regex RecordingContextPattern();

	[GeneratedRegex(
		pattern: @"\b(?:January|February|March|April|May|June|July|August|September|October|November|December)\s+\d{4}|\d{4}-\d{2}-\d{2}|\d{1,2}\.\d{1,2}\.\d{4}\b"
	)]
	private static partial Regex DatePattern();

	[GeneratedRegex(
		pattern: @"(?<ref>(?:(?:CD|Disc|Side)\s+\d+,\s+)?(?:Track|Side)\s+[\d\-]+)\s*:\s*(?<annotation>[^\n]+)",
		options: RegexOptions.IgnoreCase
	)]
	private static partial Regex TrackAnnotationPattern();
}
