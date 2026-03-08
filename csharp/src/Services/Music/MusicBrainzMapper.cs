namespace CSharpScripts.Services.Music;

internal static class MusicBrainzMapper
{
	private static List<string> ExtractNames<T>(IEnumerable<T>? items, Func<T, string?> selector) =>
		items?.Select(selector).Where(n => !string.IsNullOrEmpty(n)).Cast<string>().ToList() ?? [];

	internal static MusicBrainzRelease MapRelease(IRelease r)
	{
		List<MusicBrainzMedium> media = [];
		if (r.Media is { } mediaList)
			foreach (IMedium medium in mediaList)
			{
				List<MusicBrainzTrack> tracks = [];
				if (medium.Tracks is { } trackList)
					foreach (ITrack track in trackList)
						tracks.Add(
							new MusicBrainzTrack(
								Id: track.Id,
								track.Title ?? track.Recording?.Title ?? "",
								track.Position ?? 0,
								Number: track.Number,
								Length: track.Length,
								RecordingId: track.Recording?.Id,
								FormatArtistCredit(track.ArtistCredit)
							)
						);

				media.Add(
					new MusicBrainzMedium(
						Position: medium.Position,
						Format: medium.Format,
						Title: medium.Title,
						TrackCount: medium.TrackCount,
						Tracks: tracks
					)
				);
			}

		List<MusicBrainzCredit> credits = [];
		if (r.Relationships is { } relationships)
			foreach (IRelationship rel in relationships)
				if (rel.Artist is { } artist && !IsNullOrEmpty(rel.Type))
					credits.Add(
						new MusicBrainzCredit(
							artist.Name ?? "",
							Role: rel.Type,
							ArtistId: artist.Id,
							rel.Attributes is { } attrs
								? Join(separator: ", ", values: attrs)
								: null
						)
					);

		List<MusicBrainzLabel> labels = [];
		if (r.LabelInfo is { } labelInfo)
			foreach (ILabelInfo li in labelInfo)
				labels.Add(
					new MusicBrainzLabel(
						Id: li.Label?.Id,
						Name: li.Label?.Name,
						CatalogNumber: li.CatalogNumber
					)
				);

		return new MusicBrainzRelease(
			Id: r.Id,
			r.Title ?? "",
			Artist: r.ArtistCredit?.FirstOrDefault()?.Artist?.Name,
			FormatArtistCredit(r.ArtistCredit),
			r.Date?.NearestDate is DateTime dt ? DateOnly.FromDateTime(dt) : null,
			Country: r.Country,
			Status: r.Status,
			Barcode: r.Barcode,
			Asin: r.Asin,
			Quality: r.Quality,
			Packaging: r.Packaging,
			Disambiguation: r.Disambiguation,
			ReleaseGroupId: r.ReleaseGroup?.Id,
			ReleaseGroupTitle: r.ReleaseGroup?.Title,
			ReleaseGroupType: r.ReleaseGroup?.PrimaryType,
			Media: media,
			Credits: credits,
			Labels: labels,
			ExtractNames(r.Tags, t => t.Name),
			ExtractNames(r.Genres, g => g.Name),
			Annotation: r.Annotation
		);
	}

	internal static MusicBrainzArtist MapArtist(IArtist a) =>
		new(
			Id: a.Id,
			a.Name ?? "",
			SortName: a.SortName,
			Type: a.Type,
			Gender: a.Gender,
			Country: a.Country,
			Area: a.Area?.Name,
			Disambiguation: a.Disambiguation,
			a.LifeSpan?.Begin?.NearestDate is DateTime b ? DateOnly.FromDateTime(b) : null,
			a.LifeSpan?.End?.NearestDate is DateTime e ? DateOnly.FromDateTime(e) : null,
			Ended: a.LifeSpan?.Ended,
			ExtractNames(a.Aliases, al => al.Name),
			ExtractNames(a.Tags, t => t.Name),
			ExtractNames(a.Genres, g => g.Name),
			Annotation: a.Annotation,
			(double?)a.Rating?.Value,
			RatingVotes: a.Rating?.VoteCount
		);

	internal static MusicBrainzRecording MapRecording(IRecording r)
	{
		IRelationship? workRelationship = r.Relationships?.FirstOrDefault(rel =>
			rel.Work is not null
		);
		var workName = workRelationship?.Work?.Title;
		Guid? workId = workRelationship?.Work?.Id;

		string? conductor = null;
		string? orchestra = null;
		string? recordingVenue = null;
		DateOnly? recordingDate = null;

		if (r.Relationships is { } relationships)
			foreach (IRelationship rel in relationships)
			{
				TryExtractConductor(rel, ref conductor, ref recordingDate);
				TryExtractOrchestra(rel, ref orchestra);
				TryExtractVenue(rel, ref recordingVenue, ref recordingDate);
			}

		return new MusicBrainzRecording(
			Id: r.Id,
			r.Title ?? "",
			Artist: r.ArtistCredit?.FirstOrDefault()?.Artist?.Name,
			FormatArtistCredit(r.ArtistCredit),
			Length: r.Length,
			r.FirstReleaseDate?.NearestDate is DateTime dt ? DateOnly.FromDateTime(dt) : null,
			IsVideo: r.Video,
			Disambiguation: r.Disambiguation,
			r.Isrcs?.ToList() ?? [],
			ExtractNames(r.Tags, t => t.Name),
			ExtractNames(r.Genres, g => g.Name),
			(double?)r.Rating?.Value,
			RatingVotes: r.Rating?.VoteCount,
			Annotation: r.Annotation,
			WorkName: workName,
			WorkId: workId,
			Conductor: conductor,
			Orchestra: orchestra,
			RecordingVenue: recordingVenue,
			RecordingDate: recordingDate
		);
	}

	private static void TryExtractConductor(
		IRelationship rel,
		ref string? conductor,
		ref DateOnly? recordingDate
	)
	{
		var relType = rel.Type?.ToLowerInvariant();
		if (relType is null)
			return;

		if (relType.EqualsExact("conductor") && rel.Artist is { } conductorArtist)
		{
			conductor = conductorArtist.Name;
			if (recordingDate is null && rel.Begin?.NearestDate is DateTime beginDate)
				recordingDate = DateOnly.FromDateTime(beginDate);
		}
	}

	private static void TryExtractOrchestra(IRelationship rel, ref string? orchestra)
	{
		var relType = rel.Type?.ToLowerInvariant();
		if (relType is null)
			return;

		if (
			(
				relType
					is "orchestra"
						or "performing orchestra"
						or "ensemble"
						or "choir"
						or "philharmonic"
				|| (
					relType.EqualsExact("instrument")
					&& rel.Artist?.Name is { } name
					&& (
						name.ContainsIgnoreCase("Orchestra")
						|| name.ContainsIgnoreCase("Philharmonic")
						|| name.ContainsIgnoreCase("Symphony")
						|| name.ContainsIgnoreCase("Choir")
					)
				)
			) && rel.Artist is { } orchestraArtist
		)
		{
			orchestra = orchestraArtist.Name;
		}
	}

	private static void TryExtractVenue(
		IRelationship rel,
		ref string? recordingVenue,
		ref DateOnly? recordingDate
	)
	{
		var relType = rel.Type?.ToLowerInvariant();
		if (relType is null)
			return;

		if (relType is "recorded at" or "recorded in" && rel.Place is { } place)
		{
			recordingVenue = place.Name;
			if (recordingDate is null && rel.Begin?.NearestDate is DateTime beginDate)
				recordingDate = DateOnly.FromDateTime(beginDate);
		}
	}

	internal static MusicBrainzRecording MapRecordingFromSearch(IRecording r) =>
		new(
			Id: r.Id,
			r.Title ?? "",
			Artist: r.ArtistCredit?.FirstOrDefault()?.Artist?.Name,
			FormatArtistCredit(r.ArtistCredit),
			Length: r.Length,
			r.FirstReleaseDate?.NearestDate is DateTime dt ? DateOnly.FromDateTime(dt) : null,
			IsVideo: r.Video,
			Disambiguation: r.Disambiguation,
			r.Isrcs?.ToList() ?? [],
			[],
			[],
			Rating: null,
			RatingVotes: null,
			Annotation: null
		);

	internal static readonly FrozenSet<string> ExcludedRoles = FrozenSet.ToFrozenSet(
		[
			"choir",
			"chorus",
			"chorus master",
			"choir conductor",
			"choir director",
			"vocal",
			"vocals",
			"singer",
			"soprano",
			"mezzo-soprano",
			"alto",
			"contralto",
			"tenor",
			"baritone",
			"bass",
			"bass-baritone",
			"narrator",
			"speaker",
		],
		comparer: StringComparer.OrdinalIgnoreCase
	);

	internal static readonly FrozenSet<string> ConductorRoles = FrozenSet.ToFrozenSet(
		["conductor", "director"],
		comparer: StringComparer.OrdinalIgnoreCase
	);

	internal static readonly FrozenSet<string> OrchestraRoles = FrozenSet.ToFrozenSet(
		["orchestra", "performing orchestra", "ensemble", "performer", "choir", "philharmonic"],
		comparer: StringComparer.OrdinalIgnoreCase
	);

	internal static readonly FrozenSet<string> SoloistRoles = FrozenSet.ToFrozenSet(
		[
			"instrument",
			"piano",
			"violin",
			"viola",
			"cello",
			"double bass",
			"flute",
			"oboe",
			"clarinet",
			"bassoon",
			"horn",
			"trumpet",
			"trombone",
			"tuba",
			"harp",
			"organ",
			"harpsichord",
			"guitar",
			"percussion",
			"timpani",
			"soloist",
		],
		comparer: StringComparer.OrdinalIgnoreCase
	);

	internal static string? FormatArtistCredit(IReadOnlyList<INameCredit>? credits)
	{
		if (credits is null || credits.Count == 0)
			return null;

		return Join(
			"",
			credits.Select(c => (c.Name ?? c.Artist?.Name ?? "") + (c.JoinPhrase ?? ""))
		);
	}
}
