"""Google-style docstring pattern — CLI user-facing commands only."""


def sync_library(source: str, dry_run: bool = False) -> int:
    """
    Synchronize media library from source path.

    Args:
        source: Absolute path to the source directory.
        dry_run: If True, print actions without making changes. Default is False.

    Returns:
        Number of files synchronized.

    Raises:
        FileNotFoundError: If source path does not exist.

    Examples:
        >>> sync_library("/media/music", dry_run=True)
        42
    """
    if not source:
        raise FileNotFoundError(f"Source path not found: {source}")
    return 0
