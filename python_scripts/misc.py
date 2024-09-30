import sys
from pathlib import Path
from datetime import datetime

def log_to_file(message):
    log_file = Path.home() / "Desktop" / "Conversion Log.txt"
    timestamp = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    with open(log_file, "a", encoding="utf-8") as f:
        f.write(f"{timestamp}: {message}\n")

def get_folder_size(path):
    total_size = 0
    for entry in path.rglob('*'):
        if entry.is_file():
            total_size += entry.stat().st_size
    return total_size

def list_directories(path, indent=0):
    indentation = " " * indent
    folder_size = get_folder_size(path)
    output = f"{indentation}{path.name} (Folder Size: {folder_size / (1024 ** 2):.2f} MB)"

    print(output)
    log_to_file(output)

    for entry in path.iterdir():
        if entry.is_dir():
            list_directories(entry, indent + 2)

def list_files_and_directories(path, indent=0):
    indentation = " " * indent
    folder_size = get_folder_size(path)
    output = f"{indentation}{path.name} (Folder Size: {folder_size / (1024 ** 2):.2f} MB)"
    print(output)
    log_to_file(output)

    for entry in path.iterdir():
        if entry.is_dir():
            list_files_and_directories(entry, indent + 2)
        elif entry.is_file():
            output = f"{indentation}  {entry.name} (Size: {entry.stat().st_size / (1024 ** 2):.2f} MB)"
            print(output)
            log_to_file(output)

if __name__ == "__main__":
    if len(sys.argv) != 3:
        print("Usage: python script_name.py [list_dir|list_files_and_dirs]")
        exit()

    command = sys.argv[1]
    directory = Path(sys.argv[2])

    if command == 'list_dir':
        list_directories(directory)
    elif command == 'list_files_and_dirs':
        list_files_and_directories(directory)
    else:
        print("Unknown command. Use 'list_dir' or 'list_files_and_dirs'.")