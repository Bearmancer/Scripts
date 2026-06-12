import os

entities_dir = 'csharp/src/Data/Entities'
configs_dir = 'csharp/src/Data/Configuration'

os.makedirs(entities_dir, exist_ok=True)
os.makedirs(configs_dir, exist_ok=True)

entities = {
    'Video': '''using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Scripts.Data.Entities;

public sealed class Video
{
    public int Id { get; set; }
    public string VideoId { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string TitleLower { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ChannelName { get; set; } = string.Empty;
    public string ChannelNameLower { get; set; } = string.Empty;
    public DateOnly? UploadDate { get; set; }
    public DateTimeOffset? SyncedAt { get; set; }
    public JsonDocument? Metadata { get; set; }
    public string? TranslatedTitle { get; set; }
    public string? TranslatedDescription { get; set; }

    public ICollection<PlaylistVideo> PlaylistVideos { get; set; } = new List<PlaylistVideo>();
}
''',
    'Playlist': '''using System;
using System.Collections.Generic;

namespace Scripts.Data.Entities;

public sealed class Playlist
{
    public int Id { get; set; }
    public string PlaylistId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string TitleLower { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ChannelName { get; set; } = string.Empty;
    public string ChannelNameLower { get; set; } = string.Empty;

    public ICollection<PlaylistVideo> PlaylistVideos { get; set; } = new List<PlaylistVideo>();
}
''',
    'PlaylistVideo': '''using System;

namespace Scripts.Data.Entities;

public sealed class PlaylistVideo
{
    public int PlaylistId { get; set; }
    public int VideoId { get; set; }
    public int Position { get; set; }

    public Playlist Playlist { get; set; } = null!;
    public Video Video { get; set; } = null!;
}
''',
    'Project': '''using System;
using System.Collections.Generic;

namespace Scripts.Data.Entities;

public sealed class Project
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NameLower { get; set; } = string.Empty;

    public ICollection<Issue> Issues { get; set; } = new List<Issue>();
}
''',
    'Issue': '''using System;
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
''',
    'ExecutionLog': '''using System;
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
}
'''
}

configs = {
    'Video': '''using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Scripts.Data.Entities;

namespace Scripts.Data.Configuration;

internal sealed class VideoConfiguration : IEntityTypeConfiguration<Video>
{
    public void Configure(EntityTypeBuilder<Video> builder)
    {
        builder.ToTable("videos", "youtube");
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.VideoId).IsRequired();
        builder.HasIndex(x => x.VideoId).IsUnique();
        
        builder.Property(x => x.Url).IsRequired();
        builder.Property(x => x.Title).IsRequired();
        builder.Property(x => x.TitleLower).IsRequired();
        builder.Property(x => x.Description).IsRequired();
        builder.Property(x => x.ChannelName).IsRequired();
        builder.Property(x => x.ChannelNameLower).IsRequired();
        
        builder.Property(x => x.Metadata).HasColumnType("jsonb");
    }
}
''',
    'Playlist': '''using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Scripts.Data.Entities;

namespace Scripts.Data.Configuration;

internal sealed class PlaylistConfiguration : IEntityTypeConfiguration<Playlist>
{
    public void Configure(EntityTypeBuilder<Playlist> builder)
    {
        builder.ToTable("playlists", "youtube");
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.PlaylistId).IsRequired();
        builder.HasIndex(x => x.PlaylistId).IsUnique();
        
        builder.Property(x => x.Title).IsRequired();
        builder.Property(x => x.TitleLower).IsRequired();
        builder.Property(x => x.Description).IsRequired();
        builder.Property(x => x.ChannelName).IsRequired();
        builder.Property(x => x.ChannelNameLower).IsRequired();
    }
}
''',
    'PlaylistVideo': '''using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Scripts.Data.Entities;

namespace Scripts.Data.Configuration;

internal sealed class PlaylistVideoConfiguration : IEntityTypeConfiguration<PlaylistVideo>
{
    public void Configure(EntityTypeBuilder<PlaylistVideo> builder)
    {
        builder.ToTable("playlist_videos", "youtube");
        builder.HasKey(x => new { x.PlaylistId, x.VideoId });
        
        builder.HasOne(x => x.Playlist)
            .WithMany(x => x.PlaylistVideos)
            .HasForeignKey(x => x.PlaylistId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasOne(x => x.Video)
            .WithMany(x => x.PlaylistVideos)
            .HasForeignKey(x => x.VideoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
''',
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
        
        builder.HasOne(x => x.Project)
            .WithMany(x => x.Issues)
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.SetNull);
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
        
        builder.Property(x => x.TaskName).IsRequired();
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.Input).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.Output).HasColumnType("jsonb");
        
        builder.Property(x => x.Timestamp).HasDefaultValueSql("NOW()");
        
        builder.HasOne(x => x.Issue)
            .WithMany(x => x.ExecutionLogs)
            .HasForeignKey(x => x.IssueId)
            .OnDelete(DeleteBehavior.Cascade);
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

print("Entities and Configurations created.")
