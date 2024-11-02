import sys
import json
from py3createtorrent import create_torrent
from pathlib import Path
from datetime import datetime
from music import rename_file_red


def log_to_file(message):
    log_file = Path.home() / "Desktop" / "Python Functions' Log.txt"
    timestamp = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    with open(log_file, "a", encoding="utf-8") as f:
        f.write(f"{timestamp}: {message}\n")


def get_folder_size(path):
    total_size = 0
    for entry in path.rglob('*'):
        if entry.is_file():
            total_size += entry.stat().st_size
    return total_size


def list_directories(path, indent=0, sort_order="1"):
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
        list_directories(entry[0], indent + 2, sort_order)


def list_files_and_directories(path, indent=0, sort_order="1"):
    indentation = "  " * indent
    folder_size = get_folder_size(path)
    output = f"{indentation}{path.name} (Folder Size: {folder_size / (1024 ** 2):.2f} MB)"
    print(output)
    log_to_file(output)

    entries = list(path.iterdir())
    directories = [entry for entry in entries if entry.is_dir()]
    files = [entry for entry in entries if entry.is_file()]

    if sort_order == "1":
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


def make_torrents(folder):
    print(f'Now processing: {folder}')

    rename_file_red(folder)

    dropbox = json.load(open(Path.home() / 'AppData' / 'Local' / 'Dropbox' / 'info.json')).get('personal', {}).get('path')

    create_torrent(path=str(folder), trackers=['https://home.opsfet.ch/7a0917ca5bbdc282de7f2eed00a69e2b/announce'], private=True, source="OPS", output=f"{dropbox}\\{folder.name} - OPS.torrent")
    create_torrent(path=str(folder), trackers=["https://flacsfor.me/250f870ba861cefb73003d29826af739/announce"], private=True, source="RED",output=f"{dropbox}\\{folder.name} - RED.torrent")


def parse_arguments():
    if len(sys.argv) < 3:
        print("Invalid number of arguments entered.")
        exit()

    command = sys.argv[1]
    directory = Path(sys.argv[2])
    process_all_subfolders = sys.argv[3].lower() == 'true' if len(sys.argv) > 3 else True

    return command, directory, process_all_subfolders


def process_make_torrents(directory, process_all_subfolders):
    directories = [d for d in directory.iterdir() if d.is_dir()] if process_all_subfolders else [directory]

    for subfolder in directories:
        if subfolder.is_dir():
            make_torrents(subfolder)


def main():
    command, directory, process_all_subfolders = parse_arguments()

    if command == 'list_dir':
        list_directories(directory)
    elif command == 'list_files_and_dirs':
        list_files_and_directories(directory)
    elif command == "make_torrents":
        process_make_torrents(directory, process_all_subfolders)
    else:
        print("Unknown command. Use 'list_dir', 'list_files_and_dirs' or 'make_torrents'.")


if __name__ == "__main__":
    main()