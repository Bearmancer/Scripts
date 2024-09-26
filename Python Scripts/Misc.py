import subprocess, sys
from pathlib import Path

def call_cmdlet_all_subfolders(command, directory: Path):
    for folder in directory.rglob('*'):
        if folder.is_dir():
            try:
                subprocess.run(command, cwd=folder, check=True)
            except subprocess.CalledProcessError as e:
                print(f"Error executing command in {folder}: {e}")

def call_cmdlet_all_files(command, directory: Path):
    files = directory.glob('*')
    for file in files:
        if file.is_file():
            try:
                subprocess.run(command + [str(file)], check=True)
            except subprocess.CalledProcessError as e:
                print(f"Error executing command for file {file}: {e}")

if __name__ == "__main__":
    if sys.argv[1] == "ccas":
        call_cmdlet_all_files(Path(sys.argv[2]))
    elif sys.argv[1] == "ccaf":
        call_cmdlet_all_subfolders(Path(sys.argv[2]))
    else:
        print("Invalid number of arguments supplied.")