namespace CSharpScripts.Data.Entities;

internal sealed record SourceRecord
{
	public Guid Id { get; set; }
	public string SourceId { get; set; } = "";
	public string EntityType { get; set; } = "";
	public JsonDocument? RawData { get; set; }
}
