# Phase 4: Shared Infrastructure Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Verify diacritic stripping, case normalization, and environment-based DbContext registration rules.

**Architecture:** Add and run xUnit tests.

**Tech Stack:** C#, xUnit, EF Core, Microsoft.Extensions.DependencyInjection

---

### Task 4.1: Verify TextNormalizer.ToStorageKey removes diacritics

**Files:**
- Test: `csharp/src/Tests/TextNormalizerTests.cs`

- [ ] **Step 1: Verify the test already exists in `TextNormalizerTests.cs`**

**Pre/Post context for `csharp/src/Tests/TextNormalizerTests.cs`:**
```csharp
using CSharpScripts.Data;

namespace CSharpScripts.Tests;

public class TextNormalizerTests
{
	[Fact]
	public void ToStorageKey_RemovesDiacritics() =>
		Assert.Equal("bjork", TextNormalizer.ToStorageKey("björk"));

	[Fact]
	public void ToStorageKey_LowercasesAndTrims() =>
		Assert.Equal("sigur ros", TextNormalizer.ToStorageKey("  SIGUR rÓs  "));
}
```

- [ ] **Step 2: Run the test to verify it passes**

Run: `dotnet test --filter TextNormalizerTests.ToStorageKey_RemovesDiacritics`
Expected: PASS

- [ ] **Step 3: Commit**

```bash
git commit --allow-empty -m "test: verify TextNormalizer diacritic stripping test is present and passing"
```

---

### Task 4.2: Verify TextNormalizer.ToStorageKey lowercases and trims

**Files:**
- Test: `csharp/src/Tests/TextNormalizerTests.cs`

- [ ] **Step 1: Verify the test already exists in `TextNormalizerTests.cs`**

Refer to the context in Task 4.1.

- [ ] **Step 2: Run the test to verify it passes**

Run: `dotnet test --filter TextNormalizerTests.ToStorageKey_LowercasesAndTrims`
Expected: PASS

- [ ] **Step 3: Commit**

```bash
git commit --allow-empty -m "test: verify TextNormalizer lowercase and trim test is present and passing"
```

---

### Task 4.3: Verify DbContextRegistration throws when PGCONNSTR empty

**Files:**
- Create Test: `csharp/src/Tests/Data/DbContextRegistrationTests.cs`

- [ ] **Step 1: Write the failing/passing test**

Create `csharp/src/Tests/Data/DbContextRegistrationTests.cs` with the following content:
```csharp
using System;
using CSharpScripts.Data;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CSharpScripts.Tests.Data;

public class DbContextRegistrationTests
{
	[Fact]
	public void AddScriptsDbContext_Throws_When_PGCONNSTR_NotSet()
	{
		// Save original value
		var originalConnStr = Environment.GetEnvironmentVariable("PGCONNSTR");
		try
		{
			Environment.SetEnvironmentVariable("PGCONNSTR", null);
			var services = new ServiceCollection();

			var ex = Assert.Throws<InvalidOperationException>(() => services.AddScriptsDbContext());
			Assert.Contains("PGCONNSTR environment variable is not set", ex.Message);
		}
		finally
		{
			// Restore original value
			Environment.SetEnvironmentVariable("PGCONNSTR", originalConnStr);
		}
	}
}
```

- [ ] **Step 2: Run the test to verify it passes**

Run: `dotnet test --filter DbContextRegistrationTests.AddScriptsDbContext_Throws_When_PGCONNSTR_NotSet`
Expected: PASS

- [ ] **Step 3: Commit**

```bash
git add csharp/src/Tests/Data/DbContextRegistrationTests.cs
git commit -m "test: verify DbContextRegistration throws when PGCONNSTR is not set"
```
