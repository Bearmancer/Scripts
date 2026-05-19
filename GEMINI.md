# Scripts Repository

## Project Overview

This is a personal repository for automation scripts, utilities, and lightweight applications. The project is
multi-lingual, housing code in C#, Python, and PowerShell. It also includes a local PostgreSQL 18 database instance
(pg_db) managed via Docker Compose for isolated testing and local data storage.

### Key Technologies:

* **Shell:** PowerShell Core / Windows PowerShell
* **Languages:** C# (.NET 10), Python (managed by `uv`), PowerShell
* **Database:** PostgreSQL 18 (Local Docker) — credentials in `.env` / `$PGCONNSTR`
* **Infrastructure:** Docker Compose

---

## Building and Running

### Database Management

A local PostgreSQL 18 instance is provided for script testing. Ensure Docker is running.

* **Connection:** Set via `$env:PGCONNSTR` (see `.env`)
* **Start Database:**
  ```powershell
  docker compose up -d
  ```
* **Stop Database:**
  ```powershell
  docker compose down
  ```

### Language-Specific Execution

* **Python:** Uses `uv` for dependency management. Navigate to the `python/` directory to run scripts or manage
  packages (e.g., `uv run <script.py>`).
* **C#:** Uses the .NET SDK. Navigate to the `csharp/` directory to build or run projects (e.g., `dotnet build`,
  `dotnet run`).
* **PowerShell:** Run scripts directly using PowerShell (e.g., `.\powershell\ScriptsToolkit\<script>.ps1`).

---

## Development Conventions & Architecture

* **Terminal Environment:** Use PowerShell for all terminal operations on Windows.
* **Organization:** Place new scripts or utilities in their respective language directories (`powershell/`, `python/`,
  `csharp/`).
* **Security:**
	* **NEVER** read, log, echo, or hardcode environment secrets (e.g., `$env:*`, `.env`, API tokens, connection
	  strings) in the code.
	* Rely on `.env` files or secure credential stores.
* **State Management:** Do not modify files in the `state/` directory manually. This directory is used by Docker for
  PostgreSQL data persistence (`state/postgres/18/docker`).
* **Database Schema:** The initial database schema is defined in `.kilo/references/init_schema.sql` and is automatically
  applied when the database container starts for the first time. The full canonical schema is in
  `.kilo/references/schema.sql`.
