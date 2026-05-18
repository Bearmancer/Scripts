#pragma warning disable CS0168, IDE0059, IDE0060, CA2000, CS8604
namespace CSharpScripts.Data.Entities;

internal sealed record Artist
{
	public int Id { get; init; }
	public string Name { get; init; } = null!;
	public string? Mbid { get; init; }
	public JsonDocument? Metadata { get; init; }

	public ICollection<Album> Albums { get; } = [];
	public ICollection<Track> Tracks { get; } = [];
}
