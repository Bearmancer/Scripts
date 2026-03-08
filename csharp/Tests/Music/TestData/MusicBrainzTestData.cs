using MetaBrainz.MusicBrainz;
using MetaBrainz.MusicBrainz.Interfaces.Entities;
using NSubstitute;

namespace CSharpScripts.Tests.Music.TestData;

internal static class MusicBrainzTestData
{
	internal static IRelease CreateBoxSetRelease(int discCount, int tracksPerDisc)
	{
		IRelease release = Substitute.For<IRelease>();
		release.Id.Returns(Guid.NewGuid());
		release.Title.Returns("The Complete Box Set");

		List<IMedium> media = Enumerable
			.Range(1, discCount)
			.Select(position => CreateMedium(position, tracksPerDisc))
			.ToList();

		release.Media.Returns(media);
		release.ArtistCredit.Returns((IReadOnlyList<INameCredit>?)null);
		release.Relationships.Returns((IReadOnlyList<IRelationship>?)null);
		release.LabelInfo.Returns((IReadOnlyList<ILabelInfo>?)null);
		release.Tags.Returns((IReadOnlyList<ITag>?)null);
		release.Genres.Returns((IReadOnlyList<IGenre>?)null);
		release.Date.Returns((PartialDate?)null);
		release.ReleaseGroup.Returns((IReleaseGroup?)null);

		return release;
	}

	internal static IRelease CreateClassicalRelease()
	{
		IRelease release = Substitute.For<IRelease>();
		release.Id.Returns(Guid.NewGuid());
		release.Title.Returns("Beethoven: Symphony No. 9");

		IMedium classicalMedium = CreateMedium(position: 1, trackCount: 4);
		release.Media.Returns(new List<IMedium> { classicalMedium });

		IArtist conductorArtist = Substitute.For<IArtist>();
		conductorArtist.Id.Returns(Guid.NewGuid());
		conductorArtist.Name.Returns("Herbert von Karajan");

		IRelationship conductorRel = Substitute.For<IRelationship>();
		conductorRel.Type.Returns("conductor");
		conductorRel.Artist.Returns(conductorArtist);
		conductorRel.Attributes.Returns((IReadOnlyList<string?>?)null);
		conductorRel.Begin.Returns((PartialDate?)null);

		IArtist orchestraArtist = Substitute.For<IArtist>();
		orchestraArtist.Id.Returns(Guid.NewGuid());
		orchestraArtist.Name.Returns("Berlin Philharmonic");

		IRelationship orchestraRel = Substitute.For<IRelationship>();
		orchestraRel.Type.Returns("orchestra");
		orchestraRel.Artist.Returns(orchestraArtist);
		orchestraRel.Attributes.Returns((IReadOnlyList<string?>?)null);
		orchestraRel.Begin.Returns((PartialDate?)null);

		release.Relationships.Returns(new List<IRelationship> { conductorRel, orchestraRel });

		INameCredit nameCredit = CreateNameCredit("Berlin Philharmonic", joinPhrase: null);
		release.ArtistCredit.Returns(new List<INameCredit> { nameCredit });

		release.LabelInfo.Returns((IReadOnlyList<ILabelInfo>?)null);
		release.Tags.Returns((IReadOnlyList<ITag>?)null);
		release.Genres.Returns((IReadOnlyList<IGenre>?)null);
		release.Date.Returns((PartialDate?)null);
		release.ReleaseGroup.Returns((IReleaseGroup?)null);

		return release;
	}

	internal static IRelease CreatePopRelease()
	{
		IRelease release = Substitute.For<IRelease>();
		release.Id.Returns(Guid.NewGuid());
		release.Title.Returns("Thriller");
		release.Status.Returns("Official");
		release.Country.Returns("US");

		IMedium popMedium = CreateMedium(position: 1, trackCount: 9);
		release.Media.Returns(new List<IMedium> { popMedium });

		INameCredit nameCredit = CreateNameCredit("Michael Jackson", joinPhrase: null);
		release.ArtistCredit.Returns(new List<INameCredit> { nameCredit });

		release.Relationships.Returns((IReadOnlyList<IRelationship>?)null);
		release.LabelInfo.Returns((IReadOnlyList<ILabelInfo>?)null);
		release.Tags.Returns((IReadOnlyList<ITag>?)null);
		release.Genres.Returns((IReadOnlyList<IGenre>?)null);
		release.Date.Returns((PartialDate?)null);
		release.ReleaseGroup.Returns((IReleaseGroup?)null);

		return release;
	}

	internal static IRelease CreateMinimalRelease()
	{
		IRelease release = Substitute.For<IRelease>();
		release.Id.Returns(Guid.NewGuid());
		release.Title.Returns((string?)null);
		release.Media.Returns((IReadOnlyList<IMedium>?)null);
		release.ArtistCredit.Returns((IReadOnlyList<INameCredit>?)null);
		release.Relationships.Returns((IReadOnlyList<IRelationship>?)null);
		release.LabelInfo.Returns((IReadOnlyList<ILabelInfo>?)null);
		release.Tags.Returns((IReadOnlyList<ITag>?)null);
		release.Genres.Returns((IReadOnlyList<IGenre>?)null);
		release.Date.Returns((PartialDate?)null);
		release.ReleaseGroup.Returns((IReleaseGroup?)null);
		release.Country.Returns((string?)null);
		release.Status.Returns((string?)null);
		release.Barcode.Returns((string?)null);
		release.Asin.Returns((string?)null);
		release.Quality.Returns((string?)null);
		release.Packaging.Returns((string?)null);
		release.Disambiguation.Returns((string?)null);
		release.Annotation.Returns((string?)null);

		return release;
	}

	internal static IArtist CreateArtist(string name, string? sortName = null)
	{
		IArtist artist = Substitute.For<IArtist>();
		artist.Id.Returns(Guid.NewGuid());
		artist.Name.Returns(name);
		artist.SortName.Returns(sortName ?? name);
		artist.Type.Returns("Person");
		artist.Gender.Returns((string?)null);
		artist.Country.Returns((string?)null);
		artist.Area.Returns((IArea?)null);
		artist.Disambiguation.Returns((string?)null);
		artist.LifeSpan.Returns((ILifeSpan?)null);
		artist.Aliases.Returns((IReadOnlyList<IAlias>?)null);
		artist.Tags.Returns((IReadOnlyList<ITag>?)null);
		artist.Genres.Returns((IReadOnlyList<IGenre>?)null);
		artist.Annotation.Returns((string?)null);
		artist.Rating.Returns((IRating?)null);

		return artist;
	}

	internal static INameCredit CreateNameCredit(string artistName, string? joinPhrase)
	{
		INameCredit nameCredit = Substitute.For<INameCredit>();
		IArtist artist = Substitute.For<IArtist>();

		artist.Id.Returns(Guid.NewGuid());
		artist.Name.Returns(artistName);

		nameCredit.Artist.Returns(artist);
		nameCredit.Name.Returns(artistName);
		nameCredit.JoinPhrase.Returns(joinPhrase);

		return nameCredit;
	}

	internal static IRecording CreateRecording(string title, string? artistName = null)
	{
		IRecording recording = Substitute.For<IRecording>();
		recording.Id.Returns(Guid.NewGuid());
		recording.Title.Returns(title);
		recording.Length.Returns((TimeSpan?)TimeSpan.FromMinutes(4));
		recording.FirstReleaseDate.Returns((PartialDate?)null);
		recording.Disambiguation.Returns((string?)null);
		recording.Isrcs.Returns((IReadOnlyList<string>?)null);
		recording.Tags.Returns((IReadOnlyList<ITag>?)null);
		recording.Genres.Returns((IReadOnlyList<IGenre>?)null);
		recording.Rating.Returns((IRating?)null);
		recording.Annotation.Returns((string?)null);
		recording.Relationships.Returns((IReadOnlyList<IRelationship>?)null);

		if (artistName is not null)
		{
			INameCredit nameCredit = CreateNameCredit(artistName, joinPhrase: null);
			recording.ArtistCredit.Returns(new List<INameCredit> { nameCredit });
		}
		else
		{
			recording.ArtistCredit.Returns((IReadOnlyList<INameCredit>?)null);
		}

		return recording;
	}

	internal static List<IRecording> CreateSearchResult(int count) =>
		Enumerable
			.Range(1, count)
			.Select(i => CreateRecording($"Recording {i}", $"Artist {i}"))
			.ToList();

	private static IMedium CreateMedium(int position, int trackCount)
	{
		IMedium medium = Substitute.For<IMedium>();
		medium.Position.Returns(position);
		medium.Format.Returns("CD");
		medium.Title.Returns((string?)null);
		medium.TrackCount.Returns(trackCount);

		List<ITrack> tracks = Enumerable.Range(1, trackCount).Select(CreateTrack).ToList();

		medium.Tracks.Returns(tracks);

		return medium;
	}

	private static ITrack CreateTrack(int position)
	{
		ITrack track = Substitute.For<ITrack>();
		track.Id.Returns(Guid.NewGuid());
		track.Title.Returns($"Track {position}");
		track.Position.Returns((int?)position);
		track.Number.Returns(position.ToString());
		track.Length.Returns((TimeSpan?)TimeSpan.FromMinutes(3));
		track.Recording.Returns((IRecording?)null);
		track.ArtistCredit.Returns((IReadOnlyList<INameCredit>?)null);

		return track;
	}
}
