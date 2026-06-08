using System.Text.Json;

namespace Scripts.Data.Entities;





public sealed class ExecutionLog
{
	public int Id { get; set; }
	public DateTimeOffset Timestamp { get; set; }
	public string SessionId { get; set; } = string.Empty;
	public JsonDocument Payload { get; set; } = JsonDocument.Parse("{}");
	public int ExitCode { get; set; }

	public ICollection<FailedTask> FailedTasks { get; init; } = new List<FailedTask>();
}
