#pragma warning disable CS0168, IDE0059, IDE0060, CA2000, CS8604
namespace CSharpScripts.Data.Entities;

internal sealed record Track
{
	public int Id { get; init; }
	public int ArtistId { get; init; }
	public int? AlbumId { get; init; }
	public string Title { get; init; } = null!;
	public int? Duration { get; init; }
	public string? Mbid { get; init; }

	public Artist Artist { get; init; } = null!;
	public Album? Album { get; init; }
	public ICollection<Scrobble> Scrobbles { get; } = [];
}
