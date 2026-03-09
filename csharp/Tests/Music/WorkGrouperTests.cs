using CSharpScripts.CLI.Music;
using CSharpScripts.Models;
using FluentAssertions;

namespace CSharpScripts.Tests.Music;

internal sealed class WorkGrouperTests
{
	[Test]
	public void WhenGroupingTracksThenDurationsSoloistsAndVenuesAreMergedPerWork()
	{
		List<TrackInfo> tracks =
		[
			new(
				DiscNumber: 1,
				TrackNumber: 1,
				Title: "I. De l'aube à midi sur la mer",
				Duration: TimeSpan.FromMinutes(9),
				RecordingYear: 1964,
				Composer: "Claude Debussy",
				WorkName: "La mer",
				Conductor: "Herbert von Karajan",
				Orchestra: "Berlin Philharmonic",
				Soloists: ["Soloist A"],
				Artist: "Berlin Philharmonic",
				RecordingVenue: "Jesus-Christus-Kirche"
			),
			new(
				DiscNumber: 1,
				TrackNumber: 2,
				Title: "II. Jeux de vagues",
				Duration: TimeSpan.FromMinutes(7),
				RecordingYear: 1964,
				Composer: "Claude Debussy",
				WorkName: "La mer",
				Conductor: "Herbert von Karajan",
				Orchestra: "Berlin Philharmonic",
				Soloists: ["Soloist A", "Soloist B"],
				Artist: "Berlin Philharmonic",
				RecordingVenue: "Jesus-Christus-Kirche"
			),
			new(
				DiscNumber: 1,
				TrackNumber: 3,
				Title: "III. Dialogue du vent et de la mer",
				Duration: TimeSpan.FromMinutes(8),
				RecordingYear: 1964,
				Composer: "Claude Debussy",
				WorkName: "La mer",
				Conductor: "Herbert von Karajan",
				Orchestra: "Berlin Philharmonic",
				Soloists: ["Soloist B"],
				Artist: "Berlin Philharmonic",
				RecordingVenue: "Philharmonie Berlin"
			),
		];

		List<WorkSummary> works = WorkGrouper.Group(tracks);

		works.Should().HaveCount(1);
		works[0].TotalDuration.Should().Be(TimeSpan.FromMinutes(24));
		works[0].Soloists.Should().BeEquivalentTo(["Soloist A", "Soloist B"]);
		works[0]
			.RecordingVenues.Should()
			.BeEquivalentTo(["Jesus-Christus-Kirche", "Philharmonie Berlin"]);
	}
}
