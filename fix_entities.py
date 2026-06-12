import os

entities_dir = 'csharp/src/Data/Entities'
configs_dir = 'csharp/src/Data/Configuration'

entities = {
    'Project': '''using System;
using System.Collections.Generic;

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
''',
    'Issue': '''using System;
using System.Collections.Generic;

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
''',
    'ExecutionLog': '''using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Scripts.Data.Entities;

public sealed class ExecutionLog
{
    public int Id { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public JsonDocument Payload { get; set; } = null!;
    public int ExitCode { get; set; }

    public ICollection<FailedTask> FailedTasks { get; set; } = new List<FailedTask>();
}
'''
}

configs = {
    'Project': '''using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Scripts.Data.Entities;

namespace Scripts.Data.Configuration;

internal sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("projects", "work");
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Name).IsRequired();
        builder.Property(x => x.NameLower).IsRequired();
        builder.Property(x => x.Slug).IsRequired();
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
        builder.Property(x => x.UpdatedAt).HasDefaultValueSql("NOW()");
    }
}
''',
    'Issue': '''using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Scripts.Data.Entities;

namespace Scripts.Data.Configuration;

internal sealed class IssueConfiguration : IEntityTypeConfiguration<Issue>
{
    public void Configure(EntityTypeBuilder<Issue> builder)
    {
        builder.ToTable("issues", "work");
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Identifier).IsRequired();
        builder.HasIndex(x => x.Identifier).IsUnique();
        
        builder.Property(x => x.Title).IsRequired();
        builder.Property(x => x.TitleLower).IsRequired();
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.Priority).IsRequired();
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
        builder.Property(x => x.UpdatedAt).HasDefaultValueSql("NOW()");
        
        builder.HasOne(x => x.Project)
            .WithMany(x => x.Issues)
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Parent)
            .WithMany(x => x.SubTasks)
            .HasForeignKey(x => x.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
''',
    'ExecutionLog': '''using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Scripts.Data.Entities;

namespace Scripts.Data.Configuration;

internal sealed class ExecutionLogConfiguration : IEntityTypeConfiguration<ExecutionLog>
{
    public void Configure(EntityTypeBuilder<ExecutionLog> builder)
    {
        builder.ToTable("execution_logs", "work");
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.SessionId).IsRequired();
        builder.Property(x => x.Payload).HasColumnType("jsonb").IsRequired();
        
        builder.Property(x => x.Timestamp).HasDefaultValueSql("NOW()");
    }
}
'''
}

for name, content in entities.items():
    with open(f'{entities_dir}/{name}.cs', 'w', encoding='utf-8') as f:
        f.write(content)

for name, content in configs.items():
    with open(f'{configs_dir}/{name}Configuration.cs', 'w', encoding='utf-8') as f:
        f.write(content)

print("Updated Project, Issue, and ExecutionLog.")
