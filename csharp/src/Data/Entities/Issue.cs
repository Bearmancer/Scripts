namespace Scripts.Data.Entities;

public sealed class Issue
{
	public Guid Id { get; set; }
	public string Identifier { get; set; } = string.Empty;
	public string Title { get; set; } = string.Empty;
	public string TitleLower { get; set; } = string.Empty;
	public string? Description { get; set; }
	public string Status { get; set; } = string.Empty;
	public Guid? ProjectId { get; set; }
	public string Priority { get; set; } = string.Empty;
	public int PrioritySort { get; set; }
	public int? Estimate { get; set; }
	public Guid? ParentId { get; set; }
	public DateTimeOffset CreatedAt { get; set; }
	public DateTimeOffset UpdatedAt { get; set; }

	public Project? Project { get; set; }
	public Issue? Parent { get; set; }
	public ICollection<Issue> SubTasks { get; set; } = new List<Issue>();
}
