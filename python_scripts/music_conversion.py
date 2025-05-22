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

    for path in directory.rglob("*"):
        sanitize_name(path)

    for folder in (f for f in directory.glob("**/*") if f.is_dir()):
        if match := folder_pattern.match(folder.name):
            prefix, number = match.groups()
            new_name = f"Disc {int(number):02d}"
            new_path = folder.parent / new_name

            try:
                folder.rename(new_path)

            except FileExistsError as e:
                raise FileExistsError(f"Could not rename {folder.name} to {new_name}.\n{e}")

    return directory


def progress_indicator(step: int, message: str):
    core = f"STEP {step}: {message}"
    terminal_width = os.get_terminal_size().columns
    border = '=' * terminal_width

    print(border.center(os.get_terminal_size().columns))
    print(core.center(os.get_terminal_size().columns))
    print(border.center(os.get_terminal_size().columns))
    print()


def create_output_directory(directory, suffix):
    destination = directory.parent / f"{directory.name} [{suffix}]"

    exclusions = ["*.log", "*.m3u", "*.cue", "*.md5"]

    rc = subprocess.run(
        ["robocopy", str(directory), str(destination), "/S", "/XF", *exclusions],
        stdout=subprocess.DEVNULL
    ).returncode

    if rc >= 8:
        logging.error(f"Robocopy failed with code {rc}")
        raise RuntimeError(f"Robocopy failed with code {rc}")

    return destination


### -------------- SACD FUNCTIONS ------------- ###


def process_sacd_directory(directory: Path, fmt="all"):
    iso_files = list(directory.rglob("*.iso"))
    output_dirs = []

    progress_indicator(1, "Converting all ISOs → DFF + CUE sheets")

    for disc_number, iso in enumerate(iso_files, 1):
        dirs = convert_iso_to_dff_and_cue(iso, directory, disc_number)
        output_dirs.extend((folder, disc_number) for folder in dirs)

    progress_indicator(2, "CONVERTING DFF + CUE sheet -> FLAC")

    parent_folders = set(folder.parent for folder, _ in output_dirs)

    for folder, _ in output_dirs:
        convert_dff_to_flac(folder)

    for parent_folder in parent_folders:
        convert_audio(3, parent_folder, fmt)


def convert_iso_to_dff_and_cue(iso_path, base_dir, disc_number):
    probe_result = run_command(["sacd_extract", "-P", "-i", str(iso_path)], cwd=str(base_dir))[0]

    channel_configs = [
        ("Speaker config: (Stereo|2)", "Stereo", ["-2", "-e", "-c", "-C"]),
        ("Speaker config: (Multichannel|5|6)", "Multichannel", ["-m", "-e", "-c", "-C"])
    ]

    out_dirs = []

    for pattern, suffix, cmd in channel_configs:
        if re.search(pattern, probe_result):
            channel_dir = base_dir.parent / f"{base_dir.name} [{suffix}]"
            channel_dir.mkdir(exist_ok=True, parents=True)

            output_disc_dir = channel_dir / f"Disc {disc_number:02d}"

            if output_disc_dir.exists() and list(output_disc_dir.rglob("*.cue")):
                logging.info(f"Skipped: CUE sheet already exists for {iso_path.name}.")
                out_dirs.append(output_disc_dir)
                continue

            old_dirs = set(Path(channel_dir).glob("*/"))

            logging.info(f"Found {suffix} audio: {iso_path.name}.")

            run_command(["sacd_extract", *cmd, "-i", str(iso_path)], cwd=str(channel_dir))

            new_dirs = set(Path(channel_dir).glob("*/"))
            new_dir = next(iter(new_dirs - old_dirs), None)

            if new_dir:
                new_dir.rename(output_disc_dir)
                out_dirs.append(output_disc_dir)

            else:
                logging.warning(f"No new directories were created by sacd_extract for {iso_path.name}.")

    return out_dirs


def convert_dff_to_flac(dff_dir):
    cue_file = next(dff_dir.rglob("*.cue"))
    dff_file = next(dff_dir.rglob("*.dff"))

    if not cue_file.exists():
        raise FileNotFoundError(f"Could not find {cue_file.name}")

    gain_db = calculate_gain(dff_file)

    process_cue_file(cue_file, gain_db)

    # if dff_file.exists():
    #     dff_file.unlink()


def calculate_gain(dff_file, target_headroom_db=-0.5):
    if not dff_file.exists():
        raise FileNotFoundError(f"Could not find {dff_file.name}")

    peaks = []

    _, error = (
        ffmpeg
        .input(str(dff_file))
        .audio
        .filter('volumedetect')
        .output('null', format='null')
        .run(capture_stderr=True)
    )

    if isinstance(error, (bytes, bytearray)):
        error = error.decode('utf-8', errors='ignore')

    if m := re.search(r"max_volume: (-\d+\.?\d*) dB", error):
        peaks.append(float(m.group(1)))

    if not peaks:
        raise RuntimeError("Could not detect any peak levels.")

    return target_headroom_db - max(peaks)


### -------------- CONVERSION FUNCTIONS ------------- ###


def convert_audio(current_step, directory, fmt="all"):
    flac_files = list(directory.rglob("*.flac"))

    if not flac_files:
        logging.warning("No FLAC files found")
        return

    bd, sr = next((int(m['bits_per_raw_sample']), int(m['sample_rate']))
                  for f in flac_files
                  if (m := get_metadata(f)))

    logging.info(f"Detected: {bd}-bit/{sr}Hz")

    flac_tiers = get_flac_tiers(sr, bd)

    for flac_tier in flac_tiers:
        progress_indicator(current_step, f"Converting {directory} to {flac_tier}")
        flac_directory_conversion(directory, flac_tier)
        current_step += 1

    if fmt in ["mp3", "all"]:
        progress_indicator(current_step, "Converting FLAC to MP3")
        convert_to_mp3(directory)


def flac_directory_conversion(directory, tier):
    sample_rate, bit_depth = tier
    suffix = f"{bit_depth} - {sample_rate / 1000:.1f}"

    destination = create_output_directory(directory, suffix)

    sample_rate, bit_depth = tier
    
    flac_files = list(destination.rglob("*.flac"))

    for f in tqdm(flac_files, desc=f"Converting {directory.name} to {bit_depth}-bit/{sample_rate} Hz"):
        downsample_flac(f, tier)


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


def convert_to_mp3(directory):
    flac_files = list(directory.rglob("*.flac"))

    if not flac_files:
        raise FileNotFoundError(f"No FLAC files found.")

    destination = create_output_directory(directory, "MP3")

    for f in tqdm(flac_files, desc=f"Converting FLAC to MP3 in {directory.name}"):
        output = destination / f.with_suffix(".mp3").name

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


def get_metadata(file):
    try:
        probe_result = ffmpeg.probe(str(file))

        audio_stream = next(
                stream for stream in probe_result.get('streams', [])
                if stream.get('codec_type') == 'audio'
        )

        return {
            'bits_per_raw_sample': audio_stream.get('bits_per_raw_sample'),
            'sample_rate': audio_stream.get('sample_rate')
        }

    except Exception as e:
        raise Exception(f"Error reading metadata for {file}: {e}")


def get_flac_tiers(sample_rate, bit_depth):
    tiers = FLAC_44 if sample_rate in {44100, 88200, 176400} else FLAC_48

    for i, (tier_sr, tier_bd) in enumerate(tiers):
        if sample_rate > tier_sr and bit_depth >= tier_bd:
            return tiers[i:]

    raise ValueError(f"No suitable conversion tier found for {bit_depth}-bit/{sample_rate}Hz")


### -------------- MAIN FUNCTION ------------- ###


def main():
    parser = argparse.ArgumentParser(description="Audio format conversion and SACD extraction tool.")

    parser.add_argument("mode", choices=["convert", "extract"], 
        help="Select mode: 'convert' to process FLAC files or 'extract' to rip SACD ISOs to FLAC."
    )

    parser.add_argument(
        "-f", "--format", choices=["24-bit", "flac", "mp3", "all"],
        default="all",
        help='Output format(s): "24-bit" for high-res only, "flac" for 24- and 16-bit output, '
             '"mp3" for 320kbps MP3 alone, "all" for everything (default).'
    )

    parser.add_argument(
        "directory", type=Path, help="Path to input directory containing audio files to process"
    )

    args = parser.parse_args()

    mode = args.mode
    directory = args.directory.resolve()

    if not directory.exists():
        raise FileNotFoundError(f"Directory {directory} does not exist.")

    directory = prepare_directory(directory)
    fmt = args.format

    if mode == "convert":
        convert_audio(1, directory, fmt)
    elif mode == "extract":
        process_sacd_directory(directory, fmt)

    logging.info("Processing completed")


if __name__ == "__main__":
    main()