# Architecture Legend - Final State

## C# Types
- `int`: 32-bit signed integer
- `string`: Unicode text string
- `DateTime`: Date and time
- `DateTimeOffset`: Date and time with offset from UTC
- `TimeSpan`: Duration of time
- `Task`: Represents an asynchronous operation
- `List~T~`: Generic list of type T
- `JsonDocument`: JSON representation of metadata

## Access Modifiers
- `+`: Public
- `-`: Private
- `#`: Protected
- `~`: Generic type parameter

## Relationship Types
- `o--`: Aggregation (One-to-Many / Has-a)
- `..>`: Dependency (Uses-a / Injected)
- `--|>`: Inheritance (Is-a)

## Namespace Overview
- `Scripts.Data.Entities`: Database models (POCOs)
- `Scripts.Data.Configuration`: EF Core entity configurations
- `Scripts.Data.Repositories`: Data access layer for specific entities
- `Scripts.Services.Music`: Domain services for music work and purging
- `Scripts.Services.Sync`: External API integration services
- `Scripts.Orchestrators`: High-level workflow coordination
