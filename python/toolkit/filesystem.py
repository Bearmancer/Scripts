import json
import subprocess
from pathlib import Path
from py3createtorrent import create_torrent
from unidecode import unidecode
from toolkit.cli import get_logger

logger = get_logger("filesystem")


def run_command(cmd, cwd=None):
    process = subprocess.Popen(
        cmd,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        text=True,
        encoding="utf-8",
        errors="ignore",
        cwd=cwd,
    )

    result, error = process.communicate()

    if process.returncode != 0:
        raise subprocess.CalledProcessError(
            process.returncode, cmd, output=result, stderr=error
        )

    return unidecode(result), unidecode(error)


def get_folder_size(path):
    return sum(entry.stat().st_size for entry in path.rglob("*") if entry.is_file())


def _print_directory_info(path, indent):
    """Helper function to print directory name and size."""
    indentation = "  " * indent
    folder_size = get_folder_size(path)
    output = f"{indentation}{path.name} ({folder_size / (1024 ** 2):.2f} MB)"
    print(output)


def _sort_entries(entries, sort_by_name):
    """Helper function to sort entries by name or size."""
    if sort_by_name:
        return sorted(entries, key=lambda e: e.name)
    else:
        return sorted(entries, key=lambda e: get_folder_size(e), reverse=True)


def list_directories(path, sort_order="0", indent=0):
    _print_directory_info(path, indent)
    
    directories = [entry for entry in path.iterdir() if entry.is_dir()]
    sorted_dirs = _sort_entries(directories, sort_order == "1")

    for entry in sorted_dirs:
        list_directories(entry, sort_order, indent + 2)


def list_files_and_directories(path, sort_order=False, indent=0):
    _print_directory_info(path, indent)

    entries = list(path.iterdir())
    directories = [entry for entry in entries if entry.is_dir()]
    files = [entry for entry in entries if entry.is_file()]

    sorted_dirs = _sort_entries(directories, sort_order)

    if sort_order:
        files.sort(key=lambda e: e.name)
    else:
        files.sort(key=lambda e: e.stat().st_size, reverse=True)

    for entry in sorted_dirs:
        list_files_and_directories(entry, sort_order, indent + 2)

    indentation = "  " * indent
    for entry in files:
        file_size_mb = entry.stat().st_size / (1024**2)
        file_output = f"{indentation}  {entry.name} ({file_size_mb:.2f} MB)"
        print(file_output)


def rename_file_red(path):
    if not path.exists() or not path.is_dir():
        logger.error(f"Path does not exist: {path}")
        return

    root_path = path.parent
    renamed_count = 0

    for file in path.rglob("*"):
        relative_path_length = len(str(file.relative_to(root_path)))

        if relative_path_length > 180:
            new_length = (
                180 - (relative_path_length - len(file.name)) - len(file.suffix)
            )
            new_name = file.stem[:new_length] + file.suffix
            new_file_path = file.with_name(new_name)
            file.rename(new_file_path)
            renamed_count += 1
            logger.info(f"Renamed: {file.name}")

    logger.info(
        f"Renamed {renamed_count} files"
        if renamed_count
        else "No files needed renaming"
    )


def make_torrents(folder):
    logger.info(f"Creating torrents for {folder.name}")

    dropbox_info_path = Path.home() / "AppData" / "Local" / "Dropbox" / "info.json"

    if not dropbox_info_path.exists():
        raise FileNotFoundError("Dropbox info.json not found")

    dropbox = Path(json.load(open(dropbox_info_path)).get("personal", {}).get("path"))

    rename_file_red(folder)

    create_torrent(
        path=str(folder),
        trackers=["https://home.opsfet.ch/7a0917ca5bbdc282de7f2eed00a69e2b/announce"],
        private=True,
        source="OPS",
        output=str(dropbox / "Lance" / f"{folder.name} - OPS.torrent"),
    )

    create_torrent(
        path=str(folder),
        trackers=["https://flacsfor.me/250f870ba861cefb73003d29826af739/announce"],
        private=True,
        source="RED",
        output=str(dropbox / "Lance" / f"{folder.name} - RED.torrent"),
    )

    logger.info(f"Torrents created for {folder.name}")
