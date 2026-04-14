using System.Text;

namespace CSharpScripts.Services.Music;

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
		StringComparer.OrdinalIgnoreCase
	);

	internal static readonly FrozenSet<string> ConductorRoles = FrozenSet.ToFrozenSet(
		["conductor", "director"],
		StringComparer.OrdinalIgnoreCase
	);

	internal static readonly FrozenSet<string> OrchestraRoles = FrozenSet.ToFrozenSet(
		["orchestra", "performing orchestra", "ensemble", "performer", "choir", "philharmonic"],
		StringComparer.OrdinalIgnoreCase
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
		StringComparer.OrdinalIgnoreCase
	);

	private static List<string> ExtractNames<T>(IEnumerable<T>? items, Func<T, string?> selector)
	{
		if (items is null)
			return [];
		List<string> result = [];
		foreach (T item in items)
		{
			var name = selector(item);
			if (!IsNullOrEmpty(name))
				result.Add(name);
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
								track.Id,
								track.Title ?? track.Recording?.Title ?? "",
								track.Position ?? 0,
								track.Number,
								track.Length,
								track.Recording?.Id,
								FormatArtistCredit(track.ArtistCredit)
							)
						);
					}
				}

				media.Add(
					new MusicBrainzMedium(
						medium.Position,
						medium.Format,
						medium.Title,
						medium.TrackCount,
						tracks
					)
				);
			}
		}

		List<MusicBrainzCredit> credits = [];
		if (r.Relationships is { } relationships)
		{
			foreach (IRelationship rel in relationships)
			{
				if (rel.Artist is { } artist && !IsNullOrEmpty(rel.Type))
				{
					credits.Add(
						new MusicBrainzCredit(
							artist.Name ?? "",
							rel.Type,
							artist.Id,
							rel.Attributes is { } attrs ? Join(", ", attrs) : null
						)
					);
				}
			}
		}

		List<MusicBrainzLabel> labels = [];
		if (r.LabelInfo is { } labelInfo)
		{
			foreach (ILabelInfo li in labelInfo)
			{
				labels.Add(new MusicBrainzLabel(li.Label?.Id, li.Label?.Name, li.CatalogNumber));
			}
		}

		return new MusicBrainzRelease(
			r.Id,
			r.Title ?? "",
			r.ArtistCredit?.FirstOrDefault()?.Artist?.Name,
			FormatArtistCredit(r.ArtistCredit),
			r.Date?.NearestDate is DateTime dt ? DateOnly.FromDateTime(dt) : null,
			r.Country,
			r.Status,
			r.Barcode,
			r.Asin,
			r.Quality,
			r.Packaging,
			r.Disambiguation,
			r.ReleaseGroup?.Id,
			r.ReleaseGroup?.Title,
			r.ReleaseGroup?.PrimaryType,
			media,
			credits,
			labels,
			ExtractNames(r.Tags, t => t.Name),
			ExtractNames(r.Genres, g => g.Name),
			r.Annotation
		);
	}

	internal static MusicBrainzArtist MapArtist(IArtist a) =>
		new(
			a.Id,
			a.Name ?? "",
			a.SortName,
			a.Type,
			a.Gender,
			a.Country,
			a.Area?.Name,
			a.Disambiguation,
			a.LifeSpan?.Begin?.NearestDate is DateTime b ? DateOnly.FromDateTime(b) : null,
			a.LifeSpan?.End?.NearestDate is DateTime e ? DateOnly.FromDateTime(e) : null,
			a.LifeSpan?.Ended,
			ExtractNames(a.Aliases, al => al.Name),
			ExtractNames(a.Tags, t => t.Name),
			ExtractNames(a.Genres, g => g.Name),
			a.Annotation,
			(double?)a.Rating?.Value,
			a.Rating?.VoteCount
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
		{
			foreach (IRelationship rel in relationships)
			{
				var relType = rel.Type?.ToLowerInvariant();
				if (relType is null)
					continue;

				TryExtractConductor(rel, relType, ref conductor, ref recordingDate);
				TryExtractOrchestra(rel, relType, ref orchestra);
				TryExtractVenue(rel, relType, ref recordingVenue, ref recordingDate);
			}
		}

		return new MusicBrainzRecording(
			r.Id,
			r.Title ?? "",
			r.ArtistCredit?.FirstOrDefault()?.Artist?.Name,
			FormatArtistCredit(r.ArtistCredit),
			r.Length,
			r.FirstReleaseDate?.NearestDate is DateTime dt ? DateOnly.FromDateTime(dt) : null,
			r.Video,
			r.Disambiguation,
			r.Isrcs?.ToList() ?? [],
			ExtractNames(r.Tags, t => t.Name),
			ExtractNames(r.Genres, g => g.Name),
			(double?)r.Rating?.Value,
			r.Rating?.VoteCount,
			r.Annotation,
			workName,
			workId,
			null,
			conductor,
			orchestra,
			recordingVenue,
			recordingDate
		);
	}

	private static void TryExtractConductor(
		IRelationship rel,
		string relType,
		ref string? conductor,
		ref DateOnly? recordingDate
	)
	{
		if (relType.EqualsIgnoreCase("conductor", Ordinal) && rel.Artist is { } conductorArtist)
		{
			conductor = conductorArtist.Name;
			if (recordingDate is null && rel.Begin?.NearestDate is DateTime beginDate)
				recordingDate = DateOnly.FromDateTime(beginDate);
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
				|| (
					relType.EqualsIgnoreCase("instrument", Ordinal)
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
				recordingDate = DateOnly.FromDateTime(beginDate);
		}
	}

	internal static MusicBrainzRecording MapRecordingFromSearch(IRecording r) =>
		new(
			r.Id,
			r.Title ?? "",
			r.ArtistCredit?.FirstOrDefault()?.Artist?.Name,
			FormatArtistCredit(r.ArtistCredit),
			r.Length,
			r.FirstReleaseDate?.NearestDate is DateTime dt ? DateOnly.FromDateTime(dt) : null,
			r.Video,
			r.Disambiguation,
			r.Isrcs?.ToList() ?? [],
			[],
			[],
			null,
			null,
			null
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
