#pragma warning disable CS0168, IDE0059, IDE0060, CA2000, CS8604
namespace CSharpScripts.Data.Entities;

internal sealed record Scrobble
{
	public long Id { get; init; }
	public int TrackId { get; init; }
	public DateTimeOffset ScrobbledAt { get; init; }
	public string Platform { get; init; } = null!;

	public Track Track { get; init; } = null!;
}
