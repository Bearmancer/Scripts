using Riok.Mapperly.Abstractions;

namespace CSharpScripts.Services.Music;

[Mapper]
internal static partial class DiscogsMapper
{
	// ─── User-defined type conversion ────────────────────────────────────────

	private static string NullToEmpty(string? s) => s ?? "";

	internal static string? ExtractArtist(string? title) =>
		title?.Contains(" - ") == true ? title.Split(" - ")[0].Trim() : null;

	internal static int? ParseYear(string? year) => int.TryParse(year, out var y) ? y : null;

	internal static TimeSpan? ParseDuration(string? duration) =>
		TimeSpan.TryParse(duration, out TimeSpan result) ? result : null;

	// ─── Manual mappings (property renames require non-generated implementations) ─

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

	// ─── Mapperly-generated mappings ─────────────────────────────────────────

	internal static partial DiscogsLabel MapLabel(Label l);

	internal static partial DiscogsCompany MapCompany(Company c);

	internal static partial DiscogsVideo MapVideo(DiscogsVideoDto v);

	internal static partial DiscogsIdentifier MapIdentifier(Identifier i);

	// ─── Complex manual mappings ──────────────────────────────────────────────

	internal static DiscogsFormat MapFormat(Format f) =>
		new(f.Name ?? "", Quantity: f.Quantity, Text: f.Text, f.Descriptions?.ToList() ?? []);

	internal static DiscogsTrack MapTrack(Tracklist t) =>
		new(
			t.Position ?? "",
			t.Title ?? "",
			Duration: t.Duration,
			Type: t.Type,
			t.Artists?.Select(MapArtistRef).ToList(),
			t.ExtraArtists?.Select(MapArtistRef).ToList()
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
		Log.Debug("MapRelease entry {@ReleaseId}", r.ReleaseId);
		return new(
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
			r.Artists?.Select(MapArtistRef).ToList() ?? [],
			r.ExtraArtists?.Select(MapArtistRef).ToList() ?? [],
			r.Labels?.Select(MapLabel).ToList() ?? [],
			r.Companies?.Select(MapCompany).ToList() ?? [],
			r.Genres?.ToList() ?? [],
			r.Styles?.ToList() ?? [],
			r.Tracklist?.Select(MapTrack).ToList() ?? [],
			r.Formats?.Select(MapFormat).ToList() ?? [],
			r.Identifiers?.Select(MapIdentifier).ToList() ?? [],
			r.Images?.Select(MapImage).ToList() ?? [],
			r.Videos?.Select(MapVideo).ToList() ?? [],
			r.Community is { } c ? MapCommunity(c) : null,
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
			m.Artists?.Select(MapArtistRef).ToList() ?? [],
			m.Genres?.ToList() ?? [],
			m.Styles?.ToList() ?? [],
			m.Tracklist?.Select(MapTrack).ToList() ?? [],
			m.Images?.Select(MapImage).ToList() ?? [],
			m.Videos?.Select(MapVideo).ToList() ?? [],
			QuantityForSale: m.QuantityForSale,
			(decimal?)m.LowestPrice
		);

	internal static DiscogsVersion MapVersion(ParkSquare.Discogs.Dto.Version v) =>
		new(
			Id: v.ReleaseId,
			v.Title ?? "",
			Format: v.Format,
			Label: v.Label,
			Country: v.Country,
			ParseYear(v.ReleaseYear),
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
		if (IsNullOrWhiteSpace(notes))
			return (null, null);

		int? year = null;
		string? venue = null;

		var lines = notes.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);

		foreach (var rawLine in lines)
		{
			var line = rawLine.Trim();

			var appliesToDisc = true;
			Match discRangeMatch = Regex.Match(
				line,
				@"^(?:CD|Disc)\s*(\d+)(?:\s*[-–]\s*(\d+))?:",
				RegexOptions.IgnoreCase
			);
			if (discRangeMatch.Success)
			{
				var startDisc = int.Parse(discRangeMatch.Groups[1].Value);
				var endDisc = discRangeMatch.Groups[2].Success
					? int.Parse(discRangeMatch.Groups[2].Value)
					: startDisc;
				appliesToDisc = discNumber >= startDisc && discNumber <= endDisc;
			}

			if (!appliesToDisc)
				continue;

			year ??= ExtractYearFromLine(line);

			venue ??= ExtractVenueFromLine(line);
		}

		return (year, venue);
	}

	internal static int? ExtractYearFromLine(string line)
	{
		Match recordedMatch = Regex.Match(
			line,
			@"[Rr]ecorded\s+(?:\w+\s+)?(\d{4})",
			RegexOptions.IgnoreCase
		);
		if (recordedMatch.Success && int.TryParse(recordedMatch.Groups[1].Value, out var y1))
			return y1;

		Match yearMatch = Regex.Match(line, @"\b(19\d{2}|20\d{2})\b");
		if (yearMatch.Success && int.TryParse(yearMatch.Groups[1].Value, out var y2))
			return y2;

		return null;
	}

	internal static string? ExtractVenueFromLine(string line)
	{
		Match venueMatch = Regex.Match(
			input: line,
			pattern: @"(?:@|at|in)\s+([A-Z][^,\.\n]+(?:,\s*[A-Z][^,\.\n]+)?)",
			options: RegexOptions.IgnoreCase
		);
		if (venueMatch.Success)
			return venueMatch.Groups[groupnum: 1].Value.Trim();

		Match commaVenueMatch = Regex.Match(
			input: line,
			pattern: @"\d{4},\s*([A-Z][^,\.\n]+(?:,\s*[A-Z][^,\.\n]+)?)",
			options: RegexOptions.None
		);
		if (commaVenueMatch.Success)
			return commaVenueMatch.Groups[groupnum: 1].Value.Trim();

		return null;
	}
}
