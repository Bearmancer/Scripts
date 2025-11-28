import argparse
import json
import subprocess
from datetime import datetime
from pathlib import Path
from py3createtorrent import create_torrent
from unidecode import unidecode
from music_tools import rename_file_red


def log_to_file(message: str):
    log_file = Path.home() / "Desktop" / "Python Functions' Log.txt"
    timestamp = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    with open(log_file, "a", encoding="utf-8") as f:
        f.write(f"{timestamp}: {message}\n")


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


def get_folder_size(path: Path):
    return sum(entry.stat().st_size for entry in path.rglob("*") if entry.is_file())


def list_directories(path: Path, sort_order: str = "0", indent: int = 0):
    indentation = "  " * indent
    folder_size = get_folder_size(path)
    output = (
        f"{indentation}{path.name} (Folder Size: {folder_size / (1024 ** 2):.2f} MB)"
    )
    print(output)
    log_to_file(output)

    entries = [
        (entry, get_folder_size(entry)) for entry in path.iterdir() if entry.is_dir()
    ]

    entries.sort(
        key=lambda e: e[0].name if sort_order == "1" else e[1],
        reverse=sort_order != "1",
    )

    for entry, _ in entries:
        list_directories(entry, sort_order, indent + 2)


def list_files_and_directories(path: Path, sort_order: bool = False, indent: int = 0):
    indentation = "  " * indent
    folder_size = get_folder_size(path)
    output = (
        f"{indentation}{path.name} (Folder Size: {folder_size / (1024 ** 2):.2f} MB)"
    )
    print(output)
    log_to_file(output)

    entries = list(path.iterdir())
    directories = [entry for entry in entries if entry.is_dir()]
    files = [entry for entry in entries if entry.is_file()]

    if sort_order:
        directories.sort(key=lambda e: e.name)
        files.sort(key=lambda e: e.name)
    else:
        directories.sort(key=lambda e: get_folder_size(e), reverse=True)
        files.sort(key=lambda e: e.stat().st_size, reverse=True)

    for entry in directories:
        list_files_and_directories(entry, sort_order, indent + 2)

    for entry in files:
        file_size_mb = entry.stat().st_size / (1024 ** 2)
        file_output = f"{indentation}  {entry.name} (Size: {file_size_mb:.2f} MB)"
        print(file_output)
        log_to_file(file_output)


def make_torrents(folder: Path):
    print(f"Now processing: {folder}")

    dropbox = Path(
        json.load(open(Path.home() / "AppData" / "Local" / "Dropbox" / "info.json"))
        .get("personal", {})
        .get("path")
    )

    rename_file_red(folder)

    print(f"Creating torrents for {folder.name}...")

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


def main():
    parser = argparse.ArgumentParser(description="File and Torrent Management Tool")
    parser.add_argument(
        "command",
        choices=["list_dir", "list_files_and_dirs", "make_torrents"],
        help="Command to execute",
    )
    parser.add_argument("directory", type=Path, help="Directory to process")
    parser.add_argument(
        "--sort_order",
        default="0",
        help="Sorting options for list commands (0: by size, 1: by name)",
    )
    parser.add_argument(
        "--subdirs",
        action="store_true",
        help="Create torrents for each subdirectory inside the directory passed, rather than for the directory itself",
    )

    args = parser.parse_args()

    directory = args.directory

    if args.command == "list_dir":
        list_directories(directory, args.sort_order)
    elif args.command == "list_files_and_dirs":
        list_files_and_directories(directory, args.sort_order == "1")
    elif args.command == "make_torrents":
        if args.subdirs:
            for entry in (e for e in directory.iterdir() if e.is_dir()):
                make_torrents(entry)
        else:
            make_torrents(directory)


if __name__ == "__main__":
    main()
