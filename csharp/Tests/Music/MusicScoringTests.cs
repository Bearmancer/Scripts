using CSharpScripts.Models;
using CSharpScripts.Services.Music;
using AssertionExtensions = FluentAssertions.AssertionExtensions;

namespace CSharpScripts.Tests.Music;

internal sealed class MusicScoringTests
{
	[Test]
	public void WhenQueryMatchesTitleExactlyThenReturnsPerfectScore()
	{
		SearchResult result = new(
			Source: MusicSource.Discogs,
			Id: "123",
			Title: "Heroes",
			Artist: "David Bowie",
			Year: 1977,
			Format: "Album",
			Label: "RCA",
			ReleaseType: "Album",
			Score: null,
			Country: "UK",
			CatalogNumber: null,
			Status: null,
			Disambiguation: null,
			Genres: null,
			Styles: null
		);

		var score = MusicScoringService.CalculateRelevanceScore(query: "Heroes", result: result);

		AssertionExtensions.Should(score).Be(expected: 100);
	}

	[Test]
	public void WhenQueryMatchesArtistAndTitleThenReturnsPerfectScore()
	{
		SearchResult result = new(
			Source: MusicSource.MusicBrainz,
			Id: "guid-123",
			Title: "Heroes",
			Artist: "David Bowie",
			Year: 1977,
			Format: null,
			Label: null,
			ReleaseType: "Album",
			Score: 95,
			Country: null,
			CatalogNumber: null,
			Status: null,
			Disambiguation: null,
			Genres: null,
			Styles: null
		);

		var score = MusicScoringService.CalculateRelevanceScore(
			query: "david bowie heroes",
			result: result
		);

		AssertionExtensions.Should(score).Be(expected: 100);
	}

	[Test]
	public void WhenQueryPartiallyMatchesThenReturnsPartialScore()
	{
		SearchResult result = new(
			Source: MusicSource.Discogs,
			Id: "456",
			Title: "The Rise and Fall of Ziggy Stardust",
			Artist: "David Bowie",
			Year: 1972,
			Format: "Album",
			Label: "RCA",
			ReleaseType: "Album",
			Score: null,
			Country: "UK",
			CatalogNumber: null,
			Status: null,
			Disambiguation: null,
			Genres: null,
			Styles: null
		);

		var score = MusicScoringService.CalculateRelevanceScore(
			query: "Ziggy Amsterdam",
			result: result
		);

		AssertionExtensions.Should(score).BeGreaterThan(expected: 0);
		AssertionExtensions.Should(score).BeLessThan(expected: 100);
	}

	[Test]
	public void WhenResultIsRecordingTypeThenIsTrackReturnsTrue()
	{
		SearchResult result = new(
			Source: MusicSource.MusicBrainz,
			Id: "recording-123",
			Title: "Heroes",
			Artist: "David Bowie",
			Year: 1977,
			Format: null,
			Label: null,
			ReleaseType: "recording",
			Score: null,
			Country: null,
			CatalogNumber: null,
			Status: null,
			Disambiguation: null,
			Genres: null,
			Styles: null
		);

		AssertionExtensions.Should(MusicScoringService.IsTrackResult(result: result)).BeTrue();
	}

	[Test]
	public void WhenResultIsAlbumTypeThenIsTrackReturnsFalse()
	{
		SearchResult result = new(
			Source: MusicSource.Discogs,
			Id: "789",
			Title: "Heroes",
			Artist: "David Bowie",
			Year: 1977,
			Format: "Album",
			Label: "RCA",
			ReleaseType: "album",
			Score: null,
			Country: "UK",
			CatalogNumber: null,
			Status: null,
			Disambiguation: null,
			Genres: null,
			Styles: null
		);

		AssertionExtensions.Should(MusicScoringService.IsTrackResult(result: result)).BeFalse();
	}

	[Test]
	public void WhenFilterIsAlbumAndTypeIsAlbumThenMatchesTypeReturnsTrue()
	{
		SearchResult result = new(
			Source: MusicSource.Discogs,
			Id: "100",
			Title: "Low",
			Artist: "David Bowie",
			Year: 1977,
			Format: "Album",
			Label: "RCA",
			ReleaseType: "album",
			Score: null,
			Country: "UK",
			CatalogNumber: null,
			Status: null,
			Disambiguation: null,
			Genres: null,
			Styles: null
		);

		AssertionExtensions
			.Should(MusicScoringService.MatchesType(result: result, filter: "album"))
			.BeTrue();
	}

	[Test]
	public void WhenFilterIsEpAndTypeIsSingleThenMatchesTypeReturnsFalse()
	{
		SearchResult result = new(
			Source: MusicSource.Discogs,
			Id: "200",
			Title: "Space Oddity",
			Artist: "David Bowie",
			Year: 1969,
			Format: "7\"",
			Label: "Philips",
			ReleaseType: "single",
			Score: null,
			Country: "UK",
			CatalogNumber: null,
			Status: null,
			Disambiguation: null,
			Genres: null,
			Styles: null
		);

		AssertionExtensions
			.Should(MusicScoringService.MatchesType(result: result, filter: "ep"))
			.BeFalse();
	}

	[Test]
	public void WhenFilterIsCompilationAndTypeIsCompilationThenMatchesTypeReturnsTrue()
	{
		SearchResult result = new(
			Source: MusicSource.Discogs,
			Id: "300",
			Title: "Best of Bowie",
			Artist: "David Bowie",
			Year: 2002,
			Format: "CD",
			Label: "EMI",
			ReleaseType: "compilation",
			Score: null,
			Country: "UK",
			CatalogNumber: null,
			Status: null,
			Disambiguation: null,
			Genres: null,
			Styles: null
		);

		AssertionExtensions
			.Should(MusicScoringService.MatchesType(result: result, filter: "compilation"))
			.BeTrue();
	}
}
