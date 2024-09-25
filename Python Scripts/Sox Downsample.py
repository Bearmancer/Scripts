import subprocess, sys
from pathlib import Path

def sox_downsample(folder: Path):
    original = folder / "original"
    converted = folder / "converted"
    problem_files = []

    original.mkdir(exist_ok=True)
    converted.mkdir(exist_ok=True)

    for file in folder.glob('*.flac'):
        flac_info = subprocess.run(['sox', '--i', str(file)], capture_output=True, text=True)
        flac_info_output = flac_info.stdout.strip()

        precision_match = [line for line in flac_info_output.splitlines() if "Precision" in line]
        precision = precision_match[0].split(":")[-1].strip() if precision_match else None
        
        sample_rate_match = [line for line in flac_info_output.splitlines() if "Sample Rate" in line]
        sample_rate = sample_rate_match[0].split(":")[-1].strip() if precision_match else None

        if precision is None or sample_rate is None:
            continue

        actions = {
            "24-bit, 192000": lambda: subprocess.run(['sox', '-S', str(file), '-R', '-G', '-b', '16', str(converted/file.name), 'rate', '-v', '-L', '48000', 'dither']),
            "24-bit, 96000": lambda: subprocess.run(['sox', '-S', str(file), '-R', '-G', '-b', '16', str(converted/file.name), 'rate', '-v', '-L', '48000', 'dither']),
            "24-bit, 48000": lambda: subprocess.run(['sox', '-S', str(file), '-R', '-G', '-b', '16', str(converted/file.name), 'dither']),
            "24-bit, 176400": lambda: subprocess.run(['sox', '-S', str(file), '-R', '-G', '-b', '16', str(converted/file.name), 'rate', '-v', '-L', '44100', 'dither']),
            "24-bit, 88200": lambda: subprocess.run(['sox', '-S', str(file), '-R', '-G', '-b', '16', str(converted/file.name), 'rate', '-v', '-L', '44100', 'dither']),
            "24-bit, 44100": lambda: subprocess.run(['sox', '-S', str(file), '-R', '-G', '-b', '16', str(converted/file.name), 'dither']),
            "16-bit, 44100": lambda: (print(f"{file.name} is already 16-bit."), file.rename(original / file.name)),
            "16-bit, 48000": lambda: (print(f"{file.name} is already 16-bit"), file.rename(original / file.name)),
        }

        action = actions.get(f"{precision}, {sample_rate}")
        if (action):
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
        converted_file.rename(folder/converted_file.name)

    if len(list(original.glob('*.flac'))) == len(list(folder.glob('*.flac'))):
        for dir_path in [converted, original]:
            dir_path.rmdir()

if __name__ == "__main__":
    if len(sys.argv) > 1:
        sox_downsample(Path(sys.argv[1]))
    else:
        print("Please provide a directory path.")