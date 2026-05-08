namespace CSharpScripts.Data.Entities;

internal sealed record FailedTask
{
    public int Id { get; set; }
    public string TaskName { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}
