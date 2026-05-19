# Phase 6: DbContext Configuration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Assert and verify that the `ScriptsDbContext` configuration (NoTracking, configurations assembly loading, and Videos DbSet) is correct.

**Architecture:** Create an xUnit test file that instantiates the DbContext with an in-memory/sqlite or mocked provider and queries its configurations and state.

**Tech Stack:** C#, xUnit, EF Core, SQLite In-Memory

---

### Task 6.1: Add DbContext Configuration Tests

**Files:**
- Create Test: `csharp/src/Tests/Data/ScriptsDbContextTests.cs`

- [ ] **Step 1: Write the test code**

Create `csharp/src/Tests/Data/ScriptsDbContextTests.cs` with the following content:
```csharp
using CSharpScripts.Data;
using CSharpScripts.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CSharpScripts.Tests.Data;

public class ScriptsDbContextTests
{
	[Fact]
	public void DbContext_Defaults_To_NoTracking()
	{
		var options = new DbContextOptionsBuilder<ScriptsDbContext>()
			.UseInMemoryDatabase(databaseName: "test_notracking")
			.Options;

		using var ctx = new ScriptsDbContext(options);
		Assert.Equal(QueryTrackingBehavior.NoTracking, ctx.ChangeTracker.QueryTrackingBehavior);
	}

	[Fact]
	public void DbContext_Has_Videos_DbSet()
	{
		var options = new DbContextOptionsBuilder<ScriptsDbContext>()
			.UseInMemoryDatabase(databaseName: "test_videos_dbset")
			.Options;

		using var ctx = new ScriptsDbContext(options);
		Assert.NotNull(ctx.Videos);
		Assert.IsAssignableFrom<DbSet<Video>>(ctx.Videos);
	}

	[Fact]
	public void DbContext_ModelBuilder_Applies_Configurations_From_Assembly()
	{
		var options = new DbContextOptionsBuilder<ScriptsDbContext>()
			.UseInMemoryDatabase(databaseName: "test_model_creating")
			.Options;

		using var ctx = new ScriptsDbContext(options);
		
		// If configurations are applied, we should see configured entities in the model
		var entityType = ctx.Model.FindEntityType(typeof(Artist));
		Assert.NotNull(entityType);
	}
}
```

- [ ] **Step 2: Run tests to verify they pass**

Run: `dotnet test --filter ScriptsDbContextTests`
Expected: PASS

- [ ] **Step 3: Commit**

```bash
git add csharp/src/Tests/Data/ScriptsDbContextTests.cs
git commit -m "test: add unit tests for ScriptsDbContext configuration"
```
