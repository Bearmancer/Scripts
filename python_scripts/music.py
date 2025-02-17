import pyperclip, subprocess, sys
from pathlib import Path


def rename_file_red(path: Path):
    if not path.exists() or not path.is_dir():
        print(f"Error: The specified path '{path}' does not exist.")
        exit(1)

    root_path = path.parent
    old_files_list = []
    new_files_list = []

    for file in path.rglob('*'):
        relative_path_length = len(str(file.relative_to(root_path)))

        if relative_path_length > 180:
            old_files_list.append(file)

            new_length = 180 - (relative_path_length - len(file.name)) - len(file.suffix)
            new_name = file.stem[:new_length] + file.suffix

            new_file_path = file.with_name(new_name)
            file.rename(new_file_path)
            new_files_list.append(new_file_path)

            print(f"Old name: '{file}'\n")
            print(f"New name: '{new_file_path}\n-----------------------\n")

    if new_files_list:
        new_file_names = f"filelist:\"{'|'.join(map(str, new_files_list))}\""
        output = (
            f"Old file names of {path}:\n\n{chr(10).join(map(str, old_files_list))}"
            f"\n\n-----------------------\n\n"
            f"New File Names of {path}:\n\n"
            f"{new_file_names}\n"
        )

        print(f"Files have been renamed for {path}.\n-----------------------\n")
        pyperclip.copy(f"{new_file_names}")
        with (Path.home() / "Desktop" / "Excessively Long Files.txt").open('w') as file:
            file.write(output)

    else:
        print(f"No files renamed for {path}.\n-----------------------\n")


def calculate_image_size(path: Path):
    exif_tool = r"C:\Users\Lance\Desktop\exiftool-12.96_64\exiftool.exe"
    problematic_files = []

    for flac_file in path.glob('*.flac'):
        image_size = subprocess.run([exif_tool, '-PictureLength', '-s', '-s', '-s', str(flac_file)], capture_output=True, text=True)

        image_size_kb = round(int(image_size.stdout.strip()) / 1024, 2)

        if image_size_kb > 1024:
            print(f"{flac_file} embedded image size is: {image_size_kb} KB")
            problematic_files.append(image_size_kb)

    if problematic_files:
        output = f"Files larger than 1MB:\nfilelist:\"{'|'.join(str(file) for file in problematic_files)}\""
        with (Path.home() / "Desktop" / "Files with Giant Embedded Images.txt").open('w') as file:
            file.write(output)
        print(output)
    else:
        print("No files with embedded artwork less than 1MB")


def main():
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
        print("Invalid argument entered.")


if __name__ == "__main__":
    main()