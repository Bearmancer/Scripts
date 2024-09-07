import os
import sys

def list_directories(path='.', indent=0):
    """Recursively list directories with indentation starting from the current directory."""
    indentation = " " * indent
    with os.scandir(path) as entries:
        for entry in entries:
            if entry.is_dir():
                print(f"{indentation} {entry.name}")
                list_directories(entry.path, indent + 2)

def list_files_and_directories(path='.', indent=0):
    """Recursively list directories and files with indentation starting from the current directory."""
    indentation = " " * indent
    with os.scandir(path) as entries:
        for entry in entries:
            if entry.is_dir():
                print(f"{indentation} {entry.name}")
                list_files_and_directories(entry.path, indent + 2)
        for entry in entries:
            if entry.is_file():
                print(f"{indentation} {entry.name}")

def main():
    if len(sys.argv) != 2:
        print("Usage: python script_name.py [list_dir|list_files_and_dirs]")
        return
    
    command = sys.argv[1]
    if command == 'list_dir':
        list_directories()
    elif command == 'list_files_and_dirs':
        list_files_and_directories()
    else:
        print("Unknown command. Use 'list_dir' or 'list_files_and_dirs'.")

if __name__ == "__main__":
    main()