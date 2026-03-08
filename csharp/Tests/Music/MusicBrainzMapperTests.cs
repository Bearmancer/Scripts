using CSharpScripts.Models;
using CSharpScripts.Services.Music;
using CSharpScripts.Tests.Music.TestData;
using FluentAssertions;
using MetaBrainz.MusicBrainz.Interfaces.Entities;
using NSubstitute;

namespace CSharpScripts.Tests.Music;

internal sealed class MusicBrainzMapperTests
{
	[Test]
	public void MapRelease_BoxSet_MapsAllMedia()
	{
		// Arrange
		IRelease release = MusicBrainzTestData.CreateBoxSetRelease(discCount: 80, tracksPerDisc: 5);

		// Act
		MusicBrainzRelease result = MusicBrainzMapper.MapRelease(release);

		// Assert
		result.Media.Should().HaveCount(80);
	}

	[Test]
	public void MapRelease_BoxSet_MapsAllTracks()
	{
		// Arrange
		IRelease release = MusicBrainzTestData.CreateBoxSetRelease(discCount: 80, tracksPerDisc: 5);

		// Act
		MusicBrainzRelease result = MusicBrainzMapper.MapRelease(release);

		// Assert
		result.Tracks.Should().HaveCount(400);
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
		MusicBrainzRelease result = MusicBrainzMapper.MapRelease(release);

		// Assert
		result.Media.Should().AllSatisfy(m => m.Tracks.Should().HaveCount(12));
	}

	[Test]
	public void MapRelease_ClassicalRelease_ExtractsConductor()
	{
		// Arrange
		IRelease release = MusicBrainzTestData.CreateClassicalRelease();

		// Act
		MusicBrainzRelease result = MusicBrainzMapper.MapRelease(release);

		// Assert
		result.Credits.Should().ContainSingle(c => c.Role == "conductor");
	}

	[Test]
	public void MapRelease_ClassicalRelease_ConductorNameIsCorrect()
	{
		// Arrange
		IRelease release = MusicBrainzTestData.CreateClassicalRelease();

		// Act
		MusicBrainzRelease result = MusicBrainzMapper.MapRelease(release);

		// Assert
		result.Credits.First(c => c.Role == "conductor").Name.Should().Be("Herbert von Karajan");
	}

	[Test]
	public void MapRelease_ClassicalRelease_ExtractsOrchestra()
	{
		// Arrange
		IRelease release = MusicBrainzTestData.CreateClassicalRelease();

		// Act
		MusicBrainzRelease result = MusicBrainzMapper.MapRelease(release);

		// Assert
		result.Credits.Should().ContainSingle(c => c.Role == "orchestra");
	}

	[Test]
	public void MapRelease_ClassicalRelease_OrchestraNameIsCorrect()
	{
		// Arrange
		IRelease release = MusicBrainzTestData.CreateClassicalRelease();

		// Act
		MusicBrainzRelease result = MusicBrainzMapper.MapRelease(release);

		// Assert
		result.Credits.First(c => c.Role == "orchestra").Name.Should().Be("Berlin Philharmonic");
	}

	[Test]
	public void MapRelease_NullSafety_DoesNotThrow()
	{
		// Arrange
		IRelease release = MusicBrainzTestData.CreateMinimalRelease();

		// Act
		Action act = () => MusicBrainzMapper.MapRelease(release);

		// Assert
		act.Should().NotThrow();
	}

	[Test]
	public void MapRelease_NullSafety_ProducesEmptyMedia()
	{
		// Arrange
		IRelease release = MusicBrainzTestData.CreateMinimalRelease();

		// Act
		MusicBrainzRelease result = MusicBrainzMapper.MapRelease(release);

		// Assert
		result.Media.Should().BeEmpty();
	}

	[Test]
	public void MapRelease_NullSafety_ProducesEmptyCredits()
	{
		// Arrange
		IRelease release = MusicBrainzTestData.CreateMinimalRelease();

		// Act
		MusicBrainzRelease result = MusicBrainzMapper.MapRelease(release);

		// Assert
		result.Credits.Should().BeEmpty();
	}

	[Test]
	public void MapRelease_NullSafety_ProducesEmptyLabels()
	{
		// Arrange
		IRelease release = MusicBrainzTestData.CreateMinimalRelease();

		// Act
		MusicBrainzRelease result = MusicBrainzMapper.MapRelease(release);

		// Assert
		result.Labels.Should().BeEmpty();
	}

	[Test]
	public void MapRelease_NullSafety_TitleDefaultsToEmpty()
	{
		// Arrange
		IRelease release = MusicBrainzTestData.CreateMinimalRelease();

		// Act
		MusicBrainzRelease result = MusicBrainzMapper.MapRelease(release);

		// Assert
		result.Title.Should().BeEmpty();
	}

	[Test]
	public void MapRelease_PopRelease_MapsStatusAndCountry()
	{
		// Arrange
		IRelease release = MusicBrainzTestData.CreatePopRelease();

		// Act
		MusicBrainzRelease result = MusicBrainzMapper.MapRelease(release);

		// Assert
		result.Status.Should().Be("Official");
		result.Country.Should().Be("US");
	}

	[Test]
	public void MapRelease_PopRelease_MapsTitle()
	{
		// Arrange
		IRelease release = MusicBrainzTestData.CreatePopRelease();

		// Act
		MusicBrainzRelease result = MusicBrainzMapper.MapRelease(release);

		// Assert
		result.Title.Should().Be("Thriller");
	}

	[Test]
	public void MapArtist_WithName_MapsNameCorrectly()
	{
		// Arrange
		IArtist artist = MusicBrainzTestData.CreateArtist("David Bowie", sortName: "Bowie, David");

		// Act
		MusicBrainzArtist result = MusicBrainzMapper.MapArtist(artist);

		// Assert
		result.Name.Should().Be("David Bowie");
	}

	[Test]
	public void MapArtist_WithSortName_MapsSortNameCorrectly()
	{
		// Arrange
		IArtist artist = MusicBrainzTestData.CreateArtist("David Bowie", sortName: "Bowie, David");

		// Act
		MusicBrainzArtist result = MusicBrainzMapper.MapArtist(artist);

		// Assert
		result.SortName.Should().Be("Bowie, David");
	}

	[Test]
	public void FormatArtistCredit_WithMultipleCreditsAndJoinPhrase_FormatsCorrectly()
	{
		// Arrange
		INameCredit credit1 = MusicBrainzTestData.CreateNameCredit("Simon", joinPhrase: " & ");
		INameCredit credit2 = MusicBrainzTestData.CreateNameCredit("Garfunkel", joinPhrase: null);
		List<INameCredit> credits = [credit1, credit2];

		// Act
		string? result = MusicBrainzMapper.FormatArtistCredit(credits);

		// Assert
		result.Should().Be("Simon & Garfunkel");
	}

	[Test]
	public void FormatArtistCredit_WithSingleCredit_ReturnsName()
	{
		// Arrange
		INameCredit credit = MusicBrainzTestData.CreateNameCredit("Beethoven", joinPhrase: null);
		List<INameCredit> credits = [credit];

		// Act
		string? result = MusicBrainzMapper.FormatArtistCredit(credits);

		// Assert
		result.Should().Be("Beethoven");
	}

	[Test]
	public void FormatArtistCredit_WithNullCredits_ReturnsNull()
	{
		// Act
		string? result = MusicBrainzMapper.FormatArtistCredit(null);

		// Assert
		result.Should().BeNull();
	}

	[Test]
	public void MapRecordingFromSearch_WithMultipleRecordings_MapsAllItems()
	{
		// Arrange
		List<IRecording> recordings = MusicBrainzTestData.CreateSearchResult(count: 25);

		// Act
		List<MusicBrainzRecording> results = recordings
			.Select(MusicBrainzMapper.MapRecordingFromSearch)
			.ToList();

		// Assert
		results.Should().HaveCount(25);
	}

	[Test]
	public void MapRecordingFromSearch_WithArtistName_MapsArtist()
	{
		// Arrange
		List<IRecording> recordings = MusicBrainzTestData.CreateSearchResult(count: 1);

		// Act
		MusicBrainzRecording result = MusicBrainzMapper.MapRecordingFromSearch(recordings[0]);

		// Assert
		result.Artist.Should().Be("Artist 1");
	}

	[Test]
	public void MapRecordingFromSearch_WithArtistName_MapsTitle()
	{
		// Arrange
		List<IRecording> recordings = MusicBrainzTestData.CreateSearchResult(count: 1);

		// Act
		MusicBrainzRecording result = MusicBrainzMapper.MapRecordingFromSearch(recordings[0]);

		// Assert
		result.Title.Should().Be("Recording 1");
	}

	[Test]
	public void MapRecordingFromSearch_PaginatedResults_FirstAndLastItemTitlesAreCorrect()
	{
		// Arrange
		List<IRecording> recordings = MusicBrainzTestData.CreateSearchResult(count: 50);

		// Act
		List<MusicBrainzRecording> results = recordings
			.Select(MusicBrainzMapper.MapRecordingFromSearch)
			.ToList();

		// Assert
		results[0].Title.Should().Be("Recording 1");
		results[49].Title.Should().Be("Recording 50");
	}
}
