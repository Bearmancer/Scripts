import json
import argparse
from pathlib import Path
from datetime import datetime
from py3createtorrent import create_torrent
from music import rename_file_red


def log_to_file(message: str):
    log_file = Path.home() / "Desktop" / "Python Functions' Log.txt"
    timestamp = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    with open(log_file, "a", encoding="utf-8") as f:
        f.write(f"{timestamp}: {message}\n")


def get_folder_size(path: Path):
    total_size = 0
    for entry in path.rglob('*'):
        if entry.is_file():
            total_size += entry.stat().st_size
    return total_size


def list_directories(path: Path, sort_order: str, indent: int = 0):
    indentation = "  " * indent
    folder_size = get_folder_size(path)
    output = f"{indentation}{path.name} (Folder Size: {folder_size / (1024 ** 2):.2f} MB)"
    print(output)
    log_to_file(output)

    entries = [(entry, get_folder_size(entry)) for entry in path.iterdir() if entry.is_dir()]

    if sort_order == "1":
        entries.sort(key=lambda e: e[0].name)
    else:
        entries.sort(key=lambda e: e[1], reverse=True)

    for entry, _ in entries:
        list_directories(entry, indent + 2, sort_order)


def list_files_and_directories(path: Path, sort_order: bool, indent: int = 0):
    indentation = "  " * indent
    folder_size = get_folder_size(path)
    output = f"{indentation}{path.name} (Folder Size: {folder_size / (1024 ** 2):.2f} MB)"
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
        list_files_and_directories(entry, indent + 2, sort_order)

    for entry in files:
        file_size_mb = entry.stat().st_size / (1024 ** 2)
        file_output = f"{indentation}  {entry.name} (Size: {file_size_mb:.2f} MB)"
        print(file_output)
        log_to_file(file_output)


def make_torrents(folder: Path, process_all_subfolders: bool):
    print(f'Now processing: {folder}')

    rename_file_red(folder)

    dropbox = json.load(open(Path.home() / 'AppData' / 'Local' / 'Dropbox' / 'info.json')).get('personal', {}).get('path')

    if process_all_subfolders:
        directories = [d for d in folder.iterdir() if d.is_dir()]
    else:
        directories = [folder]

    for subfolder in directories:
        print(f"Creating torrents for {subfolder.name}...")

        create_torrent(path=str(subfolder), trackers=['https://home.opsfet.ch/7a0917ca5bbdc282de7f2eed00a69e2b/announce'], private=True, source="OPS", output=f"{dropbox}\\Lance\\{subfolder.name} - OPS.torrent")

        create_torrent(path=str(subfolder), trackers=["https://flacsfor.me/250f870ba861cefb73003d29826af739/announce"], private=True, source="RED", output=f"{dropbox}\\Lance\\{subfolder.name} - RED.torrent")


def main():
    parser = argparse.ArgumentParser(description="Process directories and files.")

    parser.add_argument("command", choices=["list_dir", "list_files_and_dirs", "make_torrents"], help="Command to execute")
    parser.add_argument("directory", type=Path, help="Directory to process")
    parser.add_argument("sort_order", choices=["0", "1"], help="Sorting order for directories/files (0 for size, 1 for name)")
    parser.add_argument("--process_all_subfolders", action="store_true", help="Process all subfolders for make_torrents")

    args = parser.parse_args()

    if args.command == 'list_dir':
        list_directories(args.directory, sort_order=args.sort_order)
    elif args.command == 'list_files_and_dirs':
        list_files_and_directories(args.directory, sort_order=args.sort_order == "1")
    elif args.command == "make_torrents":
        make_torrents(args.directory, process_all_subfolders=args.process_all_subfolders)
    else:
        print("Unknown command. Use 'list_dir', 'list_files_and_dirs' or 'make_torrents'.")


if __name__ == "__main__":
    main()