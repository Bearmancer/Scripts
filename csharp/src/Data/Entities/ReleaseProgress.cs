using System.Text.Json;

namespace CSharpScripts.Data.Entities;

internal sealed record ReleaseProgress
{
	public long Id { get; set; }
	public string ReleaseId { get; set; } = null!;
	public int DiscNumber { get; set; }
	public int TrackNumber { get; set; }
	public string Title { get; set; } = null!;
	public string? Duration { get; set; }
	public int? RecordingYear { get; set; }
	public string? Composer { get; set; }
	public string? WorkName { get; set; }
	public string? Conductor { get; set; }
	public string? Orchestra { get; set; }
	public JsonDocument? Soloists { get; set; }
	public string? Artist { get; set; }
	public string? RecordingVenue { get; set; }
	public string? RecordingId { get; set; }
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
