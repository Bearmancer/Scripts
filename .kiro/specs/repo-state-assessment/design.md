# Design Document: Repository Assessment and Integration

## Overview

This design specifies the architecture and implementation approach for a comprehensive repository assessment and integration system that resolves build errors, consolidates documentation, establishes a Fibery data ingestion pipeline, and analyzes git history for commit squashing. The system operates within the existing Tier 1 EF Core migration context, maintaining strict boundaries to avoid interference with ongoing database migration work.

The solution consists of four primary subsystems:
1. **Build Error Resolution System** - Fixes Lingua library enum casing issues in LanguageIdentifier.cs
2. **Documentation Consolidation System** - Merges AGENTS.md, CURRENT_STATUS.md, and INDEX.md into a single source of truth
3. **Fibery Ingestion Pipeline** - ETL process to parse Fibery exports and load into PostgreSQL
4. **Git History Analysis System** - Analyzes commit history and proposes logical squash groupings

## Architecture

### High-Level Component Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                    Assessment Orchestrator                       │
│  (Coordinates execution of all subsystems and generates report)  │
└────────┬────────────┬────────────┬────────────┬─────────────────┘
         │            │            │            │
         ▼            ▼            ▼            ▼
┌────────────┐ ┌──────────┐ ┌──────────┐ ┌──────────────┐
│   Build    │ │   Doc    │ │  Fibery  │ │     Git      │
│   Fixer    │ │Consolidat│ │ Ingestion│ │   History    │
│            │ │   or     │ │ Pipeline │ │   Analyzer   │
└─────┬──────┘ └────┬─────┘ └────┬─────┘ └──────┬───────┘
      │             │             │              │
      ▼             ▼             ▼              ▼
┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────────┐
│Language  │ │REPOSITORY│ │PostgreSQL│ │GIT_SQUASH_   │
│Identifier│ │_STATUS.md│ │ Database │ │ANALYSIS.md   │
│   .cs    │ │          │ │          │ │              │
└──────────┘ └──────────┘ └──────────┘ └──────────────┘
```

### Technology Stack

| Layer          | Technology                               |
| -------------- | ---------------------------------------- |
| Language       | C# 13 (.NET 10)                          |
| Database       | PostgreSQL 18 via EF Core 10 + Npgsql 10 |
| Resilience     | Polly v8                                 |
| Logging        | Serilog (CompactJsonFormatter)           |
| Testing        | TUnit + FluentAssertions                 |
| File I/O       | System.IO with UTF-8 encoding            |
| Git Operations | LibGit2Sharp (read-only)                 |


## Component Design

### 1. Build Error Resolution System

**Purpose:** Fix Lingua library enum casing issues to achieve zero-error, zero-warning build.

**Components:**

#### 1.1 LanguageEnumFixer

**Responsibility:** Transform SCREAMING_SNAKE_CASE Lingua enum references to PascalCase.

**Implementation:**
```csharp
public sealed class LanguageEnumFixer
{
    private static readonly Dictionary<string, string> EnumMappings = new()
    {
        ["ENGLISH"] = "English",
        ["FRENCH"] = "French",
        ["GERMAN"] = "German",
        ["SPANISH"] = "Spanish",
        ["PORTUGUESE"] = "Portuguese",
        ["ITALIAN"] = "Italian",
        ["DUTCH"] = "Dutch",
        ["RUSSIAN"] = "Russian",
        ["CHINESE"] = "Chinese",
        ["JAPANESE"] = "Japanese",
        ["KOREAN"] = "Korean",
        ["ARABIC"] = "Arabic",
        ["HINDI"] = "Hindi",
        ["BENGALI"] = "Bengali",
        ["CATALAN"] = "Catalan",
        ["CZECH"] = "Czech",
        ["DANISH"] = "Danish",
        ["FINNISH"] = "Finnish",
        ["GREEK"] = "Greek",
        ["HUNGARIAN"] = "Hungarian",
        ["NORWEGIAN"] = "Norwegian",
        ["POLISH"] = "Polish",
        ["ROMANIAN"] = "Romanian",
        ["SLOVAK"] = "Slovak",
        ["SWEDISH"] = "Swedish",
        ["TURKISH"] = "Turkish",
        ["UKRAINIAN"] = "Ukrainian",
        ["VIETNAMESE"] = "Vietnamese",
        ["THAI"] = "Thai"
    };

    public string FixEnumReferences(string sourceCode)
    {
        string result = sourceCode;
        foreach (var (oldValue, newValue) in EnumMappings)
        {
            result = Regex.Replace(
                result,
                $@"\bLanguage\.{oldValue}\b",
                $"Language.{newValue}",
                RegexOptions.None
            );
        }
        return result;
    }
}
```


#### 1.2 NullComparisonFixer

**Responsibility:** Handle Language enum null comparisons by using nullable Language? type.

**Implementation:**
```csharp
public sealed class NullComparisonFixer
{
    public string FixNullComparisons(string sourceCode)
    {
        return Regex.Replace(
            sourceCode,
            @"Language\s+(\w+)\s*==\s*null",
            "Language? $1 == null",
            RegexOptions.None
        );
    }
}
```

#### 1.3 BuildValidator

**Responsibility:** Execute dotnet build and verify exit code 0 with zero warnings.

**Implementation:**
```csharp
public sealed class BuildValidator
{
    public async Task<BuildResult> ValidateBuildAsync(CancellationToken ct)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "build csharp/Scripts.slnx",
            WorkingDirectory = "C:\\Users\\Lance\\Dev\\Scripts",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        await process.WaitForExitAsync(ct);

        return new BuildResult(
            ExitCode: process.ExitCode,
            Output: await process.StandardOutput.ReadToEndAsync(ct),
            Errors: await process.StandardError.ReadToEndAsync(ct)
        );
    }
}
```


### 2. Documentation Consolidation System

**Purpose:** Create a single authoritative documentation file from multiple sources with conflict resolution.

**Components:**

#### 2.1 DocumentationSource

**Responsibility:** Represent a source document with metadata.

**Implementation:**
```csharp
public sealed record DocumentationSource(
    string FilePath,
    string Content,
    DateTimeOffset LastModified,
    IReadOnlyList<DocumentationSection> Sections
);

public sealed record DocumentationSection(
    string Title,
    string Content,
    int LineNumber
);
```

#### 2.2 DocumentationParser

**Responsibility:** Parse markdown files into structured sections.

**Implementation:**
```csharp
public sealed class DocumentationParser
{
    public DocumentationSource Parse(string filePath)
    {
        var content = File.ReadAllText(filePath, Encoding.UTF8);
        var fileInfo = new FileInfo(filePath);
        var sections = ExtractSections(content);

        return new DocumentationSource(
            FilePath: filePath,
            Content: content,
            LastModified: fileInfo.LastWriteTimeUtc,
            Sections: sections
        );
    }

    private IReadOnlyList<DocumentationSection> ExtractSections(string content)
    {
        var sections = new List<DocumentationSection>();
        var lines = content.Split('\n');
        var currentSection = new StringBuilder();
        string currentTitle = null;
        int currentLineNumber = 0;

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (line.StartsWith("##"))
            {
                if (currentTitle != null)
                {
                    sections.Add(new DocumentationSection(
                        currentTitle,
                        currentSection.ToString(),
                        currentLineNumber
                    ));
                }
                currentTitle = line.TrimStart('#').Trim();
                currentSection.Clear();
                currentLineNumber = i;
            }
            else
            {
                currentSection.AppendLine(line);
            }
        }

        if (currentTitle != null)
        {
            sections.Add(new DocumentationSection(
                currentTitle,
                currentSection.ToString(),
                currentLineNumber
            ));
        }

        return sections;
    }
}
```


#### 2.3 ConflictResolver

**Responsibility:** Resolve conflicts between documentation sources using timestamp authority.

**Implementation:**
```csharp
public sealed class ConflictResolver
{
    public DocumentationSection ResolveConflict(
        IReadOnlyList<DocumentationSection> conflictingSections,
        IReadOnlyList<DocumentationSource> sources)
    {
        var sectionsBySource = conflictingSections
            .Select((section, index) => (Section: section, Source: sources[index]))
            .OrderByDescending(x => x.Source.LastModified)
            .ToList();

        return sectionsBySource.First().Section;
    }
}
```

#### 2.4 DocumentationConsolidator

**Responsibility:** Merge multiple documentation sources into a single unified document.

**Implementation:**
```csharp
public sealed class DocumentationConsolidator
{
    private readonly DocumentationParser _parser;
    private readonly ConflictResolver _resolver;

    public DocumentationConsolidator(
        DocumentationParser parser,
        ConflictResolver resolver)
    {
        _parser = parser;
        _resolver = resolver;
    }

    public async Task<string> ConsolidateAsync(
        IReadOnlyList<string> sourceFilePaths,
        CancellationToken ct)
    {
        var sources = sourceFilePaths
            .Select(_parser.Parse)
            .ToList();

        var allSectionTitles = sources
            .SelectMany(s => s.Sections.Select(sec => sec.Title))
            .Distinct()
            .ToList();

        var consolidatedSections = new List<DocumentationSection>();

        foreach (var title in allSectionTitles)
        {
            var sectionsWithTitle = sources
                .SelectMany(s => s.Sections.Where(sec => sec.Title == title))
                .ToList();

            if (sectionsWithTitle.Count == 1)
            {
                consolidatedSections.Add(sectionsWithTitle[0]);
            }
            else
            {
                var resolved = _resolver.ResolveConflict(sectionsWithTitle, sources);
                consolidatedSections.Add(resolved);
            }
        }

        return BuildMarkdown(consolidatedSections);
    }

    private string BuildMarkdown(IReadOnlyList<DocumentationSection> sections)
    {
        var sb = new StringBuilder();
        foreach (var section in sections)
        {
            sb.AppendLine($"## {section.Title}");
            sb.AppendLine();
            sb.AppendLine(section.Content);
            sb.AppendLine();
        }
        return sb.ToString();
    }
}
```


#### 2.5 DeprecationNoticeWriter

**Responsibility:** Add deprecation notices to source documentation files.

**Implementation:**
```csharp
public sealed class DeprecationNoticeWriter
{
    public async Task WriteDeprecationNoticeAsync(
        string filePath,
        string newFilePath,
        CancellationToken ct)
    {
        var notice = $@"
> **DEPRECATED:** This file has been consolidated into [{newFilePath}]({newFilePath}).
> Please refer to the new location for the most current information.
> This file is retained for historical reference only.

";

        var existingContent = await File.ReadAllTextAsync(filePath, Encoding.UTF8, ct);
        var updatedContent = notice + existingContent;
        await File.WriteAllTextAsync(filePath, updatedContent, Encoding.UTF8, ct);
    }
}
```

### 3. Fibery Ingestion Pipeline

**Purpose:** Parse Fibery export files and load structured data into PostgreSQL.

**Components:**

#### 3.1 FiberyEntity (EF Core Entity)

**Responsibility:** Represent a Fibery entity in PostgreSQL.

**Implementation:**
```csharp
public sealed class FiberyEntity
{
    public Guid Id { get; init; }
    public required string FiberyId { get; init; }
    public required string EntityType { get; init; }
    public required JsonDocument RawData { get; init; }
    public DateTimeOffset ImportedAt { get; init; }
    public required string SourcePath { get; init; }
}
```

#### 3.2 FiberyEntityConfiguration

**Responsibility:** Configure EF Core mapping for FiberyEntity.

**Implementation:**
```csharp
public sealed class FiberyEntityConfiguration : IEntityTypeConfiguration<FiberyEntity>
{
    public void Configure(EntityTypeBuilder<FiberyEntity> builder)
    {
        builder.ToTable("fibery_entities");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.FiberyId)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(e => e.EntityType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.RawData)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(e => e.ImportedAt)
            .IsRequired();

        builder.Property(e => e.SourcePath)
            .IsRequired();

        builder.HasIndex(e => e.FiberyId)
            .IsUnique();

        builder.HasIndex(e => e.EntityType);
    }
}
```


#### 3.3 FiberyFileParser

**Responsibility:** Parse Fibery export files (markdown and JSON) and extract metadata.

**Implementation:**
```csharp
public sealed class FiberyFileParser
{
    public FiberyFileData Parse(string filePath, string fiberyRootPath)
    {
        var content = File.ReadAllText(filePath, Encoding.UTF8);
        var relativePath = Path.GetRelativePath(fiberyRootPath, filePath);
        var entityType = DetermineEntityType(relativePath);
        var fiberyId = ExtractFiberyId(content, filePath);

        return new FiberyFileData(
            FiberyId: fiberyId,
            EntityType: entityType,
            Content: content,
            SourcePath: relativePath
        );
    }

    private string DetermineEntityType(string relativePath)
    {
        var parts = relativePath.Split(Path.DirectorySeparatorChar);
        if (parts.Length < 2) return "Unknown";

        return parts[0] switch
        {
            "Knowledge" => "Guide",
            "Repos" => parts.Length > 1 ? parts[1] : "Unknown",
            _ => "Unknown"
        };
    }

    private string ExtractFiberyId(string content, string filePath)
    {
        var idMatch = Regex.Match(content, @"fibery-id:\s*([a-zA-Z0-9-]+)");
        if (idMatch.Success)
        {
            return idMatch.Groups[1].Value;
        }

        return Path.GetFileNameWithoutExtension(filePath);
    }
}

public sealed record FiberyFileData(
    string FiberyId,
    string EntityType,
    string Content,
    string SourcePath
);
```


#### 3.4 FiberyIngestionService

**Responsibility:** Orchestrate file parsing and database insertion with idempotency.

**Implementation:**
```csharp
public sealed class FiberyIngestionService
{
    private readonly IDbContextFactory<ScriptsDbContext> _contextFactory;
    private readonly FiberyFileParser _parser;
    private readonly ILogger<FiberyIngestionService> _logger;
    private readonly ResiliencePipeline _resiliencePipeline;

    public FiberyIngestionService(
        IDbContextFactory<ScriptsDbContext> contextFactory,
        FiberyFileParser parser,
        ILogger<FiberyIngestionService> logger,
        ResiliencePipeline resiliencePipeline)
    {
        _contextFactory = contextFactory;
        _parser = parser;
        _logger = logger;
        _resiliencePipeline = resiliencePipeline;
    }

    public async Task<IngestionResult> IngestAsync(
        string fiberyRootPath,
        CancellationToken ct)
    {
        var files = Directory.GetFiles(
            fiberyRootPath,
            "*.*",
            SearchOption.AllDirectories
        ).Where(f => f.EndsWith(".md") || f.EndsWith(".json"));

        int processed = 0;
        int inserted = 0;
        int updated = 0;
        int errors = 0;

        foreach (var file in files)
        {
            try
            {
                var fileData = _parser.Parse(file, fiberyRootPath);
                var upserted = await UpsertEntityAsync(fileData, ct);

                if (upserted)
                    updated++;
                else
                    inserted++;

                processed++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process file {FilePath}", file);
                errors++;
            }
        }

        return new IngestionResult(
            FilesProcessed: processed,
            RecordsInserted: inserted,
            RecordsUpdated: updated,
            Errors: errors
        );
    }

    private async Task<bool> UpsertEntityAsync(
        FiberyFileData fileData,
        CancellationToken ct)
    {
        return await _resiliencePipeline.ExecuteAsync(async ct =>
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            var existing = await context.FiberyEntities
                .FirstOrDefaultAsync(e => e.FiberyId == fileData.FiberyId, ct);

            if (existing != null)
            {
                await context.FiberyEntities
                    .Where(e => e.FiberyId == fileData.FiberyId)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(e => e.RawData, JsonDocument.Parse(fileData.Content))
                        .SetProperty(e => e.ImportedAt, DateTimeOffset.UtcNow)
                        .SetProperty(e => e.SourcePath, fileData.SourcePath),
                        ct);
                return true;
            }

            var entity = new FiberyEntity
            {
                Id = Guid.NewGuid(),
                FiberyId = fileData.FiberyId,
                EntityType = fileData.EntityType,
                RawData = JsonDocument.Parse(fileData.Content),
                ImportedAt = DateTimeOffset.UtcNow,
                SourcePath = fileData.SourcePath
            };

            context.FiberyEntities.Add(entity);
            await context.SaveChangesAsync(ct);
            return false;
        }, ct);
    }
}

public sealed record IngestionResult(
    int FilesProcessed,
    int RecordsInserted,
    int RecordsUpdated,
    int Errors
);
```


### 4. Git History Analysis System

**Purpose:** Analyze git commit history and propose logical squash groupings.

**Components:**

#### 4.1 GitCommit

**Responsibility:** Represent a git commit with metadata.

**Implementation:**
```csharp
public sealed record GitCommit(
    string Sha,
    string Message,
    DateTimeOffset Timestamp,
    string Author,
    bool IsMergeCommit
);
```

#### 4.2 CommitGroup

**Responsibility:** Represent a logical group of commits for squashing.

**Implementation:**
```csharp
public sealed record CommitGroup(
    string GroupPrefix,
    IReadOnlyList<GitCommit> Commits,
    string ProposedSquashMessage,
    string RebaseCommand
);
```

#### 4.3 GitHistoryReader

**Responsibility:** Read git commit history using LibGit2Sharp.

**Implementation:**
```csharp
public sealed class GitHistoryReader
{
    public IReadOnlyList<GitCommit> ReadRecentCommits(
        string repositoryPath,
        int count)
    {
        using var repo = new Repository(repositoryPath);
        var commits = repo.Commits
            .Take(count)
            .Select(c => new GitCommit(
                Sha: c.Sha,
                Message: c.MessageShort,
                Timestamp: c.Author.When,
                Author: c.Author.Name,
                IsMergeCommit: c.Parents.Count() > 1
            ))
            .ToList();

        return commits;
    }

    public (string HeadSha, string BranchName) GetCurrentState(string repositoryPath)
    {
        using var repo = new Repository(repositoryPath);
        return (repo.Head.Tip.Sha, repo.Head.FriendlyName);
    }

    public bool HasUncommittedChanges(string repositoryPath)
    {
        using var repo = new Repository(repositoryPath);
        return repo.RetrieveStatus().IsDirty;
    }
}
```


#### 4.4 CommitGrouper

**Responsibility:** Group commits by conventional commit prefixes and date proximity.

**Implementation:**
```csharp
public sealed class CommitGrouper
{
    private static readonly Regex ConventionalCommitPattern = 
        new(@"^(feat|fix|chore|docs|refactor|test|style|perf)\(([^)]+)\):", 
            RegexOptions.Compiled);

    public IReadOnlyList<CommitGroup> GroupCommits(
        IReadOnlyList<GitCommit> commits)
    {
        var groups = new List<CommitGroup>();
        var nonMergeCommits = commits.Where(c => !c.IsMergeCommit).ToList();

        var currentGroup = new List<GitCommit>();
        string currentPrefix = null;

        foreach (var commit in nonMergeCommits)
        {
            var match = ConventionalCommitPattern.Match(commit.Message);

            if (match.Success)
            {
                var prefix = $"{match.Groups[1].Value}({match.Groups[2].Value})";

                if (currentPrefix == prefix)
                {
                    currentGroup.Add(commit);
                }
                else
                {
                    if (currentGroup.Count > 1)
                    {
                        groups.Add(CreateCommitGroup(currentPrefix, currentGroup));
                    }

                    currentPrefix = prefix;
                    currentGroup = new List<GitCommit> { commit };
                }
            }
            else
            {
                if (currentGroup.Count > 1)
                {
                    groups.Add(CreateCommitGroup(currentPrefix, currentGroup));
                }

                currentPrefix = null;
                currentGroup = new List<GitCommit>();
            }
        }

        if (currentGroup.Count > 1)
        {
            groups.Add(CreateCommitGroup(currentPrefix, currentGroup));
        }

        return groups;
    }

    private CommitGroup CreateCommitGroup(
        string prefix,
        IReadOnlyList<GitCommit> commits)
    {
        var squashMessage = $"{prefix}: Consolidated {commits.Count} commits";
        var oldestSha = commits.Last().Sha;
        var newestSha = commits.First().Sha;
        var rebaseCommand = $"git rebase -i {oldestSha}~1";

        return new CommitGroup(
            GroupPrefix: prefix,
            Commits: commits,
            ProposedSquashMessage: squashMessage,
            RebaseCommand: rebaseCommand
        );
    }
}
```


#### 4.5 GitSquashAnalysisWriter

**Responsibility:** Generate GIT_SQUASH_ANALYSIS.md with proposed squash groups.

**Implementation:**
```csharp
public sealed class GitSquashAnalysisWriter
{
    public async Task WriteAnalysisAsync(
        string outputPath,
        IReadOnlyList<CommitGroup> groups,
        string headSha,
        string branchName,
        bool hasUncommittedChanges,
        CancellationToken ct)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Git Squash Analysis");
        sb.AppendLine();
        sb.AppendLine($"**Generated:** {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"**Current Branch:** {branchName}");
        sb.AppendLine($"**HEAD Commit:** {headSha}");
        sb.AppendLine($"**Uncommitted Changes:** {(hasUncommittedChanges ? "Yes" : "No")}");
        sb.AppendLine();

        sb.AppendLine("## Proposed Squash Groups");
        sb.AppendLine();

        foreach (var group in groups)
        {
            sb.AppendLine($"### Group: {group.GroupPrefix}");
            sb.AppendLine();
            sb.AppendLine($"**Commits:** {group.Commits.Count}");
            sb.AppendLine($"**Proposed Message:** {group.ProposedSquashMessage}");
            sb.AppendLine($"**Rebase Command:** `{group.RebaseCommand}`");
            sb.AppendLine();
            sb.AppendLine("**Commits in Group:**");
            sb.AppendLine();

            foreach (var commit in group.Commits)
            {
                sb.AppendLine($"- `{commit.Sha[..7]}` {commit.Message} ({commit.Timestamp:yyyy-MM-dd HH:mm})");
            }

            sb.AppendLine();
        }

        await File.WriteAllTextAsync(outputPath, sb.ToString(), Encoding.UTF8, ct);
    }
}
```


### 5. Assessment Orchestrator

**Purpose:** Coordinate execution of all subsystems and generate comprehensive assessment report.

**Components:**

#### 5.1 AssessmentOrchestrator

**Responsibility:** Execute all assessment tasks in sequence and collect results.

**Implementation:**
```csharp
public sealed class AssessmentOrchestrator
{
    private readonly LanguageEnumFixer _enumFixer;
    private readonly BuildValidator _buildValidator;
    private readonly DocumentationConsolidator _docConsolidator;
    private readonly FiberyIngestionService _fiberyIngestion;
    private readonly GitHistoryReader _gitReader;
    private readonly CommitGrouper _commitGrouper;
    private readonly ILogger<AssessmentOrchestrator> _logger;

    public async Task<AssessmentReport> ExecuteAssessmentAsync(CancellationToken ct)
    {
        _logger.LogInformation("Starting repository assessment");

        var buildResult = await FixBuildErrorsAsync(ct);
        var docResult = await ConsolidateDocumentationAsync(ct);
        var fiberyResult = await IngestFiberyDataAsync(ct);
        var gitResult = await AnalyzeGitHistoryAsync(ct);

        var report = new AssessmentReport(
            Timestamp: DateTimeOffset.UtcNow,
            BuildStatus: buildResult,
            DocumentationStatus: docResult,
            FiberyStatus: fiberyResult,
            GitAnalysisStatus: gitResult
        );

        await WriteAssessmentReportAsync(report, ct);

        return report;
    }

    private async Task<BuildStatus> FixBuildErrorsAsync(CancellationToken ct)
    {
        var languageIdentifierPath = 
            "C:\\Users\\Lance\\Dev\\Scripts\\csharp\\src\\Services\\Language\\LanguageIdentifier.cs";

        var sourceCode = await File.ReadAllTextAsync(languageIdentifierPath, Encoding.UTF8, ct);
        var fixedCode = _enumFixer.FixEnumReferences(sourceCode);

        await File.WriteAllTextAsync(languageIdentifierPath, fixedCode, Encoding.UTF8, ct);

        var buildResult = await _buildValidator.ValidateBuildAsync(ct);

        return new BuildStatus(
            Success: buildResult.ExitCode == 0,
            ExitCode: buildResult.ExitCode,
            ErrorCount: CountErrors(buildResult.Output),
            WarningCount: CountWarnings(buildResult.Output)
        );
    }

    private int CountErrors(string output) =>
        Regex.Matches(output, @"error\s+CS\d+:", RegexOptions.IgnoreCase).Count;

    private int CountWarnings(string output) =>
        Regex.Matches(output, @"warning\s+CS\d+:", RegexOptions.IgnoreCase).Count;
}
```


## Data Flow

### Build Error Resolution Flow

```
LanguageIdentifier.cs (SCREAMING_SNAKE_CASE)
    ↓
LanguageEnumFixer.FixEnumReferences()
    ↓
LanguageIdentifier.cs (PascalCase)
    ↓
BuildValidator.ValidateBuildAsync()
    ↓
BuildResult (ExitCode: 0, Errors: 0, Warnings: 0)
```

### Documentation Consolidation Flow

```
AGENTS.md + CURRENT_STATUS.md + INDEX.md
    ↓
DocumentationParser.Parse() (for each file)
    ↓
DocumentationSource[] (with sections and timestamps)
    ↓
ConflictResolver.ResolveConflict() (for conflicting sections)
    ↓
DocumentationConsolidator.ConsolidateAsync()
    ↓
REPOSITORY_STATUS.md (unified document)
    ↓
DeprecationNoticeWriter.WriteDeprecationNoticeAsync() (for each source)
    ↓
Source files with deprecation notices
```

### Fibery Ingestion Flow

```
fibery/ directory (markdown + JSON files)
    ↓
FiberyFileParser.Parse() (for each file)
    ↓
FiberyFileData[] (FiberyId, EntityType, Content, SourcePath)
    ↓
FiberyIngestionService.UpsertEntityAsync() (for each file)
    ↓
PostgreSQL fibery_entities table
    ↓
IngestionResult (FilesProcessed, RecordsInserted, RecordsUpdated, Errors)
```

### Git History Analysis Flow

```
Git Repository
    ↓
GitHistoryReader.ReadRecentCommits(50)
    ↓
GitCommit[] (Sha, Message, Timestamp, Author, IsMergeCommit)
    ↓
CommitGrouper.GroupCommits()
    ↓
CommitGroup[] (GroupPrefix, Commits, ProposedSquashMessage, RebaseCommand)
    ↓
GitSquashAnalysisWriter.WriteAnalysisAsync()
    ↓
GIT_SQUASH_ANALYSIS.md
```


## Error Handling Strategy

### Build Error Resolution

**Error Scenarios:**
- LanguageIdentifier.cs file not found → Log error, fail fast
- File read/write permission denied → Log error, fail fast
- Build process fails to start → Log error, fail fast
- Build returns non-zero exit code → Log details, continue to report

**Handling:**
```csharp
try
{
    var sourceCode = await File.ReadAllTextAsync(path, Encoding.UTF8, ct);
    var fixedCode = _enumFixer.FixEnumReferences(sourceCode);
    await File.WriteAllTextAsync(path, fixedCode, Encoding.UTF8, ct);
}
catch (FileNotFoundException ex)
{
    _logger.LogError(ex, "LanguageIdentifier.cs not found at {Path}", path);
    throw;
}
catch (UnauthorizedAccessException ex)
{
    _logger.LogError(ex, "Permission denied accessing {Path}", path);
    throw;
}
```

### Documentation Consolidation

**Error Scenarios:**
- Source file not found → Log warning, skip file
- Malformed markdown → Log warning, include raw content
- Write permission denied → Log error, fail fast

**Handling:**
```csharp
foreach (var sourcePath in sourceFilePaths)
{
    try
    {
        var source = _parser.Parse(sourcePath);
        sources.Add(source);
    }
    catch (FileNotFoundException ex)
    {
        _logger.LogWarning(ex, "Source file not found: {Path}", sourcePath);
    }
}
```

### Fibery Ingestion

**Error Scenarios:**
- Malformed JSON → Log error, skip file, continue
- Missing FiberyId → Use filename as fallback
- Database connection failure → Retry via Polly, then fail
- Duplicate key violation → Update existing record

**Handling:**
```csharp
foreach (var file in files)
{
    try
    {
        var fileData = _parser.Parse(file, fiberyRootPath);
        await UpsertEntityAsync(fileData, ct);
        processed++;
    }
    catch (JsonException ex)
    {
        _logger.LogError(ex, "Failed to parse JSON file {FilePath}", file);
        errors++;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected error processing {FilePath}", file);
        errors++;
    }
}
```

### Git History Analysis

**Error Scenarios:**
- Repository not found → Log error, fail fast
- LibGit2Sharp exception → Log error, fail fast
- No commits found → Log warning, return empty analysis

**Handling:**
```csharp
try
{
    using var repo = new Repository(repositoryPath);
    var commits = repo.Commits.Take(count).ToList();
    return commits;
}
catch (RepositoryNotFoundException ex)
{
    _logger.LogError(ex, "Git repository not found at {Path}", repositoryPath);
    throw;
}
```


## Database Schema

### fibery_entities Table

```sql
CREATE TABLE fibery_entities (
    id UUID PRIMARY KEY,
    fibery_id VARCHAR(255) NOT NULL UNIQUE,
    entity_type VARCHAR(100) NOT NULL,
    raw_data JSONB NOT NULL,
    imported_at TIMESTAMPTZ NOT NULL,
    source_path TEXT NOT NULL
);

CREATE INDEX idx_fibery_entities_entity_type ON fibery_entities(entity_type);
CREATE UNIQUE INDEX idx_fibery_entities_fibery_id ON fibery_entities(fibery_id);
```

**Rationale:**
- `id` as UUID primary key for internal referencing
- `fibery_id` as unique identifier from Fibery export
- `entity_type` indexed for filtering by type (Guide, Issue, Project, etc.)
- `raw_data` as JSONB for flexible schema and queryability
- `imported_at` for tracking import history
- `source_path` for traceability back to source file

### EF Core Migration

The migration will be generated using:
```powershell
dotnet ef migrations add AddFiberyEntitiesTable `
    --project csharp/src/Data/Scripts.Data.csproj `
    --startup-project csharp/src/CLI/Scripts.CLI.csproj
```

## Integration with Existing Tier 1 Work

### Constraints

1. **No modifications to existing EF Core entities** - Only add FiberyEntity
2. **No modifications to existing migrations** - Only add new migration for fibery_entities
3. **Use existing ScriptsDbContext** - Add FiberyEntity DbSet
4. **Use existing IDbContextFactory pattern** - No new database access patterns
5. **Use existing Polly resilience policies** - Reuse RepositoryResilienceFactory
6. **Use existing Serilog configuration** - No logging changes
7. **Only modify LanguageIdentifier.cs in src/Data/** - No other Data/ files touched

### ScriptsDbContext Extension

```csharp
public sealed class ScriptsDbContext : DbContext
{
    public DbSet<Artist> Artists => Set<Artist>();
    public DbSet<Album> Albums => Set<Album>();
    public DbSet<Track> Tracks => Set<Track>();
    public DbSet<Scrobble> Scrobbles => Set<Scrobble>();
    public DbSet<Video> Videos => Set<Video>();
    public DbSet<ExecutionLog> ExecutionLogs => Set<ExecutionLog>();
    public DbSet<FailedTask> FailedTasks => Set<FailedTask>();
    public DbSet<SourceRecord> SourceRecords => Set<SourceRecord>();
    public DbSet<FiberyEntity> FiberyEntities => Set<FiberyEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ScriptsDbContext).Assembly);
    }
}
```


## Testing Strategy

### Unit Tests

**Build Error Resolution:**
- Test LanguageEnumFixer with all 29 enum values
- Test NullComparisonFixer with various null comparison patterns
- Test BuildValidator exit code parsing

**Documentation Consolidation:**
- Test DocumentationParser with valid and malformed markdown
- Test ConflictResolver with multiple conflicting sections
- Test DocumentationConsolidator with 3 source files
- Test DeprecationNoticeWriter output format

**Fibery Ingestion:**
- Test FiberyFileParser with markdown and JSON files
- Test entity type determination from directory structure
- Test FiberyId extraction from file content and metadata
- Test idempotency (re-ingesting same file updates, not duplicates)

**Git History Analysis:**
- Test CommitGrouper with conventional commit prefixes
- Test CommitGrouper with non-conventional commits
- Test merge commit exclusion
- Test consecutive commit grouping

### Integration Tests

**Fibery Ingestion:**
- Test full ingestion pipeline with Testcontainers PostgreSQL
- Test ExecuteUpdateAsync for upsert operations
- Test Polly retry on transient database failures
- Test UTF-8 encoding for files with special characters

**Build Validation:**
- Test actual dotnet build execution
- Test exit code capture
- Test error and warning counting

### Property-Based Tests

**Documentation Consolidation:**
- Property: For any set of conflicting sections with timestamps, the most recent should be selected
- Property: For any markdown file, parsing should extract all sections

**Fibery Ingestion:**
- Property: For any markdown file in fibery/, it should be parsed successfully
- Property: For any JSON file in fibery/, it should be parsed successfully
- Property: For any FiberyId, re-ingesting should update the existing record, not create a duplicate
- Property: For any file read operation, UTF-8 encoding should be used

**Git History Analysis:**
- Property: For any commits with identical prefixes, they should be grouped together
- Property: For any merge commit, it should be excluded from squash groups
- Property: For any commit group, the total count should equal the number of commits in the group


## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system—essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: Conflict Resolution Timestamp Authority

*For any* set of documentation sections with conflicting titles from multiple sources, the consolidated output SHALL contain the section from the source with the most recent LastModified timestamp.

**Validates: Requirements 2.3**

### Property 2: Markdown File Parsing Completeness

*For any* markdown file in the fibery/ directory, the FiberyFileParser SHALL successfully parse the file and extract FiberyId, EntityType, and Content without throwing exceptions.

**Validates: Requirements 4.1**

### Property 3: JSON File Parsing Completeness

*For any* JSON file in the fibery/ directory, the FiberyFileParser SHALL successfully parse the file and extract FiberyId, EntityType, and Content without throwing exceptions.

**Validates: Requirements 4.2**

### Property 4: FiberyId Extraction Consistency

*For any* Fibery entity file, the FiberyFileParser SHALL extract a non-empty FiberyId either from file metadata, file content, or filename as fallback.

**Validates: Requirements 4.3**

### Property 5: EntityType Determination from Path

*For any* Fibery entity file, the FiberyFileParser SHALL determine EntityType based on the directory structure of the file's path relative to fibery/ root.

**Validates: Requirements 4.4**

### Property 6: Complete Content Storage

*For any* ingested Fibery file, the RawData JSONB column SHALL contain the complete file content without truncation or data loss.

**Validates: Requirements 4.5**

### Property 7: Relative Path Recording

*For any* ingested Fibery file, the SourcePath column SHALL contain the correct relative path from the fibery/ directory root.

**Validates: Requirements 4.6**

### Property 8: UTC Timestamp Recording

*For any* ingested Fibery file, the ImportedAt column SHALL contain a valid UTC timestamp representing the time of ingestion.

**Validates: Requirements 4.7**

### Property 9: Idempotent Upsert Behavior

*For any* FiberyId, re-ingesting a file with that FiberyId SHALL update the existing record rather than creating a duplicate, maintaining exactly one record per FiberyId.

**Validates: Requirements 4.8**


### Property 10: Error Logging and Continuation

*For any* file that causes a parsing error during ingestion, the error SHALL be logged with file path and error details, and processing SHALL continue with remaining files.

**Validates: Requirements 4.9**

### Property 11: Completion Report Metrics

*For any* ingestion run, the completion report SHALL include accurate counts of FilesProcessed, RecordsInserted, RecordsUpdated, and Errors that sum correctly.

**Validates: Requirements 4.13**

### Property 12: UTF-8 Encoding Consistency

*For any* file read operation during Fibery ingestion, UTF-8 encoding SHALL be used to prevent encoding-related data corruption.

**Validates: Requirements 4.15**

### Property 13: Commit Grouping by Prefix

*For any* sequence of consecutive commits with identical conventional commit prefixes (e.g., "feat(t1-12)"), they SHALL be grouped into a single CommitGroup.

**Validates: Requirements 5.2, 5.4**

### Property 14: Merge Commit Exclusion

*For any* commit with more than one parent (merge commit), it SHALL NOT appear in any squash group.

**Validates: Requirements 5.3**

### Property 15: Commit Group Metadata Completeness

*For any* identified CommitGroup, it SHALL include all commit SHAs, messages, and author timestamps for every commit in the group.

**Validates: Requirements 5.5**

### Property 16: Squash Message Generation

*For any* CommitGroup, a proposed squash commit message SHALL be generated that summarizes the grouped work.

**Validates: Requirements 5.7**

### Property 17: Standalone Commit Identification

*For any* commit matching standalone criteria (milestone, sign-off, merge), it SHALL NOT be included in any squash group.

**Validates: Requirements 5.8**

### Property 18: Commit Count Accuracy

*For any* CommitGroup, the total commit count SHALL equal the number of commits in the Commits collection.

**Validates: Requirements 5.9**

### Property 19: Chronological Order Preservation

*For any* CommitGroup, commits SHALL be ordered chronologically by their Timestamp property.

**Validates: Requirements 5.10**


### Property 20: Fallback Grouping by Proximity

*For any* commits without conventional commit prefixes, they SHALL be grouped by date proximity and content similarity rather than being left ungrouped.

**Validates: Requirements 5.11**

### Property 21: Rebase Command Generation

*For any* CommitGroup, a git rebase -i command template SHALL be generated that targets the correct commit range.

**Validates: Requirements 5.14**

### Property 22: Validation Failure Documentation

*For any* validation failure during assessment, the failure reason SHALL be documented in the ASSESSMENT_REPORT.md file.

**Validates: Requirements 7.11**

## Implementation Approach

### Phase 1: Build Error Resolution (Day 1)

1. Implement LanguageEnumFixer with all 29 enum mappings
2. Implement NullComparisonFixer for nullable Language? handling
3. Implement BuildValidator for exit code verification
4. Write unit tests for all fixers
5. Apply fixes to LanguageIdentifier.cs
6. Verify dotnet build Scripts.slnx returns exit code 0

### Phase 2: Documentation Consolidation (Day 1-2)

1. Implement DocumentationParser with section extraction
2. Implement ConflictResolver with timestamp-based authority
3. Implement DocumentationConsolidator with merge logic
4. Implement DeprecationNoticeWriter
5. Write unit tests for all components
6. Generate REPOSITORY_STATUS.md
7. Add deprecation notices to source files

### Phase 3: Fibery Migration Documentation (Day 2)

1. Analyze fibery/ directory structure
2. Identify all entity types (Guide, Execution Logs, Issue, Project)
3. Document file formats (markdown, JSON)
4. Specify PostgreSQL schema for fibery_entities table
5. Document parsing requirements for each entity type
6. Document idempotency and error handling requirements
7. Create FIBERY_MIGRATION.md with all specifications

### Phase 4: Fibery Ingestion Pipeline (Day 2-3)

1. Create FiberyEntity EF Core entity
2. Create FiberyEntityConfiguration
3. Generate EF Core migration for fibery_entities table
4. Implement FiberyFileParser with ID extraction and type determination
5. Implement FiberyIngestionService with upsert logic
6. Write unit tests for parser and service
7. Write integration tests with Testcontainers
8. Execute ingestion and verify results


### Phase 5: Git History Analysis (Day 3)

1. Add LibGit2Sharp NuGet package
2. Implement GitHistoryReader for commit retrieval
3. Implement CommitGrouper with conventional commit pattern matching
4. Implement GitSquashAnalysisWriter
5. Write unit tests for grouping logic
6. Generate GIT_SQUASH_ANALYSIS.md

### Phase 6: Assessment Report Generation (Day 3-4)

1. Implement AssessmentOrchestrator
2. Coordinate execution of all subsystems
3. Collect results from each subsystem
4. Generate ASSESSMENT_REPORT.md with:
   - Executive summary
   - Build status
   - Documentation consolidation status
   - Fibery migration status
   - Git history analysis status
   - Test execution results
   - Remaining blockers
   - Recommended next actions
   - Timestamps and assumptions

### Phase 7: Validation and Verification (Day 4)

1. Execute dotnet build Scripts.slnx and verify exit code 0
2. Execute dotnet test Scripts.slnx and verify all tests pass
3. Verify REPOSITORY_STATUS.md exists with all required sections
4. Verify FIBERY_MIGRATION.md exists with all entity types
5. Verify GIT_SQUASH_ANALYSIS.md exists with squash proposals
6. Verify ASSESSMENT_REPORT.md exists with all required sections
7. Verify fibery_entities table exists in PostgreSQL
8. Verify at least one record inserted into fibery_entities
9. Verify git log command executes successfully
10. Verify all deprecated files contain deprecation notices
11. Generate validation checklist with pass/fail status

## Dependencies

### NuGet Packages

- **LibGit2Sharp** (v0.30.0+) - Git repository operations
- **System.Text.Json** (built-in) - JSON parsing for Fibery files
- **Microsoft.EntityFrameworkCore** (10.0.x) - Already present
- **Npgsql.EntityFrameworkCore.PostgreSQL** (10.0.x) - Already present
- **Polly** (v8.x) - Already present
- **Serilog** (v4.x) - Already present

### External Dependencies

- PostgreSQL 18 database (via Docker Compose)
- .NET 10 SDK
- Git repository at C:\Users\Lance\Dev\Scripts


## Risk Mitigation

### Risk 1: Build Errors Persist After Enum Fix

**Mitigation:**
- Comprehensive unit tests for LanguageEnumFixer covering all 29 enum values
- Manual verification of LanguageIdentifier.cs after fix
- BuildValidator integration test to confirm zero errors and warnings

### Risk 2: Documentation Conflicts Not Resolved Correctly

**Mitigation:**
- Unit tests for ConflictResolver with multiple timestamp scenarios
- Manual review of REPOSITORY_STATUS.md before deprecating source files
- Backup source files before adding deprecation notices

### Risk 3: Fibery Data Loss During Ingestion

**Mitigation:**
- Property-based tests for complete content storage
- Integration tests verifying RawData contains full file content
- Dry-run mode for ingestion to preview changes before committing

### Risk 4: Database Migration Conflicts with Existing Tier 1 Work

**Mitigation:**
- Only add new fibery_entities table, no modifications to existing tables
- Use existing ScriptsDbContext pattern
- Test migration application on clean database before production

### Risk 5: Git History Analysis Modifies Repository

**Mitigation:**
- Use LibGit2Sharp in read-only mode
- No git write operations in analysis code
- Verification test to confirm git history unchanged after analysis

### Risk 6: Performance Issues with Large Fibery Exports

**Mitigation:**
- Process files in batches if directory contains >1000 files
- Use ExecuteUpdateAsync for efficient upserts
- Polly retry policies for transient database failures
- Progress logging every 100 files

## Success Criteria

1. ✅ dotnet build Scripts.slnx returns exit code 0
2. ✅ dotnet test Scripts.slnx shows 100% pass rate
3. ✅ REPOSITORY_STATUS.md exists with all required sections
4. ✅ FIBERY_MIGRATION.md documents all entity types and schema
5. ✅ GIT_SQUASH_ANALYSIS.md contains proposed squash groups
6. ✅ ASSESSMENT_REPORT.md contains comprehensive assessment results
7. ✅ fibery_entities table exists in PostgreSQL with correct schema
8. ✅ At least one Fibery record successfully ingested
9. ✅ All deprecated documentation files contain deprecation notices
10. ✅ No modifications to existing EF Core entities or migrations
11. ✅ No modifications to files outside allowed scope
12. ✅ Git history remains unchanged (read-only analysis)


## Appendix A: EF Core 10 Patterns

### ExecuteUpdateAsync Pattern (Required for Upserts)

```csharp
await context.FiberyEntities
    .Where(e => e.FiberyId == fileData.FiberyId)
    .ExecuteUpdateAsync(setters => setters
        .SetProperty(e => e.RawData, JsonDocument.Parse(fileData.Content))
        .SetProperty(e => e.ImportedAt, DateTimeOffset.UtcNow)
        .SetProperty(e => e.SourcePath, fileData.SourcePath),
        ct);
```

### IDbContextFactory Pattern (Required for Services)

```csharp
public sealed class FiberyIngestionService
{
    private readonly IDbContextFactory<ScriptsDbContext> _contextFactory;

    public async Task ProcessAsync(CancellationToken ct)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
    }
}
```

### Polly v8 Resilience Pattern (Required for Database Operations)

```csharp
private readonly ResiliencePipeline _resiliencePipeline;

public async Task<T> ExecuteWithRetryAsync<T>(
    Func<CancellationToken, Task<T>> operation,
    CancellationToken ct)
{
    return await _resiliencePipeline.ExecuteAsync(operation, ct);
}
```

## Appendix B: File Paths Reference

### Source Files

- `C:\Users\Lance\Dev\Scripts\AGENTS.md`
- `C:\Users\Lance\Dev\Scripts\AI\plans\CURRENT_STATUS.md`
- `C:\Users\Lance\Dev\Scripts\AI\plans\INDEX.md`
- `C:\Users\Lance\Dev\Scripts\csharp\src\Services\Language\LanguageIdentifier.cs`
- `C:\Users\Lance\Dev\Scripts\fibery\` (directory)

### Generated Files

- `C:\Users\Lance\Dev\Scripts\AI\plans\REPOSITORY_STATUS.md`
- `C:\Users\Lance\Dev\Scripts\AI\plans\FIBERY_MIGRATION.md`
- `C:\Users\Lance\Dev\Scripts\AI\plans\GIT_SQUASH_ANALYSIS.md`
- `C:\Users\Lance\Dev\Scripts\AI\plans\ASSESSMENT_REPORT.md`

### Database Files

- `C:\Users\Lance\Dev\Scripts\csharp\src\Data\Entities\FiberyEntity.cs`
- `C:\Users\Lance\Dev\Scripts\csharp\src\Data\Configuration\FiberyEntityConfiguration.cs`
- `C:\Users\Lance\Dev\Scripts\csharp\src\Data\Migrations\<timestamp>_AddFiberyEntitiesTable.cs`

### Test Files

- `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Fibery\FiberyFileParserTests.cs`
- `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Fibery\FiberyIngestionServiceTests.cs`
- `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Documentation\DocumentationConsolidatorTests.cs`
- `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Git\CommitGrouperTests.cs`

