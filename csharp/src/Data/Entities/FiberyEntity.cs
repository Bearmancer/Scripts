namespace CSharpScripts.Data.Entities;

internal sealed record FiberyEntity
{
	public Guid Id { get; set; }
	public string FiberyId { get; set; } = Empty;
	public string EntityType { get; set; } = Empty;
	public JsonDocument? RawData { get; set; }
}


