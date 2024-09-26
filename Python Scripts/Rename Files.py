import sys
from pathlib import Path

def main(directory: Path):
    if not directory.exists() or not directory.is_dir():
        print(f"Error: The specified directory '{directory}' does not exist.")
        sys.exit(1)

    root_directory = directory.parent
    file_list = []
    old_file_names = "Old File Names:\n"

    for file in directory.rglob('*'):
        relative_path = file.relative_to(root_directory)

        if len(relative_path) > 180:
            old_file_names.join(f"{file}\n")

            new_length = 180 - (len(relative_path) - len(file.name))
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
        desktop_path = Path.home() / "Desktop" / f"Files Renamed - {directory.name}.txt"

        with desktop_path.open('a') as log_file:
            log_file.write(output)

        print(f"Files have been renamed for {directory}.\n-----------------------")
    else:
        print(f"No files renamed for {directory}.\n-----------------------")

if __name__ == "__main__":
    if len(sys.argv) > 1:
        main(Path(sys.argv[1]))
    else:
        print("Please provide a directory path.")