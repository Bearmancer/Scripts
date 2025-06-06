import argparse
import pyperclip
import subprocess
from pathlib import Path


def rename_file_red(path: Path):
    if not path.exists() or not path.is_dir():
        print(f"Error: The specified path '{path}' does not exist.")
        exit(1)

    root_path = path.parent
    old_files_list, new_files_list = [], []

    for file in path.rglob('*'):
        relative_path_length = len(str(file.relative_to(root_path)))

        if relative_path_length > 180:
            old_files_list.append(file)
            new_length = 180 - (relative_path_length -
                                len(file.name)) - len(file.suffix)
            new_name = file.stem[:new_length] + file.suffix
            new_file_path = file.with_name(new_name)
            file.rename(new_file_path)
            new_files_list.append(new_file_path)

            print(
                f"Old name: '{file}'\nNew name: '{new_file_path}'\n-----------------------")

    if new_files_list:
        new_file_names = f"filelist:\"{'|'.join(map(str, new_files_list))}\""
        output = f"Old file names of {path}:\n\n{chr(10).join(map(str, old_files_list))}\n\n-----------------------\n\nNew File Names of {path}:\n\n{new_file_names}"

        print(
            f"Files have been renamed for {path}.\n-----------------------\n")
        pyperclip.copy(new_file_names)
        with (Path.home() / "Desktop" / "Excessively Long Files.txt").open('w') as file:
            file.write(output)
    else:
        print(f"No files renamed for {path}.\n-----------------------\n")


def calculate_image_size(path: Path):
    exif_tool = r"C:\Users\Lance\Desktop\exiftool-12.96_64\exiftool.exe"
    problematic_files = []

    for flac_file in path.glob('*.flac'):
        result = subprocess.run([exif_tool, '-PictureLength', '-s',
                                 '-s', '-s', str(flac_file)], capture_output=True, text=True)

        try:
            image_size_kb = round(int(result.stdout.strip()) / 1024, 2)
        except ValueError:
            print(
                f"Could not convert image size for {flac_file}: '{result.stdout.strip()}'")
            continue

        if image_size_kb > 1024:
            print(f"{flac_file} embedded image size is: {image_size_kb} KB")
            problematic_files.append(flac_file)

    if problematic_files:
        output = f"Files larger than 1MB:\nfilelist:\"{'|'.join(str(file) for file in problematic_files)}\""
        with (Path.home() / "Desktop" / "Files with Giant Embedded Images.txt").open('w') as file:
            file.write(output)
        print(output)
    else:
        print("No files with embedded artwork larger than 1MB")


def main():
    parser = argparse.ArgumentParser(
        description='Utility for renaming files with long paths and calculating embedded image sizes.')
    parser.add_argument(
        '-c', '--command', choices=['calculate_image_size', 'rfr'], help='Command to execute.')
    parser.add_argument('-d', '--directory', type=Path,
                        help='Directory to process.')
    args = parser.parse_args()

    if args.command == 'calculate_image_size':
        calculate_image_size(args.directory)
    elif args.command == 'rfr':
        rename_file_red(args.directory)
    else:
        print("Error: Invalid command provided.")
        exit(1)


if __name__ == "__main__":
    main()
