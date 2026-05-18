#pragma warning disable CS0168, IDE0059, IDE0060, CA2000, CS8604
namespace CSharpScripts.Data.Entities;

internal sealed record Video
{
	public long Id { get; init; }
	public string Url { get; init; } = null!;
	public string Title { get; init; } = null!;
	public string? Description { get; init; }
	public string ChannelName { get; init; } = null!;
	public DateOnly UploadDate { get; init; }
	public DateTimeOffset SyncedAt { get; init; }

	public Dictionary<string, string> Metadata { get; init; } = [];
}
