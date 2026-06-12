using System;
using System.Collections.Generic;

namespace Scripts.Data.Entities;

public sealed class Project
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NameLower { get; set; } = string.Empty;

    public ICollection<Issue> Issues { get; set; } = new List<Issue>();
}
