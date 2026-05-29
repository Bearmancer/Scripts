namespace CSharpScripts.Data.Entities;

/// <summary>
/// Represents a Fibery entity record for integration tracking.
/// RawData is stored as JSONB for flexible schema evolution.
/// </summary>
internal sealed record FiberyEntity
{
	public Guid Id { get; set; }
	public string FiberyId { get; set; } = "";
	public string EntityType { get; set; } = "";
	public JsonDocument? RawData { get; set; }
}
