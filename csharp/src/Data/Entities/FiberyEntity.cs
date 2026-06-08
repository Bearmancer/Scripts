namespace Scripts.Data.Entities;





internal sealed record FiberyEntity
{
	public Guid Id { get; set; }
	public string FiberyId { get; set; } = "";
	public string EntityType { get; set; } = "";
	public JsonDocument? RawData { get; set; }
}
