import shutil
import subprocess
import sys
import unidecode
import chardet
from pathlib import Path
from pathvalidate import sanitize_filename


def sox_downsample(path: Path):
    original = path / "original"
    converted = path / "converted"

    original.mkdir(exist_ok=True)
    converted.mkdir(exist_ok=True)

    for file in path.glob('*.flac'):
        old_file_name = file.name

        file = file.rename(file.parent / sanitize_filename(unidecode.unidecode(file.name)))

        flac_info = subprocess.run(['sox', '--i', str(file)], capture_output=True)

        detected_encoding = chardet.detect(flac_info.stdout)['encoding']
        flac_info_output = flac_info.stdout.decode(detected_encoding)

        precision_match = [line for line in flac_info_output.splitlines() if "Precision" in line]
        precision = precision_match[0].split(":")[-1].strip() if precision_match else None

        sample_rate_match = [line for line in flac_info_output.splitlines() if "Sample Rate" in line]
        sample_rate = sample_rate_match[0].split(":")[-1].strip() if sample_rate_match else None

        if precision is None or sample_rate is None:
            print(f"Missing precision or sample rate information for file: {file}")
            exit(1)

        actions = {
            "24-bit, 192000": lambda: subprocess.run(['sox', '-S', str(file), '-R', '-G', '-b', '16', str(converted / old_file_name), 'rate', '-v', '-L', '48000', 'dither']),
            "24-bit, 96000": lambda: subprocess.run(['sox', '-S', str(file), '-R', '-G', '-b', '16', str(converted / old_file_name), 'rate', '-v', '-L', '48000', 'dither']),
            "24-bit, 48000": lambda: subprocess.run(['sox', '-S', str(file), '-R', '-G', '-b', '16', str(converted / old_file_name), 'dither']),
            "24-bit, 176400": lambda: subprocess.run(['sox', '-S', str(file), '-R', '-G', '-b', '16', str(converted / old_file_name), 'rate', '-v', '-L', '44100', 'dither']),
            "24-bit, 88200": lambda: subprocess.run(['sox', '-S', str(file), '-R', '-G', '-b', '16', str(converted / old_file_name), 'rate', '-v', '-L', '44100', 'dither']),
            "24-bit, 44100": lambda: subprocess.run(['sox', '-S', str(file), '-R', '-G', '-b', '16', str(converted / old_file_name), 'dither']),
            "16-bit, 48000": lambda: shutil.copy(file, converted / old_file_name),
            "16-bit, 44100": lambda: shutil.copy(file, converted / old_file_name)
        }

        action = actions.get(f"{precision}, {sample_rate}")

        if action:
            action()
            file.rename(original / old_file_name)
        else:
            print(f"No action found for {file} - Bit Depth: {precision}, Sample Rate: {sample_rate}")
            exit(1)

    for flac in converted.glob('*.flac'):
        shutil.move(flac, path)

    if len(list(path.glob('*.flac'))) == len(list(original.glob('*.flac'))):
        for dir_path in [converted, original]:
            shutil.rmtree(dir_path)


def main():
    if len(sys.argv) < 2:
        print("Usage: script.py <root_dir>")
        exit(1)

    directory = Path(sys.argv[1])

    process_all_subfolders = sys.argv[2].lower() == 'true' if len(sys.argv) > 2 else True
    directories_to_process = directory.iterdir() if process_all_subfolders else [directory]

    for subdir in directories_to_process:
        if subdir.is_dir():
            sox_downsample(subdir)


if __name__ == "__main__":
    main()