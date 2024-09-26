import subprocess, re, sys
from pathlib import Path

def extract_sacds(path):
    iso_files = list(Path(root_directory).rglob("*.iso"))
    total_discs = len(iso_files)
    sacd_extract = Path(r"C:\Users\Lance\AppData\Local\Personal\sacd_extract\sacd_extract.exe")

    move_iso()

    disc_number = 0

    for iso_file in iso_files:
        if total_discs > 1:
            disc_number += 1

        parent_folder = iso_file.parent

        if "Disc" in parent_folder.name or "CD" in parent_folder.name:
            parent_folder = parent_folder.parent

        result = subprocess.run([sacd_extract, '-P', '-i', str(iso_file)], capture_output=True, text=True)
        print(result)
        output = result.stdout

        if "Multichannel" in output or "5 Channel" in output or "6 Channel" in output:
            multi_channel_parent_folder = parent_folder / "[SACD - 5.1 - 24-88.1]"
            multi_channel_parent_folder.mkdir(exist_ok=True)

            subprocess.run([sacd_extract, '-m', '-p', '-c', '-i', str(iso_file)])
            disc_location = get_disc_location(multi_channel_parent_folder, disc_number)

            for dff_file in path().rglob("*.dff"):
                dff_file.rename(disc_location / dff_file.name)

            dff_to_flac(multi_channel_parent_folder)

        if "Stereo" in output or "2 Channel" in output:
            stereo_parent_folder = parent_folder / "[SACD - 2.0 - 24-88.1]"
            stereo_parent_folder.mkdir(exist_ok=True)

            subprocess.run([sacd_extract, '-2', '-p', '-c', '-i', str(iso_file)])
            disc_location = get_disc_location(stereo_parent_folder, disc_number)

            for dff_file in path().rglob("*.dff"):
                dff_file.rename(disc_location / dff_file.name)
            dff_to_flac(stereo_parent_folder)
        else:
            print(f"Audio for {iso_file} is neither 5ch nor stereo.")

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
    for folder in Path(input_folder).iterdir():
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
                        print(f"{trimmed_flac_file} not found")

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
        print(f"Unequal number of FLAC and DFF files in {directory}")

def move_iso():
    folders = {folder for folder in Path('.').rglob('*.iso') if folder.parent.name.count('.') > 1}

    for folder in folders:
        iso_files = list(folder.glob("*.iso"))

        for index, iso_file in enumerate(iso_files, start=1):
            new_folder = folder / f"Disc {index}"
            new_folder.mkdir(exist_ok=True)
            iso_file.rename(new_folder / iso_file.name)

if __name__ == "__main__":
    if len(sys.argv) != 3:
        print("Usage: script.py <root_dir> <FolderPath>")
        sys.exit(1)

    root_directory = Path (sys.argv[1])
    extract_sacds(root_directory)