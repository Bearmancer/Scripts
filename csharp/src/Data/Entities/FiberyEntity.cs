namespace CSharpScripts.Data.Entities;

internal sealed record FiberyEntity
{
    public Guid Id { get; set; }
    public string FiberyId { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public JsonDocument? RawData { get; set; }
}
