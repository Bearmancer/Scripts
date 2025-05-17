import argparse
import os
import subprocess
import logging
import re
import ffmpeg
from tqdm import tqdm
from pathlib import Path
from pathvalidate import sanitize_filename
from unidecode import unidecode
from misc import run_command
from split_cuesheet import process_cue_file

logging.basicConfig(
    level=logging.INFO,
    format="%(levelname)s - %(asctime)s - %(message)s",
    datefmt="%H:%M:%S",
)

FLAC_44 = [(176400, 24), (88200, 24), (44100, 16)]
FLAC_48 = [(192000, 24), (96000, 24), (48000, 16)]


### -------------- HELPER FUNCTION ------------- ###


def prepare_directory(directory: Path):
    sanitize_name = lambda p: p.rename(p.with_name(sanitize_filename(unidecode(p.name)))) or p

    folder_pattern = re.compile(r"^(Disc|CD|Disk)\s?(\d+)$", re.IGNORECASE)
    renamed_count = 0

    for path in directory.rglob("*"):
        sanitize_name(path)

    for folder in (f for f in directory.glob("**/*") if f.is_dir()):
        if match := folder_pattern.match(folder.name):
            prefix, number = match.groups()
            new_name = f"Disc {int(number):02d}"
            new_path = folder.parent / new_name

            try:
                folder.rename(new_path)
                renamed_count += 1
            
            except FileExistsError as e:
                raise FileExistsError(f"Could not rename {folder.name} to {new_name}.\n{e}")

    return directory


def progess_indicator(step: int, message: str):
    core = f"STEP {step}/3: {message}"
    terminal_width = os.get_terminal_size().columns
    border = '=' * terminal_width

    print(border.center(os.get_terminal_size().columns))
    print(core.center(os.get_terminal_size().columns))
    print(border.center(os.get_terminal_size().columns))
    print()


### -------------- SACD FUNCTIONS ------------- ###

def process_sacd_directory(src: Path, fmt="all"):
    iso_files = list(src.rglob("*.iso"))
    output_dirs = []

    progess_indicator(1, "Converting all ISOs → DFF + CUE sheets")

    for disc_number, iso in enumerate(iso_files, 1):
        dirs = converto_iso_to_dff_and_cue(iso, src, disc_number)
        output_dirs.extend((folder, disc_number) for folder in dirs)

    logging.info("All ISOs converted to DFF with CUE sheets.\n----------------------")

    parent_folders = set(folder.parent for folder, _ in output_dirs)

    progess_indicator(2, "CONVERTING DFF + CUE sheet -> FLAC")

    for folder, _ in output_dirs:
        dff_directory_conversion(folder)

    logging.info("All DFFs converted to FLAC.\n----------------------")

    if fmt == "24-bit":
        return

    progess_indicator(3, "Downsampling FLAC to 16-bit.")

    for parent_folder in parent_folders:
        process_flac_directory(parent_folder, fmt)


def converto_iso_to_dff_and_cue(iso_path, base_dir, disc_number):
    probe_result = run_command(["sacd_extract", "-P", "-i", str(iso_path)], cwd=str(base_dir))[0]

    channel_configs = [
        ("Speaker config: (Stereo|2)", "Stereo", ["-2", "-e", "-c", "-C"]),
        ("Speaker config: (Multichannel|5|6)", "Multichannel", ["-m", "-e", "-c", "-C"])
    ]

    out_dirs = []

    for pattern, suffix, cmd in channel_configs:
        if re.search(pattern, probe_result):
            logging.info(f"Extracting {suffix} audio: {iso_path.name}")

            channel_dir = base_dir.parent / f"{base_dir.name} [{suffix}]"
            channel_dir.mkdir(exist_ok=True, parents=True)

            output_disc_dir = channel_dir / f"Disc {disc_number:02d}"

            if output_disc_dir.exists() and list(output_disc_dir.rglob("*.cue")):
                logging.info(f"Skipped: CUE sheet already exists in {output_disc_dir.name}.")
                out_dirs.append(output_disc_dir)
                continue

            before_dirs = set(d for d in channel_dir.iterdir() if d.is_dir())

            run_command(["sacd_extract", *cmd, "-i", str(iso_path)], cwd=str(channel_dir))

            new_dir = next((d for d in channel_dir.iterdir() if d not in before_dirs and d.is_dir()), None)

            if new_dir:
                new_dir.rename(output_disc_dir)
                out_dirs.append(output_disc_dir)

    return out_dirs


def dff_directory_conversion(dff_dir):
    dff_files = list(dff_dir.rglob("*.dff"))
    cue_files = list(dff_dir.rglob("*.cue"))

    if not cue_files:
        raise RuntimeError(f"No CUE files found in {dff_dir.name}")

    dr = calculate_gain(dff_files)

    for cue_file in cue_files:
        convert_dff_to_flac(cue_file, dr)
        

def convert_dff_to_flac(cue_file, gain_db):
    logging.info(f"Converting {cue_file.name} to FLAC.")
    
    dff_file = cue_file.with_suffix('.dff')
    
    process_cue_file(cue_file, gain_db)    

    if dff_file.exists():
        dff_file.unlink()


def calculate_gain(dff_files, target_headroom_db=-0.5):
    peaks = []

    for dff in dff_files:
        _, err = (
            ffmpeg
            .input(str(dff))
            .audio
            .filter('volumedetect')
            .output('null', format='null')
            .run(capture_stderr=True)
        )

        if isinstance(err, (bytes, bytearray)):
            err = err.decode('utf-8', errors='ignore')

        if m := re.search(r"max_volume: (-\d+\.?\d*) dB", err):
            peaks.append(float(m.group(1)))
    
    if not peaks:
        raise RuntimeError("Could not detect any peak levels")
    
    return target_headroom_db - max(peaks)


### -------------- CONVERSION FUNCTIONS ------------- ###


def process_flac_directory(src, fmt="all"):
    logging.info(f"Processing FLAC directory: {src.stem}")

    flac_files = list(src.rglob("*.flac"))

    if not flac_files:
        logging.warning("No FLAC files found")
        return

    bit_depths_and_rates = [get_metadata(f) for f in flac_files]
    bit_depths, sample_rates = zip(*bit_depths_and_rates)

    bd, sr = min(bit_depths), min(sample_rates)
    logging.info(f"Detected: {bd}-bit/{sr}Hz")

    tiers = get_conversion_tiers(sr, bd)

    for tier in tiers:
        process_tier(src, tier, fmt)


def process_tier(src, tier, fmt):
    sample_rate, bit_depth = tier
    suffix = f"{bit_depth} - {sample_rate/1000:.1f}"
    dest = src.parent / f"{src.name} [{suffix}]"

    exclusions = ["*.log", "*.m3u", "*.cue", "*.md5"]

    rc = subprocess.run(
        ["robocopy", str(src), str(dest), "/S", "/XF", *exclusions],
        stdout=subprocess.DEVNULL
    ).returncode

    if rc >= 8:
        logging.error(f"Robocopy failed with code {rc}")
        raise RuntimeError(f"Robocopy failed with code {rc}")

    flac_files = list(dest.rglob("*.flac"))

    for f in tqdm(flac_files, desc=f"Converting {src.name} to {bit_depth}-bit/{sample_rate} Hz"):
        downsample_flac(f, tier)

    logging.info(f"\nConverted to {bit_depth}-{sample_rate / 1000:.1f}KHz FLAC successfully.\n------------------")

    if fmt in ["mp3", "all"]:
        convert_flac_to_mp3(src)


def downsample_flac(file, tier):
    sample_rate, bit_depth = tier
    temp_a = file.with_name("a.flac")
    temp_b = file.with_name("b.flac")

    try:
        file.rename(temp_a)

        cmd = [
            "sox", "-S", str(temp_a),
            "-b", str(bit_depth),
            "-R", "-G", str(temp_b),
            "rate", "-v", "-L", str(sample_rate)
            ]

        run_command(cmd)
        temp_a.unlink()
        temp_b.rename(file)

    finally:
        if temp_a.exists():
            temp_a.rename(file)

        if temp_b.exists():
            temp_b.unlink()


def convert_flac_to_mp3(src):
    logging.info(f"Converting FLAC to MP3 in {src.name}")

    mp3_dir = src.parent / f"{src.name} [MP3]"
    mp3_dir.mkdir(exist_ok=True)

    flac_files = list(src.rglob("*.flac"))

    for f in tqdm(flac_files, desc=f"Converting FLAC to MP3 in {src.name}"):
        output = mp3_dir / f.with_suffix(".mp3").name

        (
            ffmpeg
            .input(str(f))
            .output(
                str(output),
                acodec='libmp3lame',
                audio_bitrate='320k',
                format='mp3',
            )
            .run()
        )

        f.unlink()

    logging.info(f"Successfully converted to MP3.\n------------------")


def get_metadata(file):
    out = run_command([
        "ffprobe", "-select_streams", "a:0",
        "-show_entries", "stream=bits_per_raw_sample,sample_rate",
        "-of", "default=nw=1", str(file)
    ])[0]

    extract = lambda key: int(m.group(1)) if (m := re.search(f"{key}=(\\d+)", out)) else None
    
    return extract("bits_per_raw_sample"), extract("sample_rate")


def get_conversion_tiers(sample_rate, bit_depth):
    tiers = FLAC_44 if sample_rate in {44100, 88200, 176400} else FLAC_48

    for i, (tier_sr, tier_bd) in enumerate(tiers):
        if sample_rate > tier_sr and bit_depth >= tier_bd:
            selected_tiers = tiers[i:]
            return selected_tiers

    raise ValueError(f"No suitable conversion tier found for {bit_depth}-bit/{sample_rate}Hz")


### -------------- MAIN FUNCTION ------------- ###


def main():
    parser = argparse.ArgumentParser(description="Audio processing tool")
    parser.add_argument("mode", choices=["convert", "extract"],
                        help="Processing mode: convert (FLAC) or extract (SACD)")
    parser.add_argument("-f", "--format", choices=["24-bit", "flac", "mp3", "all", "none"], default="all",
                        help="Output format (default: all)")
    parser.add_argument("directory", type=Path, help="Directory to process")

    args = parser.parse_args()

    directory = prepare_directory(args.directory.resolve())

    if args.mode == "convert":
        process_flac_directory(directory, args.format)

    elif args.mode == "extract":
        process_sacd_directory(directory, args.format)

    logging.info("Processing completed")


if __name__ == "__main__":
    main()