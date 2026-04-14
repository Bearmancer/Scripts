using CSharpScripts.Models;
using CSharpScripts.Services.Music;
using CSharpScripts.Tests.Music.TestData;
using MetaBrainz.MusicBrainz.Interfaces.Entities;
using AssertionExtensions = FluentAssertions.AssertionExtensions;

namespace CSharpScripts.Tests.Music;

internal sealed class MusicBrainzMapperTests
{
	[Test]
	public void MapRelease_BoxSet_MapsAllMedia()
	{
		// Arrange
		IRelease release = MusicBrainzTestData.CreateBoxSetRelease(discCount: 80, tracksPerDisc: 5);

		// Act
		MusicBrainzRelease result = MusicBrainzMapper.MapRelease(r: release);

		// Assert
		AssertionExtensions.Should(result.Media).HaveCount(expected: 80);
	}

	[Test]
	public void MapRelease_BoxSet_MapsAllTracks()
	{
		// Arrange
		IRelease release = MusicBrainzTestData.CreateBoxSetRelease(discCount: 80, tracksPerDisc: 5);

		// Act
		MusicBrainzRelease result = MusicBrainzMapper.MapRelease(r: release);

		// Assert
		AssertionExtensions.Should(result.Tracks).HaveCount(expected: 400);
	}

	[Test]
	public void MapRelease_BoxSet_EachMediumHasCorrectTrackCount()
	{
		// Arrange
		IRelease release = MusicBrainzTestData.CreateBoxSetRelease(
			discCount: 10,
			tracksPerDisc: 12
		);

		// Act
		MusicBrainzRelease result = MusicBrainzMapper.MapRelease(r: release);

		// Assert
		AssertionExtensions
			.Should(result.Media)
			.AllSatisfy(m => AssertionExtensions.Should(m.Tracks).HaveCount(expected: 12));
	}

	[Test]
	public void MapRelease_ClassicalRelease_ExtractsConductor()
	{
		// Arrange
		IRelease release = MusicBrainzTestData.CreateClassicalRelease();

		// Act
		MusicBrainzRelease result = MusicBrainzMapper.MapRelease(r: release);

		// Assert
		AssertionExtensions.Should(result.Credits).ContainSingle(c => c.Role == "conductor");
	}

	[Test]
	public void MapRelease_ClassicalRelease_ConductorNameIsCorrect()
	{
		// Arrange
		IRelease release = MusicBrainzTestData.CreateClassicalRelease();

		// Act
		MusicBrainzRelease result = MusicBrainzMapper.MapRelease(r: release);

		// Assert
		AssertionExtensions
			.Should(Enumerable.First(result.Credits, c => c.Role == "conductor").Name)
			.Be(expected: "Herbert von Karajan");
	}

	[Test]
	public void MapRelease_ClassicalRelease_ExtractsOrchestra()
	{
		// Arrange
		IRelease release = MusicBrainzTestData.CreateClassicalRelease();

		// Act
		MusicBrainzRelease result = MusicBrainzMapper.MapRelease(r: release);

		// Assert
		AssertionExtensions.Should(result.Credits).ContainSingle(c => c.Role == "orchestra");
	}

	[Test]
	public void MapRelease_ClassicalRelease_OrchestraNameIsCorrect()
	{
		// Arrange
		IRelease release = MusicBrainzTestData.CreateClassicalRelease();

		// Act
		MusicBrainzRelease result = MusicBrainzMapper.MapRelease(r: release);

		// Assert
		AssertionExtensions
			.Should(Enumerable.First(result.Credits, c => c.Role == "orchestra").Name)
			.Be(expected: "Berlin Philharmonic");
	}

	[Test]
	public void MapRelease_NullSafety_DoesNotThrow()
	{
		// Arrange
		IRelease release = MusicBrainzTestData.CreateMinimalRelease();

		// Act
		void act() => MusicBrainzMapper.MapRelease(r: release);

		// Assert
		AssertionExtensions.Should(act).NotThrow();
	}

	[Test]
	public void MapRelease_NullSafety_ProducesEmptyMedia()
	{
		// Arrange
		IRelease release = MusicBrainzTestData.CreateMinimalRelease();

		// Act
		MusicBrainzRelease result = MusicBrainzMapper.MapRelease(r: release);

		// Assert
		AssertionExtensions.Should(result.Media).BeEmpty();
	}

	[Test]
	public void MapRelease_NullSafety_ProducesEmptyCredits()
	{
		// Arrange
		IRelease release = MusicBrainzTestData.CreateMinimalRelease();

		// Act
		MusicBrainzRelease result = MusicBrainzMapper.MapRelease(r: release);

		// Assert
		AssertionExtensions.Should(result.Credits).BeEmpty();
	}

	[Test]
	public void MapRelease_NullSafety_ProducesEmptyLabels()
	{
		// Arrange
		IRelease release = MusicBrainzTestData.CreateMinimalRelease();

		// Act
		MusicBrainzRelease result = MusicBrainzMapper.MapRelease(r: release);

		// Assert
		AssertionExtensions.Should(result.Labels).BeEmpty();
	}

	[Test]
	public void MapRelease_NullSafety_TitleDefaultsToEmpty()
	{
		// Arrange
		IRelease release = MusicBrainzTestData.CreateMinimalRelease();

		// Act
		MusicBrainzRelease result = MusicBrainzMapper.MapRelease(r: release);

		// Assert
		AssertionExtensions.Should(result.Title).BeEmpty();
	}

	[Test]
	public void MapRelease_PopRelease_MapsStatusAndCountry()
	{
		// Arrange
		IRelease release = MusicBrainzTestData.CreatePopRelease();

		// Act
		MusicBrainzRelease result = MusicBrainzMapper.MapRelease(r: release);

		// Assert
		AssertionExtensions.Should(result.Status).Be(expected: "Official");
		AssertionExtensions.Should(result.Country).Be(expected: "US");
	}

	[Test]
	public void MapRelease_PopRelease_MapsTitle()
	{
		// Arrange
		IRelease release = MusicBrainzTestData.CreatePopRelease();

		// Act
		MusicBrainzRelease result = MusicBrainzMapper.MapRelease(r: release);

		// Assert
		AssertionExtensions.Should(result.Title).Be(expected: "Thriller");
	}

	[Test]
	public void MapArtist_WithName_MapsNameCorrectly()
	{
		// Arrange
		IArtist artist = MusicBrainzTestData.CreateArtist(
			name: "David Bowie",
			sortName: "Bowie, David"
		);

		// Act
		MusicBrainzArtist result = MusicBrainzMapper.MapArtist(a: artist);

		// Assert
		AssertionExtensions.Should(result.Name).Be(expected: "David Bowie");
	}

	[Test]
	public void MapArtist_WithSortName_MapsSortNameCorrectly()
	{
		// Arrange
		IArtist artist = MusicBrainzTestData.CreateArtist(
			name: "David Bowie",
			sortName: "Bowie, David"
		);

		// Act
		MusicBrainzArtist result = MusicBrainzMapper.MapArtist(a: artist);

		// Assert
		AssertionExtensions.Should(result.SortName).Be(expected: "Bowie, David");
	}

	[Test]
	public void FormatArtistCredit_WithMultipleCreditsAndJoinPhrase_FormatsCorrectly()
	{
		// Arrange
		INameCredit credit1 = MusicBrainzTestData.CreateNameCredit(
			artistName: "Simon",
			joinPhrase: " & "
		);
		INameCredit credit2 = MusicBrainzTestData.CreateNameCredit(
			artistName: "Garfunkel",
			joinPhrase: null
		);
		List<INameCredit> credits = [credit1, credit2];

		// Act
		var result = MusicBrainzMapper.FormatArtistCredit(credits: credits);

		// Assert
		AssertionExtensions.Should(result).Be(expected: "Simon & Garfunkel");
	}

	[Test]
	public void FormatArtistCredit_WithSingleCredit_ReturnsName()
	{
		// Arrange
		INameCredit credit = MusicBrainzTestData.CreateNameCredit(
			artistName: "Beethoven",
			joinPhrase: null
		);
		List<INameCredit> credits = [credit];

		// Act
		var result = MusicBrainzMapper.FormatArtistCredit(credits: credits);

		// Assert
		AssertionExtensions.Should(result).Be(expected: "Beethoven");
	}

	[Test]
	public void FormatArtistCredit_WithNullCredits_ReturnsNull()
	{
		// Act
		var result = MusicBrainzMapper.FormatArtistCredit(credits: null);

		// Assert
		AssertionExtensions.Should(result).BeNull();
	}

	[Test]
	public void MapRecordingFromSearch_WithMultipleRecordings_MapsAllItems()
	{
		// Arrange
		List<IRecording> recordings = MusicBrainzTestData.CreateSearchResult(count: 25);

		// Act
		var results = Enumerable.ToList(
			recordings.Select(selector: MusicBrainzMapper.MapRecordingFromSearch)
		);

		// Assert
		AssertionExtensions.Should(results).HaveCount(expected: 25);
	}

	[Test]
	public void MapRecordingFromSearch_WithArtistName_MapsArtist()
	{
		// Arrange
		List<IRecording> recordings = MusicBrainzTestData.CreateSearchResult(count: 1);

		// Act
		MusicBrainzRecording result = MusicBrainzMapper.MapRecordingFromSearch(
			recordings[index: 0]
		);

		// Assert
		AssertionExtensions.Should(result.Artist).Be(expected: "Artist 1");
	}

	[Test]
	public void MapRecordingFromSearch_WithArtistName_MapsTitle()
	{
		// Arrange
		List<IRecording> recordings = MusicBrainzTestData.CreateSearchResult(count: 1);

		// Act
		MusicBrainzRecording result = MusicBrainzMapper.MapRecordingFromSearch(
			recordings[index: 0]
		);

		// Assert
		AssertionExtensions.Should(result.Title).Be(expected: "Recording 1");
	}

	[Test]
	public void MapRecordingFromSearch_PaginatedResults_FirstAndLastItemTitlesAreCorrect()
	{
		// Arrange
		List<IRecording> recordings = MusicBrainzTestData.CreateSearchResult(count: 50);

		// Act
		var results = Enumerable.ToList(
			recordings.Select(selector: MusicBrainzMapper.MapRecordingFromSearch)
		);

		// Assert
		AssertionExtensions.Should(results[index: 0].Title).Be(expected: "Recording 1");
		AssertionExtensions.Should(results[index: 49].Title).Be(expected: "Recording 50");
	}
}
