# Phase 11: Optimization — Compiled Model Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Enable and generate EF Core compiled models to optimize DBContext startup performance.

**Architecture:** Enable the build-time optimization setting in `.csproj` and run the `dotnet ef dbcontext optimize` command.

**Tech Stack:** C#, EF Core CLI

---

### Task 11.1: Add EFOptimizeContext to csproj

**Files:**
- Modify: `csharp/CSharpScripts.csproj`

- [ ] **Step 1: Edit project configuration**

**Pre-modification code chunk for `csharp/CSharpScripts.csproj`:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
	<PropertyGroup>
		<OutputType>Library</OutputType>
		<PackageId>CSharpScripts</PackageId>
		<AssemblyName>tools</AssemblyName>
		<PublishSingleFile>true</PublishSingleFile>
		<SelfContained>false</SelfContained>
		<TreatWarningsAsErrors>true</TreatWarningsAsErrors>
	</PropertyGroup>
```

**Post-modification code chunk for `csharp/CSharpScripts.csproj` (enabling EF optimization):**
```xml
<Project Sdk="Microsoft.NET.Sdk">
	<PropertyGroup>
		<OutputType>Library</OutputType>
		<PackageId>CSharpScripts</PackageId>
		<AssemblyName>tools</AssemblyName>
		<PublishSingleFile>true</PublishSingleFile>
		<SelfContained>false</SelfContained>
		<TreatWarningsAsErrors>true</TreatWarningsAsErrors>
		<EFOptimizeContext>true</EFOptimizeContext>
	</PropertyGroup>
```

- [ ] **Step 2: Verify the build still compiles**

Run: `dotnet build "C:\Users\Lance\Dev\Scripts\csharp\CSharpScripts.csproj"`
Expected: Build passes with 0 errors.

- [ ] **Step 3: Commit**

```bash
git add csharp/CSharpScripts.csproj
git commit -m "build: enable EFOptimizeContext for EF Core compiled models"
```

---

### Task 11.2: Generate compiled model files

**Files:**
- Create: `csharp/CompiledModels/` (generated output files)

- [ ] **Step 1: Generate EF compiled model**

Run:
```powershell
dotnet ef dbcontext optimize --project "C:\Users\Lance\Dev\Scripts\csharp\CSharpScripts.csproj" --output-dir src/Data/CompiledModels --namespace CSharpScripts.Data.CompiledModels
```

- [ ] **Step 2: Assert CompiledModels files exist**

Verify the `src/Data/CompiledModels` folder is generated and contains several C# files (e.g. `ScriptsDbContextModel.cs`, etc.).

- [ ] **Step 3: Commit**

```bash
git add csharp/src/Data/CompiledModels/
git commit -m "perf: generate and add EF compiled model files"
```

---

### Task 11.3: Verify project build with compiled models auto-detection

- [ ] **Step 1: Rebuild the project**

Run: `dotnet build "C:\Users\Lance\Dev\Scripts\csharp\CSharpScripts.csproj"`
Expected: Build passes. (Note: EF Core 9+ auto-detects compiled models in the project, so no manual `.UseModel()` invocation is needed).
