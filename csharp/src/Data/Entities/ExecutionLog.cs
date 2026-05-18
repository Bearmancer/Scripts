namespace CSharpScripts.Data.Entities;

internal sealed record ExecutionLog
{
	public int Id { get; set; }
	public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
	public string? SessionId { get; set; }
	public JsonDocument? Payload { get; set; }
	public int? ExitCode { get; set; }
}


