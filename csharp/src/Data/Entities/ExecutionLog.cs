using System.Text.Json;

namespace CSharpScripts.Data.Entities;

/// <summary>
/// Records a CLI execution session. Payload is JSONB for flexible structured metadata.
/// Id is SERIAL (auto-increment int) — high frequency writes expected.
/// </summary>
public sealed class ExecutionLog
{
	public int Id { get; set; }
	public DateTimeOffset Timestamp { get; set; }
	public string SessionId { get; set; } = string.Empty;
	public JsonDocument Payload { get; set; } = JsonDocument.Parse("{}");
	public int ExitCode { get; set; }
}

