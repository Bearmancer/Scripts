import subprocess, re, sys, unicodedata, os
from pathlib import Path
from datetime import datetime

def log_to_file(message):
    log_file = Path(os.path.expanduser("~/Desktop/Conversion.log"))
    timestamp = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    with open(log_file, "a", encoding="utf-8") as f:
        f.write(f"{timestamp}: {message}")

def extract_sacds(path):
    log_to_file("SACD INVOKED:\n")

    original_name = path.name

    if (new_name := ''.join(c for c in unicodedata.normalize('NFD', original_name) if
                            unicodedata.category(c) != 'Mn')) != original_name:
        path = path.rename(path.with_name(new_name))
        log_to_file(f"Renamed folder from '{original_name}' to '{new_name}' for processing.")

    iso_files = list(path.rglob("*.iso"))
    sacd_extract = Path(r"C:\Users\Lance\AppData\Local\Personal\sacd_extract\sacd_extract.exe")

    i = 0

    for iso_file in iso_files:
        i += 1
        command = [str(sacd_extract), '-P', '-i', str(iso_file)]
        output = subprocess.run(command, capture_output=True, text=True, cwd=str(path)).stdout

        album_name = iso_file.parent.parent.name
        starting_directory = iso_file.parent.parent

        if len(iso_files) > 1:
            if "Multichannel" in output or "5 Channel" in output or "6 Channel" in output:
                starting_directory = iso_file.parent.parent.parent / f"{album_name} (Multichannel)"
                starting_directory.mkdir(parents=True, exist_ok=True)
                multichannel_path = starting_directory / f"Disc {i}"
                multichannel_path.mkdir(parents=True, exist_ok=True)
                log_to_file(f"Multichannel Path: {multichannel_path}\n--------------------")
                subprocess.run([str(sacd_extract), '-m', '-p', '-c', '-i', str(iso_file)],
                               cwd=str(multichannel_path))
                dff_to_flac(multichannel_path)

            if "Stereo" in output or "2 Channel" in output:
                starting_directory = iso_file.parent.parent.parent / f"{album_name} (Stereo)"
                starting_directory.mkdir(parents=True, exist_ok=True)
                log_to_file(f"Starting Directory: {starting_directory}\n--------------------")
                stereo_path = starting_directory / f"Disc {i}"
                stereo_path.mkdir(parents=True, exist_ok=True)
                log_to_file(f"Stereo Path: {stereo_path}")
                subprocess.run([str(sacd_extract), '-2', '-p', '-c', '-i', str(iso_file)], cwd=str(stereo_path))
                dff_to_flac(stereo_path)
        else:
            if "Multichannel" in output or "5 Channel" in output or "6 Channel" in output:
                starting_directory.mkdir(parents=True, exist_ok=True)
                multichannel_path = starting_directory / "(Multichannel)"
                multichannel_path.mkdir(parents=True, exist_ok=True)
                subprocess.run([str(sacd_extract), '-m', '-p', '-c', '-i', str(iso_file)], cwd=str(multichannel_path))
                dff_to_flac(multichannel_path)

            if "Stereo" in output or "2 Channel" in output:
                starting_directory.mkdir(parents=True, exist_ok=True)
                stereo_path = starting_directory / "(Stereo)"
                stereo_path.mkdir(parents=True, exist_ok=True)
                subprocess.run([str(sacd_extract), '-2', '-p', '-c', '-i', str(iso_file)], cwd=str(stereo_path))
                dff_to_flac(stereo_path)

        if not any(keyword in output for keyword in ["Stereo", "2 Channel", "Multichannel", "5 Channel", "6 Channel"]):
            log_to_file(f"Audio for {iso_file} is neither multichannel nor stereo.")

    if new_name != original_name:
        try:
            path.rename(path.parent / original_name)
            log_to_file(f"Renamed folder back to original name: '{original_name}'")
        except OSError as e:
            log_to_file(f"Error renaming folder back to original name: {e}")

    log_to_file(f"SACD EXTRACTION COMPLETE for {path}.\n")

def get_disc_location(parent_folder, disc_number):
    disc_location = parent_folder

    if disc_number > 0:
        sub_directory_path = parent_folder / f"Disc {disc_number}"
        sub_directory_path.mkdir(exist_ok=True)
        disc_location = sub_directory_path

    return disc_location

def check_dynamic_range(directory):
    dr_gains = []

    for file in Path(directory).rglob("*.dff"):
        output = subprocess.run(['ffmpeg', '-i', str(file), '-af', 'volumedetect', '-f', 'null', '-'],
                                capture_output=True, text=True)
        dr_gain = [float(match.group(1)) for match in re.finditer(r'max_volume: (-?\d+(\.\d+)?) dB', output.stderr)]
        dr_gains.extend(dr_gain)

    max_dr_gain = max(dr_gains) if dr_gains else None

    return max_dr_gain

def dff_to_flac(input_folder):
    print(f"\nDFF to FLAC function invoked for {input_folder}")
    for folder in Path(input_folder).iterdir():
        print(f"\nNow in subfolder: {folder}")
        if folder.is_dir():
            files = list(folder.glob("*.dff"))
            dynamic_range = check_dynamic_range(folder)

            if dynamic_range is not None:
                dynamic_range -= 0.5

                for file in files:
                    flac_file = file.with_suffix('.flac')

                    subprocess.run(['ffmpeg', '-i', str(file), '-vn', '-c:a', 'flac', '-sample_fmt', 's32', '-ar', '88200',
                                    '-af', f"volume={dynamic_range}", '-dither_method', 'triangular', str(flac_file)])

                    trimmed_flac_file = flac_file.with_name(flac_file.stem + ' - Trimmed.flac')

                    subprocess.run(
                        ['sox', str(flac_file), str(trimmed_flac_file), 'trim', '0.0065', 'reverse', 'silence', '1', '0',
                         '0%', 'trim', '0.0065', 'reverse', 'pad', '0.0065', '0.2'])

                    if trimmed_flac_file.exists():
                        flac_file.unlink()
                        trimmed_flac_file.rename(flac_file)
                    else:
                        print(f"\n{trimmed_flac_file} not found")

            check_dff_and_flac(folder)

def check_dff_and_flac(directory):
    flac_count = len(list(directory.glob("*.flac")))
    dff_files = list(directory.glob("*.dff"))
    dff_count = len(dff_files)

    print(f"FLAC files count: {flac_count}")
    print(f"DFF files count: {dff_count}")

    if flac_count == dff_count:
        print("Equal number of FLAC and DFF files.")
        for dff_file in dff_files:
            dff_file.unlink()
    else:
        print(f"\nUnequal number of FLAC and DFF files in {directory}")

def move_iso():
    folders = {folder for folder in Path('.').rglob('*.iso') if folder.parent.name.count('.') > 1}

    for folder in folders:
        iso_files = list(folder.glob("*.iso"))

        for index, iso_file in enumerate(iso_files, start=1):
            new_folder = folder / f"Disc {index}"
            new_folder.mkdir(exist_ok=True)
            iso_file.rename(new_folder / iso_file.name)

if __name__ == "__main__":
    if len(sys.argv) != 2:
        print("Usage: script.py <root_dir> <FolderPath>")
        sys.exit(1)

    extract_sacds(Path(sys.argv[1]))