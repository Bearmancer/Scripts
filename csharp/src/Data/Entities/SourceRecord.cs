namespace Scripts.Data.Entities;

/// <summary>
/// Represents a source record for data integration tracking.
/// RawData is stored as JSONB for flexible schema evolution.
/// </summary>
internal sealed record SourceRecord
{
	public Guid Id { get; set; }
	public string SourceId { get; set; } = "";
	public string EntityType { get; set; } = "";
	public JsonDocument? RawData { get; set; }
}
