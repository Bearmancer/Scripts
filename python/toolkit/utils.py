import subprocess

from unidecode import unidecode


def run_command(cmd: list[str], cwd: str | None = None) -> tuple[str, str]:
    """Run a subprocess command and return stdout/stderr."""
    try:
        process = subprocess.Popen(
            cmd,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            text=True,
            encoding="utf-8",
            errors="ignore",
            cwd=cwd,
        )
    except FileNotFoundError:
        raise FileNotFoundError(f"Command not found: {cmd[0]}") from None
    except PermissionError:
        raise PermissionError(f"Permission denied: {cmd[0]}") from None

    result, error = process.communicate()

    if process.returncode != 0:
        raise subprocess.CalledProcessError(
            process.returncode, cmd, output=result, stderr=error
        )

    return unidecode(result), unidecode(error)
