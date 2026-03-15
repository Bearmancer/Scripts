# toolkit

This project uses `uv` for dependency management, locking, and command execution.

## Workflow

Use a repo-local `uv` environment that is not named `.venv`:

```powershell
$env:UV_PROJECT_ENVIRONMENT = ".uv"
uv sync
uv run pytest
uv run toolkit --help
```

If you want the setting for the current PowerShell session only:

```powershell
$env:UV_PROJECT_ENVIRONMENT = ".uv"
```

## Commands

```powershell
$env:UV_PROJECT_ENVIRONMENT = ".uv"
uv lock
uv sync
uv run pytest
uv run toolkit pristine login
uv run toolkit pristine download --headless
```

## Notes

- Do not create or use `.venv` in this repository.
- `uv.lock` is the source of truth for resolved dependencies.
