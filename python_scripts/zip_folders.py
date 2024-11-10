import subprocess
import sys
from pathlib import Path

zip_size_limit = 1.9 * 1024 ** 3
sevenzip_path = r"c:\Users\Lance\Desktop\7-Zip CLI\7za.exe"
desktop = ""

def get_folder_size(folder):
    return sum(f.stat().st_size for f in folder.rglob('*') if f.is_file())

def create_zip(folder: Path):
    zip_parent = Path.home() / "Desktop" / "Elvis Zip" / folder.parent.name
    zip_parent.mkdir
    
    zip_path =  zip_parent / f"{folder.name}.zip"
    
    if zip_path.exists():
        print(f"✓ ZIP already exists: {zip_path}")
        return

    if get_folder_size(folder) < zip_size_limit:
        command = [
            sevenzip_path, "a", "-mx=0",  str(zip_path), str(folder) + "\\*"
        ]
        subprocess.run(command, check=True)
        print(f"✓ Created zip using 7-Zip CLI: {zip_path}")
    else:
        multi_part_command = [
            sevenzip_path, "a", "-mx=0", "-v1.9g", str(zip_path), str(folder) + "\\*"
        ]
        subprocess.run(multi_part_command, check=True)
        print(f"✓ Created multi-part zip using 7-Zip CLI: {zip_path}")
    

def main(directory: Path):
    for folder in directory.iterdir():
        if folder.is_dir():
            print(f"\nProcessing: {folder.name}")
            create_zip(folder)


if __name__ == "__main__":
    if len(sys.argv) != 2:
        print(f"Invalid number of arguments supplied.")
        exit
    
    main(Path(sys.argv[1]))