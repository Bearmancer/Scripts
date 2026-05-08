namespace CSharpScripts.Data.Entities;

internal sealed record SourceRecord
{
    public Guid Id { get; set; }
    public string SourceId { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public JsonDocument? RawData { get; set; }
}

