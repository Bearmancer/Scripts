using System;
using System.Collections.Generic;

namespace Scripts.Data.Entities;

public sealed class Issue
{
    public int Id { get; set; }
    public string Identifier { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string TitleLower { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? ProjectId { get; set; }

    public Project? Project { get; set; }
    public ICollection<ExecutionLog> ExecutionLogs { get; set; } = new List<ExecutionLog>();
}
