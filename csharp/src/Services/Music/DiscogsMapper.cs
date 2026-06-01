using ParkSquare.Discogs.Dto;
using Riok.Mapperly.Abstractions;
using Version = ParkSquare.Discogs.Dto.Version;

namespace Scripts.Services.Music;

[Mapper]
internal static partial class DiscogsMapper
{
	private static readonly Regex DiscRangeRegexField = new(
		pattern: @"^(?:CD|Disc)\s*(\d+)(?:\s*[-–]\s*(\d+))?:",
		RegexOptions.IgnoreCase | RegexOptions.Compiled
	);

	private static readonly Regex RecordedYearRegexField = new(
		pattern: @"[Rr]ecorded\s+(?:\w+\s+)?(\d{4})",
		RegexOptions.IgnoreCase | RegexOptions.Compiled
	);

	private static readonly Regex YearRegexField = new(
		pattern: @"\b(19\d{2}|20\d{2})\b",
		options: RegexOptions.Compiled
	);

	private static readonly Regex VenueRegexField = new(
		pattern: @"(?:@|at|in)\s+([A-Z][^,\.\n]+(?:,\s*[A-Z][^,\.\n]+)?)",
		RegexOptions.IgnoreCase | RegexOptions.Compiled
	);

	private static readonly Regex CommaVenueRegexField = new(
		pattern: @"\d{4},\s*([A-Z][^,\.\n]+(?:,\s*[A-Z][^,\.\n]+)?)",
		options: RegexOptions.Compiled
	);

	internal static string? ExtractArtist(string? title)
	{
		if (title is null)
			return null;

		ReadOnlySpan<char> span = title.AsSpan();
		ReadOnlySpan<char> sep = " - ".AsSpan();
		var idx = span.IndexOf(value: sep);
		return idx >= 0 ? span[..idx].Trim().ToString() : null;
	}

	internal static int? ParseYear(string? year) => int.TryParse(s: year, out var y) ? y : null;

	internal static TimeSpan? ParseDuration(string? duration) =>
		TimeSpan.TryParse(s: duration, out TimeSpan result) ? result : null;

	internal static DiscogsArtistRef MapArtistRef(Artist a) =>
		new(
			Id: a.Id,
			a.Name ?? "",
			Anv: a.Alias,
			Join: a.Join,
			Role: a.Role,
			Tracks: a.Tracks,
			ResourceUrl: a.ResourceUrl
		);

	internal static DiscogsImage MapImage(Image i) =>
		new(
			i.Type ?? "",
			Uri: i.Uri,
			Uri150: i.UriSmall,
			Width: i.Width,
			Height: i.Height,
			ResourceUrl: i.ResourceUrl
		);

	internal static partial DiscogsLabel MapLabel(Label l);

	internal static partial DiscogsCompany MapCompany(Company c);

	internal static partial DiscogsVideo MapVideo(DiscogsVideoDto v);

	internal static partial DiscogsIdentifier MapIdentifier(Identifier i);

	internal static DiscogsFormat MapFormat(Format f) =>
		new(f.Name ?? "", Quantity: f.Quantity, Text: f.Text, f.Descriptions?.ToList() ?? []);

	internal static DiscogsTrack MapTrack(Tracklist t) =>
		new(
			t.Position ?? "",
			t.Title ?? "",
			Duration: t.Duration,
			Type: t.Type,
			t.Artists?.Select(selector: MapArtistRef).ToList(),
			t.ExtraArtists?.Select(selector: MapArtistRef).ToList()
		);

	internal static DiscogsCommunity MapCommunity(Community c) =>
		new(
			Have: c.Have,
			Want: c.Want,
			Rating: c.Rating?.Average,
			RatingCount: c.Rating?.Count,
			Status: c.Status,
			DataQuality: c.DataQuality,
			c.Submitter is { } s
				? new DiscogsSubmitter(s.Username ?? "", ResourceUrl: s.ResourceUrl)
				: null
		);

	internal static DiscogsSearchResult MapSearchResult(ParkSquare.Discogs.Dto.SearchResult r) =>
		new(
			ReleaseId: r.ReleaseId,
			MasterId: r.MasterId,
			Title: r.Title,
			ExtractArtist(title: r.Title),
			ParseYear(year: r.Year),
			Country: r.Country,
			r.Format is { } fmt ? Join(separator: ", ", values: fmt) : null,
			r.Label is { } lbl ? Join(separator: ", ", values: lbl) : null,
			CatalogNumber: r.CatalogNumber,
			Type: r.Type,
			Thumb: r.Thumb,
			CoverImage: r.CoverImage,
			r.Genre?.ToList(),
			r.Style?.ToList(),
			r.Barcode?.ToList()
		);

	internal static DiscogsRelease MapRelease(Release r)
	{
		Log.Debug(messageTemplate: "MapRelease entry {@ReleaseId}", r.ReleaseId);
		return new DiscogsRelease(
			Id: r.ReleaseId,
			r.Title ?? "",
			Year: r.Year,
			Country: r.Country,
			Released: r.Released,
			ReleasedFormatted: r.ReleasedFormatted,
			MasterId: r.MasterId,
			MasterUrl: r.MasterUrl,
			Status: r.Status,
			DataQuality: r.DataQuality,
			Notes: r.Notes,
			Uri: r.Uri,
			ResourceUrl: r.ResourceUrl,
			r.Artists?.Select(selector: MapArtistRef).ToList() ?? [],
			r.ExtraArtists?.Select(selector: MapArtistRef).ToList() ?? [],
			r.Labels?.Select(selector: MapLabel).ToList() ?? [],
			r.Companies?.Select(selector: MapCompany).ToList() ?? [],
			r.Genres?.ToList() ?? [],
			r.Styles?.ToList() ?? [],
			r.Tracklist?.Select(selector: MapTrack).ToList() ?? [],
			r.Formats?.Select(selector: MapFormat).ToList() ?? [],
			r.Identifiers?.Select(selector: MapIdentifier).ToList() ?? [],
			r.Images?.Select(selector: MapImage).ToList() ?? [],
			r.Videos?.Select(selector: MapVideo).ToList() ?? [],
			r.Community is { } c ? MapCommunity(c: c) : null,
			EstimatedWeight: r.EstimatedWeight
		);
	}

	internal static DiscogsMaster MapMaster(MasterRelease m) =>
		new(
			Id: m.MasterId,
			m.Title ?? "",
			Year: m.Year,
			MainReleaseId: m.MainReleaseId,
			MostRecentReleaseId: m.MostRecentReleaseId,
			MainReleaseUrl: m.MainReleaseUrl,
			MostRecentReleaseUrl: m.MostRecentReleaseUrl,
			VersionsUrl: m.VersionsUrl,
			ResourceUrl: m.ResourceUrl,
			Uri: m.Uri,
			DataQuality: m.DataQuality,
			m.Artists?.Select(selector: MapArtistRef).ToList() ?? [],
			m.Genres?.ToList() ?? [],
			m.Styles?.ToList() ?? [],
			m.Tracklist?.Select(selector: MapTrack).ToList() ?? [],
			m.Images?.Select(selector: MapImage).ToList() ?? [],
			m.Videos?.Select(selector: MapVideo).ToList() ?? [],
			QuantityForSale: m.QuantityForSale,
			(decimal?)m.LowestPrice
		);

	internal static DiscogsVersion MapVersion(Version v) =>
		new(
			Id: v.ReleaseId,
			v.Title ?? "",
			Format: v.Format,
			Label: v.Label,
			Country: v.Country,
			ParseYear(year: v.ReleaseYear),
			CatalogNumber: v.CatalogNumber,
			Status: v.Status,
			ResourceUrl: v.ResourceUrl,
			Thumb: v.Thumb
		);

	internal static (int? Year, string? Venue) ParseNotesForRecordingInfo(
		string? notes,
		int discNumber
	)
	{
		if (IsNullOrWhiteSpace(value: notes))
			return (null, null);

		int? year = null;
		string? venue = null;

		var lines = notes.Split(['\n', '\r'], options: StringSplitOptions.RemoveEmptyEntries);

		foreach (var rawLine in lines)
		{
			var line = rawLine.Trim();

			var appliesToDisc = true;
			Match discRangeMatch = DiscRangeRegexField.Match(input: line);
			if (discRangeMatch.Success)
			{
				var startDisc = int.Parse(s: discRangeMatch.Groups[groupnum: 1].Value);
				var endDisc = discRangeMatch.Groups[groupnum: 2].Success
					? int.Parse(s: discRangeMatch.Groups[groupnum: 2].Value)
					: startDisc;
				appliesToDisc = discNumber >= startDisc && discNumber <= endDisc;
			}

			if (!appliesToDisc)
				continue;

			year ??= ExtractYearFromLine(line: line);

			venue ??= ExtractVenueFromLine(line: line);
		}

		return (year, venue);
	}

	internal static int? ExtractYearFromLine(string line)
	{
		Match recordedMatch = RecordedYearRegexField.Match(input: line);
		if (
			recordedMatch.Success
			&& int.TryParse(s: recordedMatch.Groups[groupnum: 1].Value, out var y1)
		)
			return y1;

		Match yearMatch = YearRegexField.Match(input: line);
		if (yearMatch.Success && int.TryParse(s: yearMatch.Groups[groupnum: 1].Value, out var y2))
			return y2;

		return null;
	}

	internal static string? ExtractVenueFromLine(string line)
	{
		Match venueMatch = VenueRegexField.Match(input: line);
		if (venueMatch.Success)
			return venueMatch.Groups[groupnum: 1].Value.Trim();

		Match commaVenueMatch = CommaVenueRegexField.Match(input: line);
		if (commaVenueMatch.Success)
			return commaVenueMatch.Groups[groupnum: 1].Value.Trim();

		return null;
	}
}
