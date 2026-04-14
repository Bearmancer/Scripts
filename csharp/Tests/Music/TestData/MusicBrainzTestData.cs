using MetaBrainz.MusicBrainz;
using MetaBrainz.MusicBrainz.Interfaces.Entities;
using NSubstitute;

namespace CSharpScripts.Tests.Music.TestData;

internal static class MusicBrainzTestData
{
	internal static IRelease CreateBoxSetRelease(int discCount, int tracksPerDisc)
	{
		IRelease release = Substitute.For<IRelease>();
		SubstituteExtensions.Returns(release.Id, Guid.NewGuid());
		SubstituteExtensions.Returns(release.Title, returnThis: "The Complete Box Set");

		var media = Enumerable.ToList(
			Enumerable.Select(
				Enumerable.Range(start: 1, count: discCount),
				position => CreateMedium(position: position, trackCount: tracksPerDisc)
			)
		);

		SubstituteExtensions.Returns(release.Media, returnThis: media);
		SubstituteExtensions.Returns(release.ArtistCredit, (IReadOnlyList<INameCredit>?)null);
		SubstituteExtensions.Returns(release.Relationships, (IReadOnlyList<IRelationship>?)null);
		SubstituteExtensions.Returns(release.LabelInfo, (IReadOnlyList<ILabelInfo>?)null);
		SubstituteExtensions.Returns(release.Tags, (IReadOnlyList<ITag>?)null);
		SubstituteExtensions.Returns(release.Genres, (IReadOnlyList<IGenre>?)null);
		SubstituteExtensions.Returns(release.Date, (PartialDate?)null);
		SubstituteExtensions.Returns(release.ReleaseGroup, (IReleaseGroup?)null);

		return release;
	}

	internal static IRelease CreateClassicalRelease()
	{
		IRelease release = Substitute.For<IRelease>();
		SubstituteExtensions.Returns(release.Id, Guid.NewGuid());
		SubstituteExtensions.Returns(release.Title, returnThis: "Beethoven: Symphony No. 9");

		IMedium classicalMedium = CreateMedium(position: 1, trackCount: 4);
		SubstituteExtensions.Returns(release.Media, new List<IMedium> { classicalMedium });

		IArtist conductorArtist = Substitute.For<IArtist>();
		SubstituteExtensions.Returns(conductorArtist.Id, Guid.NewGuid());
		SubstituteExtensions.Returns(conductorArtist.Name, returnThis: "Herbert von Karajan");

		IRelationship conductorRel = Substitute.For<IRelationship>();
		SubstituteExtensions.Returns(conductorRel.Type, returnThis: "conductor");
		SubstituteExtensions.Returns(conductorRel.Artist, returnThis: conductorArtist);
		SubstituteExtensions.Returns(conductorRel.Attributes, (IReadOnlyList<string?>?)null);
		SubstituteExtensions.Returns(conductorRel.Begin, (PartialDate?)null);

		IArtist orchestraArtist = Substitute.For<IArtist>();
		SubstituteExtensions.Returns(orchestraArtist.Id, Guid.NewGuid());
		SubstituteExtensions.Returns(orchestraArtist.Name, returnThis: "Berlin Philharmonic");

		IRelationship orchestraRel = Substitute.For<IRelationship>();
		SubstituteExtensions.Returns(orchestraRel.Type, returnThis: "orchestra");
		SubstituteExtensions.Returns(orchestraRel.Artist, returnThis: orchestraArtist);
		SubstituteExtensions.Returns(orchestraRel.Attributes, (IReadOnlyList<string?>?)null);
		SubstituteExtensions.Returns(orchestraRel.Begin, (PartialDate?)null);

		SubstituteExtensions.Returns(
			release.Relationships,
			new List<IRelationship> { conductorRel, orchestraRel }
		);

		INameCredit nameCredit = CreateNameCredit(
			artistName: "Berlin Philharmonic",
			joinPhrase: null
		);
		SubstituteExtensions.Returns(release.ArtistCredit, new List<INameCredit> { nameCredit });

		SubstituteExtensions.Returns(release.LabelInfo, (IReadOnlyList<ILabelInfo>?)null);
		SubstituteExtensions.Returns(release.Tags, (IReadOnlyList<ITag>?)null);
		SubstituteExtensions.Returns(release.Genres, (IReadOnlyList<IGenre>?)null);
		SubstituteExtensions.Returns(release.Date, (PartialDate?)null);
		SubstituteExtensions.Returns(release.ReleaseGroup, (IReleaseGroup?)null);

		return release;
	}

	internal static IRelease CreatePopRelease()
	{
		IRelease release = Substitute.For<IRelease>();
		SubstituteExtensions.Returns(release.Id, Guid.NewGuid());
		SubstituteExtensions.Returns(release.Title, returnThis: "Thriller");
		SubstituteExtensions.Returns(release.Status, returnThis: "Official");
		SubstituteExtensions.Returns(release.Country, returnThis: "US");

		IMedium popMedium = CreateMedium(position: 1, trackCount: 9);
		SubstituteExtensions.Returns(release.Media, new List<IMedium> { popMedium });

		INameCredit nameCredit = CreateNameCredit(artistName: "Michael Jackson", joinPhrase: null);
		SubstituteExtensions.Returns(release.ArtistCredit, new List<INameCredit> { nameCredit });

		SubstituteExtensions.Returns(release.Relationships, (IReadOnlyList<IRelationship>?)null);
		SubstituteExtensions.Returns(release.LabelInfo, (IReadOnlyList<ILabelInfo>?)null);
		SubstituteExtensions.Returns(release.Tags, (IReadOnlyList<ITag>?)null);
		SubstituteExtensions.Returns(release.Genres, (IReadOnlyList<IGenre>?)null);
		SubstituteExtensions.Returns(release.Date, (PartialDate?)null);
		SubstituteExtensions.Returns(release.ReleaseGroup, (IReleaseGroup?)null);

		return release;
	}

	internal static IRelease CreateMinimalRelease()
	{
		IRelease release = Substitute.For<IRelease>();
		SubstituteExtensions.Returns(release.Id, Guid.NewGuid());
		SubstituteExtensions.Returns(release.Title, (string?)null);
		SubstituteExtensions.Returns(release.Media, (IReadOnlyList<IMedium>?)null);
		SubstituteExtensions.Returns(release.ArtistCredit, (IReadOnlyList<INameCredit>?)null);
		SubstituteExtensions.Returns(release.Relationships, (IReadOnlyList<IRelationship>?)null);
		SubstituteExtensions.Returns(release.LabelInfo, (IReadOnlyList<ILabelInfo>?)null);
		SubstituteExtensions.Returns(release.Tags, (IReadOnlyList<ITag>?)null);
		SubstituteExtensions.Returns(release.Genres, (IReadOnlyList<IGenre>?)null);
		SubstituteExtensions.Returns(release.Date, (PartialDate?)null);
		SubstituteExtensions.Returns(release.ReleaseGroup, (IReleaseGroup?)null);
		SubstituteExtensions.Returns(release.Country, (string?)null);
		SubstituteExtensions.Returns(release.Status, (string?)null);
		SubstituteExtensions.Returns(release.Barcode, (string?)null);
		SubstituteExtensions.Returns(release.Asin, (string?)null);
		SubstituteExtensions.Returns(release.Quality, (string?)null);
		SubstituteExtensions.Returns(release.Packaging, (string?)null);
		SubstituteExtensions.Returns(release.Disambiguation, (string?)null);
		SubstituteExtensions.Returns(release.Annotation, (string?)null);

		return release;
	}

	internal static IArtist CreateArtist(string name, string? sortName = null)
	{
		IArtist artist = Substitute.For<IArtist>();
		SubstituteExtensions.Returns(artist.Id, Guid.NewGuid());
		SubstituteExtensions.Returns(artist.Name, returnThis: name);
		SubstituteExtensions.Returns(artist.SortName, sortName ?? name);
		SubstituteExtensions.Returns(artist.Type, returnThis: "Person");
		SubstituteExtensions.Returns(artist.Gender, (string?)null);
		SubstituteExtensions.Returns(artist.Country, (string?)null);
		SubstituteExtensions.Returns(artist.Area, (IArea?)null);
		SubstituteExtensions.Returns(artist.Disambiguation, (string?)null);
		SubstituteExtensions.Returns(artist.LifeSpan, (ILifeSpan?)null);
		SubstituteExtensions.Returns(artist.Aliases, (IReadOnlyList<IAlias>?)null);
		SubstituteExtensions.Returns(artist.Tags, (IReadOnlyList<ITag>?)null);
		SubstituteExtensions.Returns(artist.Genres, (IReadOnlyList<IGenre>?)null);
		SubstituteExtensions.Returns(artist.Annotation, (string?)null);
		SubstituteExtensions.Returns(artist.Rating, (IRating?)null);

		return artist;
	}

	internal static INameCredit CreateNameCredit(string artistName, string? joinPhrase)
	{
		INameCredit nameCredit = Substitute.For<INameCredit>();
		IArtist artist = Substitute.For<IArtist>();

		SubstituteExtensions.Returns(artist.Id, Guid.NewGuid());
		SubstituteExtensions.Returns(artist.Name, returnThis: artistName);

		SubstituteExtensions.Returns(nameCredit.Artist, returnThis: artist);
		SubstituteExtensions.Returns(nameCredit.Name, returnThis: artistName);
		SubstituteExtensions.Returns(nameCredit.JoinPhrase, returnThis: joinPhrase);

		return nameCredit;
	}

	internal static IRecording CreateRecording(string title, string? artistName = null)
	{
		IRecording recording = Substitute.For<IRecording>();
		SubstituteExtensions.Returns(recording.Id, Guid.NewGuid());
		SubstituteExtensions.Returns(recording.Title, returnThis: title);
		SubstituteExtensions.Returns(recording.Length, TimeSpan.FromMinutes(minutes: 4));
		SubstituteExtensions.Returns(recording.FirstReleaseDate, (PartialDate?)null);
		SubstituteExtensions.Returns(recording.Disambiguation, (string?)null);
		SubstituteExtensions.Returns(recording.Isrcs, (IReadOnlyList<string>?)null);
		SubstituteExtensions.Returns(recording.Tags, (IReadOnlyList<ITag>?)null);
		SubstituteExtensions.Returns(recording.Genres, (IReadOnlyList<IGenre>?)null);
		SubstituteExtensions.Returns(recording.Rating, (IRating?)null);
		SubstituteExtensions.Returns(recording.Annotation, (string?)null);
		SubstituteExtensions.Returns(recording.Relationships, (IReadOnlyList<IRelationship>?)null);

		if (artistName is { })
		{
			INameCredit nameCredit = CreateNameCredit(artistName: artistName, joinPhrase: null);
			SubstituteExtensions.Returns(
				recording.ArtistCredit,
				new List<INameCredit> { nameCredit }
			);
		}
		else
			SubstituteExtensions.Returns(recording.ArtistCredit, (IReadOnlyList<INameCredit>?)null);

		return recording;
	}

	internal static List<IRecording> CreateSearchResult(int count) =>
		Enumerable.ToList(
			Enumerable.Select(
				Enumerable.Range(start: 1, count: count),
				i => CreateRecording($"Recording {i}", $"Artist {i}")
			)
		);

	private static IMedium CreateMedium(int position, int trackCount)
	{
		IMedium medium = Substitute.For<IMedium>();
		SubstituteExtensions.Returns(medium.Position, returnThis: position);
		SubstituteExtensions.Returns(medium.Format, returnThis: "CD");
		SubstituteExtensions.Returns(medium.Title, (string?)null);
		SubstituteExtensions.Returns(medium.TrackCount, returnThis: trackCount);

		var tracks = Enumerable.ToList(
			Enumerable.Select(Enumerable.Range(start: 1, count: trackCount), selector: CreateTrack)
		);

		SubstituteExtensions.Returns(medium.Tracks, returnThis: tracks);

		return medium;
	}

	private static ITrack CreateTrack(int position)
	{
		ITrack track = Substitute.For<ITrack>();
		SubstituteExtensions.Returns(track.Id, Guid.NewGuid());
		SubstituteExtensions.Returns(track.Title, $"Track {position}");
		SubstituteExtensions.Returns(track.Position, position);
		SubstituteExtensions.Returns(track.Number, position.ToString());
		SubstituteExtensions.Returns(track.Length, TimeSpan.FromMinutes(minutes: 3));
		SubstituteExtensions.Returns(track.Recording, (IRecording?)null);
		SubstituteExtensions.Returns(track.ArtistCredit, (IReadOnlyList<INameCredit>?)null);

		return track;
	}
}
