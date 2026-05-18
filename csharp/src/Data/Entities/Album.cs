#pragma warning disable CS0168, IDE0059, IDE0060, CA2000, CS8604
namespace CSharpScripts.Data.Entities;

internal sealed record Album
{
	public int Id { get; init; }
	public int ArtistId { get; init; }
	public string Title { get; init; } = null!;
	public DateOnly? ReleaseDate { get; init; }
	public string? Mbid { get; init; }

	public Artist Artist { get; init; } = null!;
	public ICollection<Track> Tracks { get; } = [];
}
