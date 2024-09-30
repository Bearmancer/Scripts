import subprocess, sys
from pathlib import Path

def rename_file_red(path: Path):
    if not path.exists() or not path.is_dir():
        print(f"Error: The specified path '{path}' does not exist.")
        sys.exit(1)

    root_path = path.parent
    file_list = []
    old_file_names = "Old File Names:\n"

    for file in path.rglob('*'):
        relative_path_length = len(str(file.relative_to(root_path)))

        if relative_path_length > 180:
            old_file_names.join(f"{file}\n")

            new_length = 180 - (relative_path_length - len(file.name))
            new_name = file.name[:new_length] + file.suffix

            file.rename(new_name)
            file_list.append(new_name)

    if file_list:
        output = f"""
    {old_file_names}
    -----------------------
    New File Names:
    filelist: "{'|'.join(map(str, file_list))}"
    """
        desktop_path = Path.home() / "Desktop" / f"Files Renamed - {path.name}.txt"

        with desktop_path.open('a') as log_file:
            log_file.write(output)

        print(f"Files have been renamed for {path}.\n-----------------------")
    else:
        print(f"No files renamed for {path}.\n-----------------------")

def calculate_image_size(path: Path):
    exif_tool = Path(r"C:\Users\Lance\Desktop\exiftool-12.96_64\exiftool.exe")
    problematic_files = []

    for flac_file in path.glob('*.flac'):
        image_size = subprocess.run([exif_tool, '-PictureLength', '-s', '-s', '-s', str(flac_file)], capture_output=True, text=True)

        image_size_kb = round(int(image_size.stdout.strip()) / 1024, 2)

        if image_size_kb > 1024:
            print(f"{flac_file} embedded image size is: {image_size_kb} KB")
            problematic_files.append(image_size_kb)

    if problematic_files:
        output = f"Files larger than 1MB:\nfilelist:\"{'|'.join(str(file) for file in problematic_files)}\""
        with (Path.home() / "Desktop" / "problematic_files.txt").open('w') as file:
            file.write(output.strip())
        print(output)
    else:
        print("No files with embedded artwork less than 1MB")

if __name__ == "__main__":
    if len(sys.argv) != 3:
        print("Invalid number of arguments supplied.")
        exit()

    method = sys.argv[1]
    directory = Path(sys.argv[2])

    if method == "calculate_image_size":
        calculate_image_size(directory)
    elif method == "rfr":
        rename_file_red(directory)
    else:
        print(f"Argument 1 entered:{sys.argv[1]}")
        print(f"Argument 2 entered:{sys.argv[2]}")
        print("Invalid argument entered.")