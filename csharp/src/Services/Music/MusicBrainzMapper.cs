using System.Text;

namespace Scripts.Services.Music;

internal static class MusicBrainzMapper
{
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

	private static List<string> ExtractNames<T>(IEnumerable<T>? items, Func<T, string?> selector)
	{
		if (items is null)
			return [];

		List<string> result = [];
		foreach (T item in items)
		{
			var name = selector(arg: item);
			if (!IsNullOrEmpty(value: name))
				result.Add(item: name);
		}
		return result;
	}

	internal static MusicBrainzRelease MapRelease(IRelease r)
	{
		List<MusicBrainzMedium> media = [];
		if (r.Media is { } mediaList)
		{
			foreach (IMedium medium in mediaList)
			{
				List<MusicBrainzTrack> tracks = [];
				if (medium.Tracks is { } trackList)
				{
					foreach (ITrack track in trackList)
					{
						tracks.Add(
							new MusicBrainzTrack(
								Id: track.Id,
								track.Title ?? track.Recording?.Title ?? "",
								track.Position ?? 0,
								Number: track.Number,
								Length: track.Length,
								RecordingId: track.Recording?.Id,
								FormatArtistCredit(credits: track.ArtistCredit)
							)
						);
					}
				}

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
		}

		List<MusicBrainzCredit> credits = [];
		if (r.Relationships is { } relationships)
		{
			foreach (IRelationship rel in relationships)
			{
				if (rel.Artist is { } artist && !IsNullOrEmpty(value: rel.Type))
				{
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
				}
			}
		}

		List<MusicBrainzLabel> labels = [];
		if (r.LabelInfo is { } labelInfo)
		{
			foreach (ILabelInfo li in labelInfo)
				labels.Add(
					new MusicBrainzLabel(
						Id: li.Label?.Id,
						Name: li.Label?.Name,
						CatalogNumber: li.CatalogNumber
					)
				);
		}

		return new MusicBrainzRelease(
			Id: r.Id,
			r.Title ?? "",
			Artist: r.ArtistCredit?.FirstOrDefault()?.Artist?.Name,
			FormatArtistCredit(credits: r.ArtistCredit),
			r.Date?.NearestDate is DateTime dt ? DateOnly.FromDateTime(dateTime: dt) : null,
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
			ExtractNames(items: r.Tags, t => t.Name),
			ExtractNames(items: r.Genres, g => g.Name),
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
			a.LifeSpan?.Begin?.NearestDate is DateTime b
				? DateOnly.FromDateTime(dateTime: b)
				: null,
			a.LifeSpan?.End?.NearestDate is DateTime e ? DateOnly.FromDateTime(dateTime: e) : null,
			Ended: a.LifeSpan?.Ended,
			ExtractNames(items: a.Aliases, al => al.Name),
			ExtractNames(items: a.Tags, t => t.Name),
			ExtractNames(items: a.Genres, g => g.Name),
			Annotation: a.Annotation,
			(double?)a.Rating?.Value,
			RatingVotes: a.Rating?.VoteCount
		);

	internal static MusicBrainzRecording MapRecording(IRecording r)
	{
		IRelationship? workRelationship = r.Relationships?.FirstOrDefault(rel => rel.Work is { });
		var workName = workRelationship?.Work?.Title;
		Guid? workId = workRelationship?.Work?.Id;

		string? conductor = null;
		string? orchestra = null;
		string? recordingVenue = null;
		DateOnly? recordingDate = null;

		if (r.Relationships is { } relationships)
		{
			foreach (IRelationship rel in relationships)
			{
				var relType = rel.Type?.ToLowerInvariant();
				if (relType is null)
					continue;

				TryExtractConductor(
					rel: rel,
					relType: relType,
					conductor: ref conductor,
					recordingDate: ref recordingDate
				);
				TryExtractOrchestra(rel: rel, relType: relType, orchestra: ref orchestra);
				TryExtractVenue(
					rel: rel,
					relType: relType,
					recordingVenue: ref recordingVenue,
					recordingDate: ref recordingDate
				);
			}
		}

		return new MusicBrainzRecording(
			Id: r.Id,
			r.Title ?? "",
			Artist: r.ArtistCredit?.FirstOrDefault()?.Artist?.Name,
			FormatArtistCredit(credits: r.ArtistCredit),
			Length: r.Length,
			r.FirstReleaseDate?.NearestDate is DateTime dt
				? DateOnly.FromDateTime(dateTime: dt)
				: null,
			IsVideo: r.Video,
			Disambiguation: r.Disambiguation,
			r.Isrcs?.ToList() ?? [],
			ExtractNames(items: r.Tags, t => t.Name),
			ExtractNames(items: r.Genres, g => g.Name),
			(double?)r.Rating?.Value,
			RatingVotes: r.Rating?.VoteCount,
			Annotation: r.Annotation,
			WorkName: workName,
			WorkId: workId,
			WorkComposer: null,
			Conductor: conductor,
			Orchestra: orchestra,
			RecordingVenue: recordingVenue,
			RecordingDate: recordingDate
		);
	}

	private static void TryExtractConductor(
		IRelationship rel,
		string relType,
		ref string? conductor,
		ref DateOnly? recordingDate
	)
	{
		if (
			relType.EqualsIgnoreCase(other: "conductor", comparisonType: Ordinal)
			&& rel.Artist is { } conductorArtist
		)
		{
			conductor = conductorArtist.Name;
			if (recordingDate is null && rel.Begin?.NearestDate is DateTime beginDate)
				recordingDate = DateOnly.FromDateTime(dateTime: beginDate);
		}
	}

	private static void TryExtractOrchestra(
		IRelationship rel,
		string relType,
		ref string? orchestra
	)
	{
		if (
			(
				relType
					is "orchestra"
						or "performing orchestra"
						or "ensemble"
						or "choir"
						or "philharmonic"
				|| relType.EqualsIgnoreCase(other: "instrument", comparisonType: Ordinal)
					&& rel.Artist?.Name is { } name
					&& (
						name.ContainsIgnoreCase(substring: "Orchestra")
						|| name.ContainsIgnoreCase(substring: "Philharmonic")
						|| name.ContainsIgnoreCase(substring: "Symphony")
						|| name.ContainsIgnoreCase(substring: "Choir")
					)
			) && rel.Artist is { } orchestraArtist
		)
			orchestra = orchestraArtist.Name;
	}

	private static void TryExtractVenue(
		IRelationship rel,
		string relType,
		ref string? recordingVenue,
		ref DateOnly? recordingDate
	)
	{
		if (relType is "recorded at" or "recorded in" && rel.Place is { } place)
		{
			recordingVenue = place.Name;
			if (recordingDate is null && rel.Begin?.NearestDate is DateTime beginDate)
				recordingDate = DateOnly.FromDateTime(dateTime: beginDate);
		}
	}

	internal static MusicBrainzRecording MapRecordingFromSearch(IRecording r) =>
		new(
			Id: r.Id,
			r.Title ?? "",
			Artist: r.ArtistCredit?.FirstOrDefault()?.Artist?.Name,
			FormatArtistCredit(credits: r.ArtistCredit),
			Length: r.Length,
			r.FirstReleaseDate?.NearestDate is DateTime dt
				? DateOnly.FromDateTime(dateTime: dt)
				: null,
			IsVideo: r.Video,
			Disambiguation: r.Disambiguation,
			r.Isrcs?.ToList() ?? [],
			[],
			[],
			Rating: null,
			RatingVotes: null,
			Annotation: null
		);

	internal static string? FormatArtistCredit(IReadOnlyList<INameCredit>? credits)
	{
		if (credits is null || credits.Count == 0)
			return null;

		StringBuilder sb = new(credits.Count * 30);
		foreach (INameCredit c in credits)
			sb.Append(c.Name ?? c.Artist?.Name ?? "").Append(c.JoinPhrase ?? "");
		return sb.ToString();
	}
}
