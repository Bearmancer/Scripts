using ParkSquare.Discogs.Dto;
using Riok.Mapperly.Abstractions;
using Version = ParkSquare.Discogs.Dto.Version;

namespace CSharpScripts.Services.Music;

[Mapper]
internal static partial class DiscogsMapper
{
	internal static string? ExtractArtist(string? title)
	{
		if (title is null)
			return null;

		ReadOnlySpan<char> span = title.AsSpan();
		ReadOnlySpan<char> sep = " - ".AsSpan();
		var idx = span.IndexOf(sep);
		return idx >= 0 ? span[..idx].Trim().ToString() : null;
	}

	internal static int? ParseYear(string? year) => int.TryParse(year, out var y) ? y : null;

	internal static TimeSpan? ParseDuration(string? duration) =>
		TimeSpan.TryParse(duration, out TimeSpan result) ? result : null;

	internal static DiscogsArtistRef MapArtistRef(Artist a) =>
		new(a.Id, a.Name ?? "", a.Alias, a.Join, a.Role, a.Tracks, a.ResourceUrl);

	internal static DiscogsImage MapImage(Image i) =>
		new(i.Type ?? "", i.Uri, i.UriSmall, i.Width, i.Height, i.ResourceUrl);

	internal static partial DiscogsLabel MapLabel(Label l);

	internal static partial DiscogsCompany MapCompany(Company c);

	internal static partial DiscogsVideo MapVideo(DiscogsVideoDto v);

	internal static partial DiscogsIdentifier MapIdentifier(Identifier i);

	internal static DiscogsFormat MapFormat(Format f) =>
		new(f.Name ?? "", f.Quantity, f.Text, f.Descriptions?.ToList() ?? []);

	internal static DiscogsTrack MapTrack(Tracklist t) =>
		new(
			t.Position ?? "",
			t.Title ?? "",
			t.Duration,
			t.Type,
			t.Artists?.Select(MapArtistRef).ToList(),
			t.ExtraArtists?.Select(MapArtistRef).ToList()
		);

	internal static DiscogsCommunity MapCommunity(Community c) =>
		new(
			c.Have,
			c.Want,
			c.Rating?.Average,
			c.Rating?.Count,
			c.Status,
			c.DataQuality,
			c.Submitter is { } s ? new DiscogsSubmitter(s.Username ?? "", s.ResourceUrl) : null
		);

	internal static DiscogsSearchResult MapSearchResult(ParkSquare.Discogs.Dto.SearchResult r) =>
		new(
			r.ReleaseId,
			r.MasterId,
			r.Title,
			ExtractArtist(r.Title),
			ParseYear(r.Year),
			r.Country,
			r.Format is { } fmt ? Join(", ", fmt) : null,
			r.Label is { } lbl ? Join(", ", lbl) : null,
			r.CatalogNumber,
			r.Type,
			r.Thumb,
			r.CoverImage,
			r.Genre?.ToList(),
			r.Style?.ToList(),
			r.Barcode?.ToList()
		);

	internal static DiscogsRelease MapRelease(Release r)
	{
		Log.Debug("MapRelease entry {@ReleaseId}", r.ReleaseId);
		return new DiscogsRelease(
			r.ReleaseId,
			r.Title ?? "",
			r.Year,
			r.Country,
			r.Released,
			r.ReleasedFormatted,
			r.MasterId,
			r.MasterUrl,
			r.Status,
			r.DataQuality,
			r.Notes,
			r.Uri,
			r.ResourceUrl,
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
			r.EstimatedWeight
		);
	}

	internal static DiscogsMaster MapMaster(MasterRelease m) =>
		new(
			m.MasterId,
			m.Title ?? "",
			m.Year,
			m.MainReleaseId,
			m.MostRecentReleaseId,
			m.MainReleaseUrl,
			m.MostRecentReleaseUrl,
			m.VersionsUrl,
			m.ResourceUrl,
			m.Uri,
			m.DataQuality,
			m.Artists?.Select(MapArtistRef).ToList() ?? [],
			m.Genres?.ToList() ?? [],
			m.Styles?.ToList() ?? [],
			m.Tracklist?.Select(MapTrack).ToList() ?? [],
			m.Images?.Select(MapImage).ToList() ?? [],
			m.Videos?.Select(MapVideo).ToList() ?? [],
			m.QuantityForSale,
			(decimal?)m.LowestPrice
		);

	internal static DiscogsVersion MapVersion(Version v) =>
		new(
			v.ReleaseId,
			v.Title ?? "",
			v.Format,
			v.Label,
			v.Country,
			ParseYear(v.ReleaseYear),
			v.CatalogNumber,
			v.Status,
			v.ResourceUrl,
			v.Thumb
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
			Match discRangeMatch = DiscRangeRegexField.Match(line);
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
		Match recordedMatch = RecordedYearRegexField.Match(line);
		if (recordedMatch.Success && int.TryParse(recordedMatch.Groups[1].Value, out var y1))
			return y1;

		Match yearMatch = YearRegexField.Match(line);
		if (yearMatch.Success && int.TryParse(yearMatch.Groups[1].Value, out var y2))
			return y2;

		return null;
	}

	internal static string? ExtractVenueFromLine(string line)
	{
		Match venueMatch = VenueRegexField.Match(line);
		if (venueMatch.Success)
			return venueMatch.Groups[1].Value.Trim();

		Match commaVenueMatch = CommaVenueRegexField.Match(line);
		if (commaVenueMatch.Success)
			return commaVenueMatch.Groups[1].Value.Trim();

		return null;
	}

	private static readonly Regex DiscRangeRegexField = new(
		@"^(?:CD|Disc)\s*(\d+)(?:\s*[-–]\s*(\d+))?:",
		RegexOptions.IgnoreCase | RegexOptions.Compiled
	);

	private static readonly Regex RecordedYearRegexField = new(
		@"[Rr]ecorded\s+(?:\w+\s+)?(\d{4})",
		RegexOptions.IgnoreCase | RegexOptions.Compiled
	);

	private static readonly Regex YearRegexField = new(
		@"\b(19\d{2}|20\d{2})\b",
		RegexOptions.Compiled
	);

	private static readonly Regex VenueRegexField = new(
		@"(?:@|at|in)\s+([A-Z][^,\.\n]+(?:,\s*[A-Z][^,\.\n]+)?)",
		RegexOptions.IgnoreCase | RegexOptions.Compiled
	);

	private static readonly Regex CommaVenueRegexField = new(
		@"\d{4},\s*([A-Z][^,\.\n]+(?:,\s*[A-Z][^,\.\n]+)?)",
		RegexOptions.Compiled
	);
}



