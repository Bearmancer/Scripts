from pathlib import Path
import shutil, subprocess, sys
path: Path

def sox_conversion():
    original = path / "original"
    converted = path / "converted"
    problem_files = []

    original.mkdir(exist_ok=True)
    converted.mkdir(exist_ok=True)

    for file in path.glob('*.flac'):
        flac_info = subprocess.run(['sox', '--i', str(file)], capture_output=True, text=True)
        flac_info_output = flac_info.stdout.strip()

        precision_match = [line for line in flac_info_output.splitlines() if "Precision" in line]
        precision = precision_match[0].split(":")[-1].strip() if precision_match else None

        sample_rate_match = [line for line in flac_info_output.splitlines() if "Sample Rate" in line]
        sample_rate = sample_rate_match[0].split(":")[-1].strip() if precision_match else None

        if precision is None or sample_rate is None:
            continue

        actions = {
            "24-bit, 192000": lambda: subprocess.run(['sox', '-S', str(file), '-R', '-G', '-b', '16', str(converted / file.name), 'rate', '-v', '-L', '48000', 'dither']),
            "24-bit, 96000": lambda: subprocess.run(['sox', '-S', str(file), '-R', '-G', '-b', '16', str(converted / file.name), 'rate', '-v', '-L', '48000', 'dither']),
            "24-bit, 48000": lambda: subprocess.run(['sox', '-S', str(file), '-R', '-G', '-b', '16', str(converted / file.name), 'dither']),
            "24-bit, 176400": lambda: subprocess.run(['sox', '-S', str(file), '-R', '-G', '-b', '16', str(converted / file.name), 'rate', '-v', '-L', '44100', 'dither']),
            "24-bit, 88200": lambda: subprocess.run(['sox', '-S', str(file), '-R', '-G', '-b', '16', str(converted / file.name), 'rate', '-v', '-L', '44100', 'dither']),
            "24-bit, 44100": lambda: subprocess.run(['sox', '-S', str(file), '-R', '-G', '-b', '16', str(converted / file.name), 'dither']),
            "16-bit, 48000": lambda: subprocess.run(shutil.copy(file, converted / file.name)),
            "16-bit, 44100": lambda: subprocess.run(shutil.copy(file, converted / file.name)),
        }

        action = actions.get(f"{precision}, {sample_rate}")
        if action:
            action()
            file.rename(original / file.name)
        else:
            print(f"No action found for {file} - Bit Depth: {precision}, Sample Rate: {sample_rate}")
            problem_files.append(file)

    if problem_files:
        print("The following files' bit-depth and sample rate could not be converted:")
        for problem_file in problem_files:
            print(f"{problem_file}")

    for converted_file in converted.iterdir():
        converted_file.rename(path / converted_file.name)

    if len(list(path.glob('*.flac'))) == len(list(original.glob('*.flac'))):
        for dir_path in [converted, original]:
            shutil.rmtree(dir_path)

def rename_file_red():
    if not path.exists() or not path.is_dir():
        print(f"Error: The specified path '{path}' does not exist.")
        sys.exit(1)

    root_path = path.parent
    file_list = []
    old_file_names = "Old File Names:\n"

    for file in path.rglob('*'):
        relative_path = file.relative_to(root_path)

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
        desktop_path = Path.home() / "Desktop" / f"Files Renamed - {path.name}.txt"

        with desktop_path.open('a') as log_file:
            log_file.write(output)

        print(f"Files have been renamed for {path}.\n-----------------------")
    else:
        print(f"No files renamed for {path}.\n-----------------------")

def calculate_image_size():
    exif_tool = Path(r"C:\Users\Lance\Desktop\exiftool-12.96_64\exiftool.exe")
    problematic_files = []

    for flac_file in path.glob('*.flac'):
        image_size = subprocess.run([exif_tool, '-PictureLength', '-s', '-s', '-s', str(flac_file)],
                                    capture_output=True, text=True)

        image_size_kb = round(int(image_size.stdout.strip()) / 1024, 2)

        if image_size_kb > 1024:
            print(f"{flac_file} embedded image size is: {image_size_kb} KB")
            problematic_files.append(image_size_kb)

    output = f"Files larger than 1MB:\nfilelist:\"{'|'.join(str(file) for file in problematic_files)}\""

    print(output)

    with (Path.home() / "Desktop" / "problematic_files.txt").open('w') as file:
        file.write(output.strip())

if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Invalid number of arguments supplied.")
        exit()

    path = Path(sys.argv[1])
    method = sys.argv[2]

    if method == "CalculateImageSize":
        calculate_image_size()
    elif method == "SoxConversion":
        sox_conversion()
    elif method == "RenameFileRed" or method == "rfr":
        rename_file_red()
    else:
        print("Invalid argument entered.")