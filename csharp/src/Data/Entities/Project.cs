namespace Scripts.Data.Entities;

public sealed class Project
{
	public Guid Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public string NameLower { get; set; } = string.Empty;
	public string Slug { get; set; } = string.Empty;
	public string Status { get; set; } = string.Empty;
	public DateTimeOffset CreatedAt { get; set; }
	public DateTimeOffset UpdatedAt { get; set; }

	public ICollection<Issue> Issues { get; set; } = new List<Issue>();
}
