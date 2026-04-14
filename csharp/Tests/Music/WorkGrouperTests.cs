using CSharpScripts.CLI.Music;
using CSharpScripts.Models;
using AssertionExtensions = FluentAssertions.AssertionExtensions;

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
				TimeSpan.FromMinutes(minutes: 9),
				RecordingYear: 1964,
				Composer: "Claude Debussy",
				WorkName: "La mer",
				Conductor: "Herbert von Karajan",
				Orchestra: "Berlin Philharmonic",
				["Soloist A"],
				Artist: "Berlin Philharmonic",
				RecordingVenue: "Jesus-Christus-Kirche"
			),
			new(
				DiscNumber: 1,
				TrackNumber: 2,
				Title: "II. Jeux de vagues",
				TimeSpan.FromMinutes(minutes: 7),
				RecordingYear: 1964,
				Composer: "Claude Debussy",
				WorkName: "La mer",
				Conductor: "Herbert von Karajan",
				Orchestra: "Berlin Philharmonic",
				["Soloist A", "Soloist B"],
				Artist: "Berlin Philharmonic",
				RecordingVenue: "Jesus-Christus-Kirche"
			),
			new(
				DiscNumber: 1,
				TrackNumber: 3,
				Title: "III. Dialogue du vent et de la mer",
				TimeSpan.FromMinutes(minutes: 8),
				RecordingYear: 1964,
				Composer: "Claude Debussy",
				WorkName: "La mer",
				Conductor: "Herbert von Karajan",
				Orchestra: "Berlin Philharmonic",
				["Soloist B"],
				Artist: "Berlin Philharmonic",
				RecordingVenue: "Philharmonie Berlin"
			),
		];

		List<WorkSummary> works = WorkGrouper.Group(tracks: tracks);

		AssertionExtensions.Should(works).HaveCount(expected: 1);
		AssertionExtensions.Should(works[index: 0].Years).Equal(1964);
		AssertionExtensions
			.Should(works[index: 0].TotalDuration)
			.Be(TimeSpan.FromMinutes(minutes: 24));
		AssertionExtensions.Should(works[index: 0].TrackRange).Be("1-3");
		AssertionExtensions
			.Should(works[index: 0].Soloists)
			.BeEquivalentTo("Soloist A", "Soloist B");
		AssertionExtensions
			.Should(works[index: 0].RecordingVenues)
			.BeEquivalentTo("Jesus-Christus-Kirche", "Philharmonie Berlin");
	}
}
