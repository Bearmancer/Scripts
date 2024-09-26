import subprocess
import sys
from pathlib import Path

def convert_dvd_to_mkv(file, dvd_folder):
    output_path = dvd_folder / "Converted"
    output_path.mkdir(exist_ok=True)
    print(f"Output path created: {output_path}")

    result = subprocess.run([r"C:\Program Files (x86)\MakeMKV\makemkvcon64.exe", "mkv", f'file:{file}', "all", str(dvd_folder), "--minlength=180"], capture_output=True, text=True)

    if result.returncode == 0:
        print(f"Successfully converted {file}.")
    else:
        print(f"Failed to convert {file}: {result.stderr}")

def main(root_dir):
    root_path = Path(root_dir)
    non_remuxable = []

    for dvd_path in root_path.iterdir():
        if dvd_path.is_dir():
            remuxable = list(dvd_path.glob("**/*"))
            remuxable = [f for f in remuxable if f.name in ('VIDEO_TS.IFO', 'index.bdmv')]

            if "BACKUP" in dvd_path.name: continue
            
            for file in remuxable:
                print(f"Converting file: {file} in {dvd_path}")
                convert_dvd_to_mkv(file, dvd_path)

            if not remuxable:
                print(f"No remuxable files found in {dvd_path}.")
                non_remuxable.append(dvd_path)

    if non_remuxable:
        print("Folders that couldn't be remuxed:")
        for folder in non_remuxable:
            print(folder)

if __name__ == "__main__":
    if len(sys.argv) != 2:
        print("Usage: script.py <root_dir>")
        sys.exit(1)

    root_dir = sys.argv[1]
    main(root_dir)