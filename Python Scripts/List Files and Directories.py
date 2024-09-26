import sys
from pathlib import Path

def list_directories(path: Path, indent=0):
    """Recursively list directories with indentation starting from the current directory."""
    indentation = " " * indent
    for entry in path.iterdir():
        if entry.is_dir():
            print(f"{indentation}{entry.name}")
            list_directories(entry, indent + 2)

def get_folder_size(path: Path):
    total_size = 0
    for entry in path.rglob('*'):
        if entry.is_file():
            total_size += entry.stat().st_size
    return total_size

def list_files_and_directories(path: Path, indent=0):
    indentation = " " * indent
    folder_size = get_folder_size(path)

    print(f"{indentation}{path.name} (Folder Size: {folder_size / (1024 ** 2):.2f} MB)")

    for entry in path.iterdir():
        if entry.is_dir():
            list_files_and_directories(entry, indent + 2)
        elif entry.is_file():
            print(f"{indentation}  {entry.name} (Size: {entry.stat().st_size / (1024 ** 2):.2f} MB)")

def main():
    if len(sys.argv) != 3:
        print("Usage: python script_name.py [list_dir|list_files_and_dirs]")
        return
    
    command = sys.argv[1]
    path = Path(sys.argv[2])
    if command == 'list_dir':
        list_directories(path)
    elif command == 'list_files_and_dirs':
        list_files_and_directories(path)
    else:
        print("Unknown command. Use 'list_dir' or 'list_files_and_dirs'.")

if __name__ == "__main__":
    main()