using System;
using System.Text.Json;

namespace Scripts.Data.Entities;

public sealed class ExecutionLog
{
    public int Id { get; set; }
    public int IssueId { get; set; }
    public string TaskName { get; set; } = string.Empty;
    public JsonDocument Input { get; set; } = null!;
    public JsonDocument? Output { get; set; }
    public string? Error { get; set; }
    public string Status { get; set; } = string.Empty;
    public int AttemptNumber { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public int DurationMs { get; set; }

    public Issue Issue { get; set; } = null!;
    public ICollection<FailedTask> FailedTasks { get; set; } = new List<FailedTask>();
}
