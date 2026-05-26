namespace CSharpScripts.Data.Entities;

/// <summary>
/// Records a failed background operation for retry or alerting.
/// Guid PK prevents collisions across distributed or long-running sessions.
/// </summary>
public sealed class FailedTask
{
	public Guid Id { get; set; }
	public string TaskName { get; set; } = string.Empty;
	public string ErrorMessage { get; set; } = string.Empty;
	public DateTimeOffset Timestamp { get; set; }
}
