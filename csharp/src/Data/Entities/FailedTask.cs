namespace Scripts.Data.Entities;

public sealed class FailedTask
{
	public Guid Id { get; set; }
	public string TaskName { get; set; } = string.Empty;
	public string ErrorMessage { get; set; } = string.Empty;
	public DateTimeOffset Timestamp { get; set; }
	public int? ExecutionLogId { get; set; }
	public ExecutionLog? ExecutionLog { get; set; }
}
