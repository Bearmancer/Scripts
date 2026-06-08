# ADR 0002: PostgreSQL MCP Server Architecture

## Status

**Proposed.** Architecture design complete. v0.3 implementation exists (3 tools, 2 resources). Remaining tools, resources, and prompts pending implementation by mcp-implementer and mcp-integrator.

### Implementation Progress

| Phase                   | Status        | Tools                              | Resources                  | Prompts |
| ----------------------- | ------------- | ---------------------------------- | -------------------------- | ------- |
| Phase 1: Core + Schema  | ⚡ Partial     | `query_execute`, `schema_describe` | `EntityMetadataResource`   | —       |
| Phase 2: Health + Admin | ⚡ Partial     | `migration_validate`               | `ConnectionStatusResource` | —       |
| Phase 3: Data Access    | ❌ Not started | T05-T09                            | R03-R06, R08, R10          | P02-P03 |
| Phase 4: Operations     | ❌ Not started | T10-T12                            | R09                        | P04-P05 |

## Date

2026-06-06

## Context

The Scripts repository manages data from three external APIs (YouTube, Last.fm, Fibery) stored in a single PostgreSQL database with four schemas (`youtube`, `music`, `fibery`, `public`). The EF Core data layer ([ADR 0001](./0001-ef-core-3-schema-architecture.md)) uses compiled models and repositories for data access. AI agents need a standardized way to interact with this database — to query data, explore schemas, run sync operations, and analyze patterns — without needing to know SQL or the EF Core internals.

The Model Context Protocol (MCP) is the open standard for connecting AI applications to external tools and data sources. An MCP server wrapping the PostgreSQL database enables any MCP-compatible client (Claude Desktop, Cursor, opencode, etc.) to interact with the database through a well-defined API surface.

### Why a custom MCP server instead of a generic PostgreSQL MCP server?

Existing PostgreSQL MCP servers (e.g., `stuzero/pg-mcp-server`, the reference Postgres MCP implementation) provide generic SQL access and schema introspection. A custom server adds:

1. **Domain-aware entity access**: AI agents query `music.artists` or `fibery.fibery_entities` with semantic understanding, not raw SQL.
2. **EF Core compiled model integration**: The server reuses the compiled EF Core model for fast, type-safe data access without duplicating schema knowledge.
3. **Repository-based operations**: Read/write operations go through the existing repository layer with resilience policies (Polly retry, rate limiting).
4. **Sync orchestration**: AI agents can trigger Last.fm scrobble syncs and YouTube playlist operations that already exist in the CLI.
5. **Schema-specific permissions**: The MCP server can expose read-only access to `music` while allowing read/write to `public`, leveraging PostgreSQL schema-level grants.

## Decision

Build a **.NET-based MCP server** using the `ModelContextProtocol` NuGet package that wraps the existing `ScriptsDbContext` (with compiled models) and repository layer. The server runs as a standalone process, communicates via stdio or SSE transport, and is configured via the same environment variables and connection strings as the existing C# project.

### Architecture Layers

```
┌─────────────────────────────────────────────────────────────┐
│                    MCP Client (AI Agent)                      │
│  (Claude Desktop / Cursor / opencode / VS Code)              │
└─────────────────────┬───────────────────────────────────────┘
                      │ JSON-RPC 2.0 (stdio or SSE)
┌─────────────────────▼───────────────────────────────────────┐
│              PostgreSQL MCP Server (.NET)                     │
│                                                               │
│  ┌───────────────────────────────────────────────────────┐  │
│  │              MCP Transport Layer                        │  │
│  │  StdioServerTransport / SseServerTransport             │  │
│  └────────────────────────┬──────────────────────────────┘  │
│  ┌────────────────────────▼──────────────────────────────┐  │
│  │              MCP Server Core                           │  │
│  │  Host.CreateApplicationBuilder → AddMcpServer()        │  │
│  │  .WithStdioServerTransport()                          │  │
│  │  .WithToolsFromAssembly() (tools, resources, prompts)  │  │
│  └────────┬──────────────────────────────┬───────────────┘  │
│           │                              │                   │
│  ┌────────▼──────────┐    ┌──────────────▼───────────────┐  │
│  │   Tool Handlers    │    │   Resource Handlers          │  │
│  │  (14 tools total)  │    │   (10 resource templates)    │  │
│  │  3 implemented     │    │  2 implemented               │  │
│  └────────┬──────────┘    └──────────────┬───────────────┘  │
│           │                              │                   │
│  ┌────────▼──────────────────────────────▼───────────────┐  │
│  │              Service Layer                              │  │
│  │  SchemaService, QueryService, SyncService,             │  │
│  │  SearchService, HealthService                          │  │
│  └────────┬──────────────────────────────────────────────┘  │
│           │                                                   │
│  ┌────────▼──────────────────────────────────────────────┐  │
│  │              Data Access Layer                          │  │
│  │  ScriptsDbContext (compiled model) + Repositories      │  │
│  │  (Album, Artist, Track, Scrobble, Video — 5 total)    │  │
│  └────────┬──────────────────────────────────────────────┘  │
└───────────┼──────────────────────────────────────────────────┘
            │ Npgsql (via PGCONNSTR)
┌───────────▼──────────────────────────────────────────────────┐
│                 PostgreSQL 18 (pg_db)                          │
│  Schemas: youtube, music, fibery, public                      │
└──────────────────────────────────────────────────────────────┘
```

### Technology Stack

| Component     | Technology                                 | Rationale                                                     |
| ------------- | ------------------------------------------ | ------------------------------------------------------------- |
| MCP SDK       | `ModelContextProtocol` NuGet               | Official .NET MCP SDK from Microsoft                          |
| ORM           | EF Core 10 + Npgsql                        | Reuses existing compiled models, repositories, entity configs |
| Transport     | stdio (primary), SSE (optional)            | stdio for local AI tools, SSE for remote/headless access      |
| DI            | `Microsoft.Extensions.DependencyInjection` | Same DI container as existing project                         |
| Logging       | Serilog                                    | Same logging infrastructure as existing project               |
| Configuration | Environment variables + `appsettings.json` | `PGCONNSTR` for connection string, env vars for MCP settings  |

## C# SDK Implementation Patterns

The server uses the official `ModelContextProtocol` NuGet package with attribute-based auto-discovery. These patterns are validated against the current SDK version and the existing v0.3 implementation.

### Tool Pattern (✅ used in QueryExecuteTool, SchemaDescribeTool, MigrationValidateTool)

```csharp
[McpServerToolType]
internal sealed class MyTool(ScriptsDbContext db)  // Constructor DI works
{
    [McpServerTool]
    [Description("Human-readable description shown in MCP client tool list.")]
    public async Task<string> my_tool_name(            // snake_case method name = tool name
        [Description("Parameter description.")] string param1,
        [Description("Optional param.")] int param2 = 10,
        CancellationToken cancellationToken = default) // Optional; MCP SDK injects
    {
        // Return JSON string, plain text, or markdown
    }
}
```

### Resource Pattern (✅ used in EntityMetadataResource, ConnectionStatusResource)

```csharp
// Direct resource (fixed URI)
[McpServerResourceType]
internal sealed class MyResources(ScriptsDbContext db)
{
    [McpServerResource(
        UriTemplate = "pg://path/to/resource",  // Fixed URI
        Name = "Resource Display Name",          // Shown in MCP client
        MimeType = "application/json")]          // Content type
    [Description("Description shown in resource list.")]
    public string GetResource() { /* return JSON/markdown */ }
}

// Template resource (parameterized URI)
[McpServerResource(
    UriTemplate = "pg://entities/{entityType}",  // {param} in URI
    Name = "Entity Definition",
    MimeType = "application/json")]
public string GetEntityDefinition(string entityType) { /* entityType auto-bound */ }
```

### Prompt Pattern (❌ not yet implemented)

```csharp
[McpServerPromptType]
public class MyPrompts
{
    [McpServerPrompt]
    [Description("Generates a prompt with parameters.")]
    public static IEnumerable<ChatMessage> MyPrompt(
        [Description("Parameter description.")]
        [AllowedValues("option1", "option2")]  // Provides completion suggestions
        string parameter) =>
        [
            new(ChatRole.User, $"User-facing message with {parameter}"),
            new(ChatRole.Assistant, "Optional assistant context to pre-load.")
        ];
}
```

### Server Boot Pattern (✅ used in Program.cs)

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddConsole(opts =>
    opts.LogToStandardErrorThreshold = LogLevel.Trace);  // stdout stays clean for JSON-RPC

builder.Services
    .AddScriptsDbContext()               // Reuses existing DI registration
    .AddMcpServer()
    .WithStdioServerTransport()          // stdio for local AI tools
    .WithToolsFromAssembly();            // Auto-discovers [McpServerToolType] and [McpServerResourceType]

await builder.Build().RunAsync();
```

### Key Architectural Constraints

1. **One class = one tool or resource type** — the `[McpServerToolType]` attribute marks the container class; each `[McpServerTool]` method becomes a tool. Constructor DI is the standard pattern.
2. **`strings` for tool input** — parameter types must be primitive types or `string`. Complex objects must be deserialized from JSON strings inside the tool handler.
3. **`CancellationToken` is optional** — placed as the last parameter, the MCP SDK automatically injects it for cancellation support.
4. **Snake_case naming convention** — tool names match the C# method name (e.g., `query_execute`, `schema_describe`). The ADR tool table uses snake_case.
5. **Logging to stderr** — all diagnostic logging must go to stderr so stdout remains a clean JSON-RPC channel for the MCP client.
6. **No `McpServerFactory` needed** — the generic host builder pattern replaces the need for a custom factory class.

## API Surface: Tools, Resources, and Prompts

MCP exposes three primitives:
- **Tools**: Executable functions the AI agent can call (read/write operations)
- **Resources**: Read-only data that provides context (schema info, sample data)
- **Prompts**: Pre-written templates that guide the AI agent through workflows

### Tools (14)

Tools are registered with name, title, description, and JSON Schema input parameters. Each tool returns structured content (text, JSON, or tables).

#### Schema Discovery (4 tools)

| #   | Tool Name               | Description                                                         | Input Parameters                                                                        | Output                               |
| --- | ----------------------- | ------------------------------------------------------------------- | --------------------------------------------------------------------------------------- | ------------------------------------ |
| T01 | `list_schemas`          | List all database schemas with table counts                         | (none)                                                                                  | JSON array of schemas                |
| T02 | `list_tables`           | List tables in a schema with row counts                             | `schema_name: string`                                                                   | JSON array of tables                 |
| T03 | `describe_table`        | Get columns, types, constraints, indexes for a table                | `schema_name: string`, `table_name: string`                                             | Markdown table of column definitions |
| T04 | `get_entity_definition` | Get EF Core entity definition with properties, types, relationships | `entity_type: string` (enum: Artist, Album, Track, Scrobble, Video, FiberyEntity, etc.) | JSON schema of entity                |

#### Data Access (5 tools)

| #   | Tool Name          | Description                                                                                                 | Input Parameters                                                                  | Output                          |
| --- | ------------------ | ----------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------- | ------------------------------- |
| T05 | `query_execute`    | ✅ IMPLEMENTED. Execute a read-only SQL query (SELECT, WITH, EXPLAIN, SHOW, TABLE) with parameterized inputs | `sql: string`, `parameters: string?` (JSON array), `max_rows: int?` (default 100) | JSON {columns, rows, rowCount}  |
| T06 | `schema_describe`  | ✅ IMPLEMENTED. Describe database schema using EF Core metadata — tables, columns, types, keys, indexes      | `table_name: string?` (optional filter)                                           | JSON with entity schema details |
| T07 | `search_entities`  | Full-text search across entity tables using PostgreSQL text search                                          | `query: string`, `entity_types: string[]?`, `max_results: int?`                   | JSON array of matches           |
| T08 | `get_row_count`    | Get exact or estimated row count for a table                                                                | `schema_name: string`, `table_name: string`                                       | Number string                   |
| T09 | `get_entity_by_id` | Retrieve a single entity by primary key                                                                     | `entity_type: string`, `id: string`                                               | JSON object                     |

#### Operations (3 tools)

| #   | Tool Name               | Description                                               | Input Parameters                        | Output             |
| --- | ----------------------- | --------------------------------------------------------- | --------------------------------------- | ------------------ |
| T10 | `check_health`          | Check database connectivity and report stats              | (none)                                  | JSON health report |
| T11 | `trigger_scrobble_sync` | Trigger a Last.fm scrobble synchronization                | `username: string?`, `full_sync: bool?` | JSON sync result   |
| T12 | `get_sync_status`       | Get the current synchronization status across all domains | (none)                                  | JSON status report |

#### Admin (2 tools)

| #   | Tool Name            | Description                                                          | Input Parameters | Output                                              |
| --- | -------------------- | -------------------------------------------------------------------- | ---------------- | --------------------------------------------------- |
| T13 | `migration_validate` | ✅ IMPLEMENTED. Show EF Core migration history and pending migrations | (none)           | JSON {status, appliedMigrations, pendingMigrations} |
| T14 | `get_database_stats` | Database size, table sizes, index usage                              | (none)           | JSON statistics                                     |

### Resources (10 templates)

Resources use URI templates for parameterized access. Each resource returns markdown or JSON content.

| #   | URI Template                                   | Name                | Description                                          | MIME Type          |
| --- | ---------------------------------------------- | ------------------- | ---------------------------------------------------- | ------------------ |
| R01 | `pg://schemas`                                 | Schema Listing      | All schemas with descriptions and table counts       | `application/json` |
| R02 | `pg://schemas/{schema}/tables`                 | Schema Tables       | Tables in a schema with row counts                   | `application/json` |
| R03 | `pg://schemas/{schema}/tables/{table}/columns` | Table Columns       | Column definitions with types, nullability, defaults | `text/markdown`    |
| R04 | `pg://schemas/{schema}/tables/{table}/sample`  | Table Sample        | First N rows of a table                              | `text/markdown`    |
| R05 | `pg://entities/{entityType}`                   | Entity Definition   | Full EF Core entity definition                       | `application/json` |
| R06 | `pg://database/info`                           | Database Overview   | Connection info, schema summary, row counts          | `text/markdown`    |
| R07 | `pg://database/extensions`                     | Extensions          | Installed PostgreSQL extensions                      | `application/json` |
| R08 | `pg://database/migrations`                     | Migrations          | EF Core migration history                            | `text/markdown`    |
| R09 | `pg://sync/status`                             | Sync Status         | Last sync timestamps per domain                      | `application/json` |
| R10 | `pg://database/stats`                          | Database Statistics | Size, table sizes, index usage                       | `application/json` |

### Prompts (5)

Prompts provide structured workflows that guide the AI agent through common tasks. Each prompt includes instructions and may pre-load relevant resources.

| #   | Prompt Name      | Title                        | Description                                                                                   | Arguments                                    |
| --- | ---------------- | ---------------------------- | --------------------------------------------------------------------------------------------- | -------------------------------------------- |
| P01 | `explore-schema` | Explore Database Schema      | Interactive schema exploration — start by listing schemas, drill into tables, examine columns | (none)                                       |
| P02 | `build-query`    | Build a Database Query       | Guide for constructing safe, efficient SQL queries against the PostgreSQL database            | `goal: string` (what you want to query)      |
| P03 | `analyze-data`   | Analyze Data Patterns        | Systematic data analysis workflow — discover patterns, check distributions, find anomalies    | `domain: string?` (youtube, music, fibery)   |
| P04 | `sync-audit`     | Synchronization Audit        | Check what data has been synced, when, and identify gaps                                      | `domain: string?`                            |
| P05 | `troubleshoot`   | Troubleshoot Database Issues | Diagnostic workflow — check health, review logs, identify problems                            | `issue: string` (description of the problem) |

## EF Core Integration

The MCP server reuses the existing EF Core infrastructure:

### Connection and Configuration

```
MCP Server Startup (Program.cs)
  → Host.CreateApplicationBuilder (Microsoft.Extensions.Hosting)
  → builder.Services.AddScriptsDbContext() — reuses existing DI registration
      - reads PGCONNSTR env var (same as CLI project)
      - creates DbContextOptions<ScriptsDbContext> with:
          Npgsql provider, EnableRetryOnFailure (5 retries, 2s base)
          Compiled model: MyCompiledModels.ScriptsDbContextModel.Instance
      - registers 5 repositories (Album, Artist, Track, Scrobble, Video)
      - registers Polly resilience pipeline
  → builder.Services.AddMcpServer()
      .WithStdioServerTransport()
      .WithToolsFromAssembly() — auto-discovers [McpServerToolType] and [McpServerResourceType] classes
  → builder.Build().RunAsync()
```

**Key design notes:**
- No `McpServerFactory` class — the server uses the standard .NET generic host pattern via `Host.CreateApplicationBuilder`
- Tools and resources are discovered automatically via `WithToolsFromAssembly()` (attribute-based auto-registration)
- The `ScriptsDbContext` is registered as scoped via `AddScriptsDbContext()` reuse
- All logging goes to stderr via `LogToStandardErrorThreshold = LogLevel.Trace` (keeps stdout clean for JSON-RPC)

### Compiled Model Integration

The server uses the same compiled model as the main project (`csharp/CompiledModels/ScriptsDbContextModel.cs` → `MyCompiledModels.ScriptsDbContextModel.Instance`). This gives:

- **Fast startup**: No runtime model discovery — the compiled model is loaded directly
- **Schema consistency**: The MCP server sees the exact same schema as the CLI
- **Single source of truth**: Schema changes only need one `dotnet ef dbcontext optimize` regeneration

The compiled model is referenced as a project dependency, not duplicated. The MCP server project references the `Scripts` library project.

### Repository Layer Reuse

All data access goes through the existing repository interfaces:

- `IAlbumRepository` — album queries, by-artist lookups
- `IArtistRepository` — artist search, entity retrieval
- `ITrackRepository` — track queries, album-track relationships
- `IScrobbleRepository` — scrobble history, timestamps, user filtering
- `IVideoRepository` — YouTube video metadata

The repositories already implement:
- Polly resilience policies (retry, circuit breaker)
- `IQueryable<T>` for composable queries
- Async operations (`ToListAsync`, `FirstOrDefaultAsync`, `CountAsync`)
- No-tracking queries (configured in `ScriptsDbContext`)

### Schema-Aware Design

The MCP server is schema-aware, respecting the four-schema architecture:

| Schema    | MCP Server Behavior                                                                                                |
| --------- | ------------------------------------------------------------------------------------------------------------------ |
| `music`   | Read-only access. AI agents can query artists, albums, tracks, scrobbles, release progress. No writes.             |
| `youtube` | Read-only access. AI agents can query video metadata. No writes.                                                   |
| `fibery`  | Read-only access. AI agents can query fibery entities. No writes.                                                  |
| `public`  | Read access to execution logs, failed tasks, source records. Write access to source records (for sync operations). |

Schema names are never hardcoded in MCP tool handlers — they are read from entity configurations (`builder.ToTable(name, schema)`) so the MCP server stays in sync with any future schema reorganization.

### Read-Only Enforcement

The `query_database` tool enforces read-only access by:

1. Wrapping all queries in `BEGIN READ ONLY; ... COMMIT;` transactions
2. Rejecting any SQL containing `INSERT`, `UPDATE`, `DELETE`, `DROP`, `ALTER`, `CREATE`, `TRUNCATE`
3. Using `SET TRANSACTION READ ONLY` at the session level
4. Setting a statement timeout (`SET statement_timeout = '30s'`)

This is defense in depth — both application-level SQL parsing and PostgreSQL-level transaction enforcement.

## Project Structure

```
csharp/
├── src/
│   ├── MCP/                               # MCP server project (exists, partial implementation)
│   │   ├── Mcp.csproj                     # Console app (PackageId: Scripts.Mcp), refs Scripts.csproj
│   │   ├── Program.cs                     # ✅ Entry point: Host.CreateApplicationBuilder, DI, stdio transport
│   │   ├── Tools/                         # Tool handler classes ([McpServerToolType])
│   │   │   ├── QueryExecuteTool.cs        # ✅ T05: SQL execution with parameterized queries
│   │   │   ├── SchemaDescribeTool.cs      # ✅ T03: EF Core schema introspection
│   │   │   ├── MigrationValidateTool.cs   # ✅ T13: Migration validation
│   │   │   ├── SchemaListTool.cs          # ❌ T01: List schemas (not yet implemented)
│   │   │   ├── TableListTool.cs           # ❌ T02: List tables in schema
│   │   │   ├── EntityDefinitionTool.cs    # ❌ T04: Get entity definition
│   │   │   ├── SearchEntitiesTool.cs      # ❌ T07: Full-text search
│   │   │   ├── DataAccessTools.cs         # ❌ T06, T08, T09: Sample data, row counts, entity-by-ID
│   │   │   ├── OperationTools.cs          # ❌ T10-T12: Health, sync trigger, sync status
│   │   │   └── AdminTools.cs             # ❌ T14: Database stats
│   │   ├── Resources/                     # Resource handler classes ([McpServerResourceType])
│   │   │   ├── EntityMetadataResource.cs  # ✅ R05: Entity metadata (pg://entities/metadata)
│   │   │   ├── ConnectionStatusResource.cs# ✅ R06: Connection status (pg://connection/status)
│   │   │   ├── SchemaResources.cs         # ❌ R01-R04: Schema listing resources
│   │   │   ├── DatabaseResources.cs       # ❌ R07-R08: Extensions, migrations
│   │   │   ├── SyncResources.cs           # ❌ R09: Sync status resource
│   │   │   └── StatsResources.cs          # ❌ R10: Database statistics
│   │   ├── Prompts/                       # Prompt definitions ([McpServerPromptType])
│   │   │   ├── ExploreSchemaPrompt.cs     # ❌ P01: Schema exploration workflow
│   │   │   ├── BuildQueryPrompt.cs        # ❌ P02: Query building guide
│   │   │   ├── AnalyzeDataPrompt.cs       # ❌ P03: Data analysis workflow
│   │   │   ├── SyncAuditPrompt.cs         # ❌ P04: Sync audit workflow
│   │   │   └── TroubleshootPrompt.cs      # ❌ P05: Diagnostic workflow
│   │   └── Services/                      # Business logic layer (not yet created)
│   │       ├── SchemaService.cs           # Schema introspection helpers
│   │       ├── QueryService.cs            # SQL execution, safety checks
│   │       ├── SearchService.cs           # Full-text search via pg_trgm
│   │       ├── SyncService.cs             # Sync orchestration
│   │       └── HealthService.cs           # Connection health, stats
│   └── Data/                              # Existing: EF Core layer (unchanged)
├── CompiledModels/                        # Existing: compiled models (unchanged)
└── tests/
    └── Scripts.MCP.Tests/                 # MCP server tests (not yet created)
        ├── Scripts.MCP.Tests.csproj
        ├── Tools/
        │   ├── QueryExecuteToolTests.cs
        │   ├── SchemaDescribeToolTests.cs
        │   └── MigrationValidateToolTests.cs
        ├── Resources/
        │   ├── EntityMetadataResourceTests.cs
        │   └── ConnectionStatusResourceTests.cs
        └── Integration/
            └── McpServerIntegrationTests.cs
```

## Transport and Deployment

### Primary: stdio Transport

```
┌──────────────┐     stdin/stdout      ┌──────────────────────┐
│  MCP Client   │ ◄──────────────────► │  PostgresMcpServer    │
│  (stdio)      │    JSON-RPC 2.0       │  (Console App)        │
└──────────────┘                       └──────────────────────┘
```

The stdio transport is the default for local AI tools. Configuration in the MCP client:

```json
{
  "mcpServers": {
    "scripts-postgres": {
      "command": "dotnet",
      "args": ["run", "--project", "csharp/src/Mcp/Mcp.csproj"],
      "env": {
        "PGCONNSTR": "Host=localhost;Port=5432;Database=pg_db;Username=lance"
      }
    }
  }
}
```

### Alternative: SSE Transport

For remote or headless scenarios, the server supports SSE transport via HTTP:

```
┌──────────────┐       HTTP/SSE        ┌──────────────────────┐
│  MCP Client   │ ◄──────────────────► │  PostgresMcpServer    │
│  (remote)     │    JSON-RPC 2.0       │  (WebHost)            │
└──────────────┘                       └──────────────────────┘
```

Configuration:

```json
{
  "mcpServers": {
    "scripts-postgres-remote": {
      "url": "http://host:5199/sse"
    }
  }
}
```

## Security Model

### Connection Security

- Connection string read from `PGCONNSTR` environment variable (never hardcoded)
- Connection uses trusted authentication (Windows integrated auth or pgpass)
- No credentials exposed in MCP resource URIs or tool responses
- Connection pooling with minimum lifetime for connection reuse

### Data Access Control

| Operation       | Tool                        | Restriction                                                     |
| --------------- | --------------------------- | --------------------------------------------------------------- |
| Read data       | T05 `query_database`        | `READ ONLY` transaction, SQL injection guard, statement timeout |
| Read schema     | T01-T04                     | No restrictions (metadata only)                                 |
| Entity access   | T06-T09                     | Repository-level filtering, row limits                          |
| Sync operations | T11 `trigger_scrobble_sync` | Requires valid API keys (Last.fm), rate limited                 |
| Admin           | T13-T14                     | Read-only metadata                                              |

### SQL Injection Prevention

The `query_database` tool uses parameterized queries. User input is never concatenated into SQL strings. When users provide a `sql` parameter:

1. The SQL is parsed and validated (reject DDL, DML, DCL)
2. Parameter placeholders are extracted
3. The query is executed via `NpgsqlCommand` with parameters

### Rate Limiting

- `query_database`: Maximum 10 concurrent queries, 60-second cooldown between queries from the same client
- `trigger_scrobble_sync`: Maximum 1 sync per 5 minutes
- Resource reads: No rate limit (read-only metadata)

## Alternatives Considered

### Alternative 1: TypeScript/Node.js MCP Server with direct PostgreSQL access

Use `@modelcontextprotocol/sdk` with `pg` npm package to connect directly to PostgreSQL.

**Pros:**
- Most mature MCP SDK (TypeScript/Node.js)
- Simpler to deploy (single Node.js process)
- Large ecosystem of PostgreSQL libraries

**Cons:**
- Duplicates schema knowledge (must redefine table schemas, relationships)  
- No EF Core compiled model benefits
- No repository reuse
- Must re-implement resilience policies, connection pooling
- Schema changes require updating both C# entity configs AND Node.js code
- SQL injection prevention must be built from scratch

**Verdict:** Rejected. The maintenance cost of keeping two schema definitions in sync outweighs the SDK maturity benefit.

### Alternative 2: Node.js MCP Server with sidecar .NET HTTP/gRPC API

Run a Node.js MCP server that calls a .NET Web API wrapping the DbContext.

**Pros:**
- Uses mature TypeScript MCP SDK
- .NET service reuses EF Core layer
- Clean separation of concerns

**Cons:**
- Two processes to deploy and manage
- Network latency between MCP server and .NET API
- Authentication between MCP server and API
- More infrastructure complexity
- gRPC requires protobuf definitions (another schema to maintain)
- Operational overhead (health checks, retries, timeouts for inter-process communication)

**Verdict:** Rejected. Over-engineered for a single-machine personal automation project. The latency and complexity are not justified.

### Alternative 3: Python MCP Server with SQLAlchemy

Use `mcp` Python package with SQLAlchemy to connect to PostgreSQL.

**Pros:**
- Second-most mature MCP SDK (Python)
- SQLAlchemy has good PostgreSQL support
- Easier to prototype

**Cons:**
- Python is not a first-class language in this repository (only a small toolkit)
- Duplicates schema knowledge
- No EF Core integration
- Different deployment model than the rest of the codebase
- Would require maintaining a separate Python project

**Verdict:** Rejected. Introducing Python as a server runtime adds maintenance burden without benefiting from the existing .NET infrastructure.

### Alternative 4: Generic PostgreSQL MCP Server with custom extensions

Use an existing generic PostgreSQL MCP server (e.g., `stuzero/pg-mcp-server`) and add domain-specific prompts/resources on top.

**Pros:**
- Leverages existing, tested implementation
- Faster initial development

**Cons:**
- No EF Core integration
- Must work within the generic server's constraints
- Extension model may be limited
- Cannot use compiled models for performance
- Cannot reuse repositories and resilience policies
- Limited to raw SQL operations — no domain entity awareness

**Verdict:** Rejected. The custom server provides domain awareness and code reuse that a generic server cannot match.

## Consequences

### Positive

- **Single source of truth**: Entity schemas are defined once in EF Core configurations. The MCP server, CLI, and tests all use the same model.
- **Compiled model performance**: Server startup is fast (no runtime model discovery) thanks to the pre-compiled EF Core model.
- **Repository reuse**: All data access goes through tested, resilience-wrapped repositories.
- **Schema consistency**: The MCP server automatically reflects schema changes — no manual synchronization needed.
- **Domain awareness**: AI agents can work with `music.artists` and `fibery.fibery_entities` at a semantic level, not raw SQL.
- **Unified configuration**: Same `PGCONNSTR` environment variable as the CLI — no separate connection configuration.
- **Security by design**: Read-only enforcement at the SQL level, parameterized queries, no credential leakage in resource URIs.

### Negative

- **.NET MCP SDK maturity**: The .NET MCP SDK (`ModelContextProtocol` NuGet) is less mature than the TypeScript SDK. API changes may be needed for future SDK versions.
- **Process coupling**: The MCP server runs as a separate process from the AI client. If the server crashes, the client loses database access.
- **Tool count considerations**: With 14 tools, 10 resources, and 5 prompts planned (3 tools and 2 resources implemented), the full server will be verbose in MCP client UI. Tool filtering (client-side) can mitigate this. The current v0.3 implementation exposes 3 tools and 2 resources.
- **Build dependency**: The MCP server project references the `Scripts` library project. Any change to the EF Core model requires rebuilding both.
- **Schema name extraction**: Reading schema names from entity configurations at runtime requires reflection or explicit mapping. If entity configs are changed without updating the MCP server's schema discovery logic, mismatches could occur.

### Neutral

- **Transport flexibility**: The server supports both stdio (local) and SSE (remote) transports. The default is stdio. SSE is available but not required for initial deployment.
- **Extensions**: The MCP server can be extended with additional tools, resources, and prompts without changing the core architecture. New entity types automatically appear in schema resources.

## Implementation Phases

### Phase 0: Core Server Boot (✅ Complete)
- ✅ Create `Mcp.csproj` project with `ModelContextProtocol` and `Npgsql` dependencies
- ✅ Create `Program.cs` with `Host.CreateApplicationBuilder`, DI, and stdio transport
- ✅ Wire up `AddScriptsDbContext()` for compiled model and connection reuse
- ✅ Register with `.AddMcpServer().WithStdioServerTransport().WithToolsFromAssembly()`
- ✅ Initial tools: `query_execute`, `schema_describe`, `migration_validate`
- ✅ Initial resources: `EntityMetadataResource`, `ConnectionStatusResource`

### Phase 1: Schema Discovery Tools (mcp-implementer)
- Implement `list_schemas` tool (T01) — enumerate schemas with table counts
- Implement `list_tables` tool (T02) — enumerate tables within a schema
- Implement `get_entity_definition` tool (T04) — full EF Core entity metadata
- Implement schema resources (R01-R04): `pg://schemas`, `pg://schemas/{schema}/tables`, etc.
- Implement `explore-schema` prompt (P01)

### Phase 2: Data Access Tools (mcp-implementer)
- Implement `QueryService` class for SQL validation and read-only enforcement
- Implement `SearchService` class for full-text search via `pg_trgm` indexes
- Implement `search_entities` tool (T07) — full-text search across entity tables
- Implement `get_row_count` tool (T08) — exact/estimated row counts
- Implement `get_entity_by_id` tool (T09) — entity retrieval by primary key
- Implement data resources (R08, R10): `pg://database/migrations`, `pg://database/stats`
- Implement `build-query` and `analyze-data` prompts (P02, P03)

### Phase 3: Operations + Admin (mcp-integrator)
- Implement `SyncService` integration with existing `ScrobbleSyncOrchestrator` and `YouTubePlaylistOrchestrator`
- Implement `HealthService` for connection statistics and latency measurements
- Implement `check_health` tool (T10)
- Implement `trigger_scrobble_sync` tool (T11)
- Implement `get_sync_status` tool (T12)
- Implement `get_database_stats` tool (T14)
- Implement sync resources (R09): `pg://sync/status`
- Implement `sync-audit` and `troubleshoot` prompts (P04, P05)

### Phase 4: Testing + Integration (mcp-tester)
- Unit tests for each tool handler with mocked `ScriptsDbContext`
- Integration tests against local PostgreSQL (Docker)
- End-to-end tests with MCP client simulator over stdio
- Performance tests (compiled model startup time, query performance)
- Documentation and usage examples

## Testing Strategy

### Unit Tests (`Scripts.MCP.Tests/Tools/`)
- Each tool handler tested in isolation with mocked `ScriptsDbContext` (in-memory provider with `SCRIPTS_NO_COMPILED_MODEL=1`)
- SQL validation rules tested with known-good and known-bad inputs
- Resource URI template parsing tested
- Prompt argument validation tested
- Tests follow the existing TUnit pattern used by `Scripts.Tests`

### Integration Tests (`Scripts.MCP.Tests/Integration/`)
- Real PostgreSQL connection (local Docker via `docker-compose.yml`)
- Full MCP request/response lifecycle via stdio transport
- Schema discovery against the actual 4-schema database
- Query execution with sample data
- Entity retrieval by ID
- Migration validation (applied, pending, snapshot health)

### End-to-End Tests
- MCP client simulator sends JSON-RPC requests over stdio
- Verifies tool discovery (`tools/list`)
- Verifies resource listing (`resources/list`, `resources/templates/list`)
- Verifies prompt listing (`prompts/list`)
- Verifies complete workflows (explore schema → query data → analyze results)

## References

- [MCP Specification (2025-11-25)](https://modelcontextprotocol.io/specification/2025-11-25/)
- [MCP .NET SDK (ModelContextProtocol NuGet)](https://www.nuget.org/packages/ModelContextProtocol)
- [MCP C# SDK Docs — Getting Started](https://github.com/modelcontextprotocol/csharp-sdk/blob/main/docs/concepts/getting-started.md)
- [MCP C# SDK — Tools](https://github.com/modelcontextprotocol/csharp-sdk/blob/main/docs/concepts/tools/tools.md)
- [MCP C# SDK — Resources](https://github.com/modelcontextprotocol/csharp-sdk/blob/main/docs/concepts/resources/resources.md)
- [MCP C# SDK — Prompts](https://github.com/modelcontextprotocol/csharp-sdk/blob/main/docs/concepts/prompts/prompts.md)
- [ADR 0001: EF Core 4-Schema Architecture](./0001-ef-core-3-schema-architecture.md)
- [MASTER_PLAN.md](../MASTER_PLAN.md) — Phase G (Wire Up EF Layer) for repository wiring context
- [Program.cs](../../csharp/src/Mcp/Program.cs) — MCP server entry point (✅ implemented)
- [QueryExecuteTool.cs](../../csharp/src/Mcp/Tools/QueryExecuteTool.cs) — SQL execution tool (✅ implemented)
- [SchemaDescribeTool.cs](../../csharp/src/Mcp/Tools/SchemaDescribeTool.cs) — Schema introspection tool (✅ implemented)
- [MigrationValidateTool.cs](../../csharp/src/Mcp/Tools/MigrationValidateTool.cs) — Migration validation tool (✅ implemented)
- [EntityMetadataResource.cs](../../csharp/src/Mcp/Resources/EntityMetadataResource.cs) — Entity metadata resource (✅ implemented)
- [ConnectionStatusResource.cs](../../csharp/src/Mcp/Resources/ConnectionStatusResource.cs) — Connection status resource (✅ implemented)
- [ScriptsDbContext.cs](../../csharp/src/Data/ScriptsDbContext.cs) — Existing DbContext
- [DbContextRegistration.cs](../../csharp/src/Data/DbContextRegistration.cs) — DI registration pattern reused by MCP server
- [RepositoryRegistration.cs](../../csharp/src/Data/Repositories/RepositoryRegistration.cs) — 5 repositories registered for DI
- [EF Core Compiled Models](https://learn.microsoft.com/en-us/ef/core/performance/advanced-performance-topics#compiled-models)
