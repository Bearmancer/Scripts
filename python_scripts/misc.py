import argparse
import json
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
    return sum(entry.stat().st_size for entry in path.rglob('*') if entry.is_file())


def list_directories(path: Path, sort_order: str = "0", indent: int = 0):
    indentation = "  " * indent
    folder_size = get_folder_size(path)
    output = f"{indentation}{path.name} (Folder Size: {folder_size / (1024 ** 2):.2f} MB)"
    print(output)
    log_to_file(output)

    entries = [(entry, get_folder_size(entry)) for entry in path.iterdir() if entry.is_dir()]

    entries.sort(key=lambda e: e[0].name if sort_order == "1" else e[1], 
                 reverse=sort_order != "1")

    for entry, _ in entries:
        list_directories(entry, sort_order, indent + 2)


def list_files_and_directories(path: Path, sort_order: bool = False, indent: int = 0):
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
        list_files_and_directories(entry, sort_order, indent + 2)

    for entry in files:
        file_size_mb = entry.stat().st_size / (1024 ** 2)
        file_output = f"{indentation}  {entry.name} (Size: {file_size_mb:.2f} MB)"
        print(file_output)
        log_to_file(file_output)


def make_torrents(folder: Path, process_all_subfolders: bool = True):
    print(f'Now processing: {folder}')

    rename_file_red(folder)

    dropbox = json.load(open(Path.home() / 'AppData' / 'Local' / 'Dropbox' / 'info.json')).get('personal', {}).get('path')

    directories = [d for d in folder.iterdir() if d.is_dir()] if process_all_subfolders else [folder]

    for subfolder in directories:
        print(f"Creating torrents for {subfolder.name}...")

        create_torrent(
            path=str(subfolder), 
            trackers=['https://home.opsfet.ch/7a0917ca5bbdc282de7f2eed00a69e2b/announce'], 
            private=True, 
            source="OPS", 
            output=f"{dropbox}\\Lance\\{subfolder.name} - OPS.torrent"
        )

        create_torrent(
            path=str(subfolder), 
            trackers=["https://flacsfor.me/250f870ba861cefb73003d29826af739/announce"], 
            private=True, 
            source="RED", 
            output=f"{dropbox}\\Lance\\{subfolder.name} - RED.torrent"
        )


def main():
    parser = argparse.ArgumentParser(description="File and Torrent Management Tool")
    parser.add_argument('command', choices=['list_dir', 'list_files_and_dirs', 'make_torrents'], help='Command to execute')
    parser.add_argument('directory', type=Path, help='Directory to process')
    parser.add_argument('--sort_order', default='0', help='Sorting options for list commands (0: by size, 1: by name)')
    parser.add_argument('--process_all_subfolders', action='store_true', help='Process all subfolders for torrent creation')

    args = parser.parse_args()

    if args.command == 'list_dir':
        list_directories(args.directory, args.sort_order)
    elif args.command == 'list_files_and_dirs':
        list_files_and_directories(args.directory, args.sort_order == '1')
    elif args.command == 'make_torrents':
        make_torrents(args.directory, args.process_all_subfolders)

if __name__ == "__main__":
    main()