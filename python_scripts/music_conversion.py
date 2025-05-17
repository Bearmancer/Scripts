import argparse
import contextlib
import subprocess
import logging
import re
import chardet
from tqdm import tqdm
from pathlib import Path
from pathvalidate import sanitize_filename
from ffcuesplitter.user_service import FileSystemOperations

logging.basicConfig(
    level=logging.INFO,
    format="%(levelname)s - %(asctime)s - %(message)s",
    datefmt="%H:%M:%S",
)

FLAC_44 = [(176400, 24), (88200, 24), (44100, 16)]

FLAC_48 = [(192000, 24), (96000, 24), (48000, 16)]

### -------------- UTILITY FUNCTIONS ------------- ###

@contextlib.contextmanager
def directory_context(directory):
    orig_path = directory
    sanitized = sanitize_filename(directory.name)
    renamed = sanitized != directory.name

    if renamed:
        directory = directory.rename(directory.parent / sanitized)

    rename_map = { 
        p: p.with_name(s) for p in directory.rglob("*")
        if p.is_file() and (s := sanitize_filename(p.name)) != p.name and not p.with_name(s).exists()
    }

    try:
        yield directory

    finally:
        for orig, new in rename_map.items():
            if new.exists() and not orig.exists():
                new.rename(orig)

        if renamed:
            directory.rename(orig_path)


def run_command(cmd, cwd=None):
    result = subprocess.run(cmd, cwd=str(cwd) if cwd else None, capture_output=True)

    def decode(data):
        try:
            return data.decode("utf-8")
        except UnicodeDecodeError:
            enc = chardet.detect(data).get("encoding", "utf-8")
            return data.decode(enc, errors="replace")

    stdout, stderr = map(decode, (result.stdout, result.stderr))

    if result.returncode != 0:
        logging.error(f"Command failed: {' '.join(cmd)}")
        logging.error(f"Error output: {stderr}")

    return stdout, stderr


def fix_cue_encoding(cue_path):
    try:
        with open(cue_path, 'rb') as f:
            content = f.read()

        result = chardet.detect(content)

        if result['encoding'] and result['encoding'].lower() != 'utf-8':
            decoded = content.decode(result['encoding'], errors='replace')
            with open(cue_path, 'w', encoding='utf-8') as f:
                f.write(decoded)
    
    except Exception as e:
        logging.warning(f"Error fixing CUE encoding for {cue_path}: {e}")


### -------------- SACD FUNCTIONS ------------- ###

def process_sacd_directory(src: Path, fmt="all"):
    iso_files = list(src.rglob("*.iso"))
    output_dirs = []

    for disc_number, iso in enumerate(iso_files, 1):
        dirs = converto_iso_to_dff_and_cue(iso, src, disc_number)
        output_dirs.extend((folder, disc_number) for folder in dirs)

    logging.info("All ISOs converted to DFF with CUE sheets.")
    print("----------------------")

    parent_folders = set(folder.parent for folder, _ in output_dirs)
    
    for folder, _ in output_dirs:
        dff_directory_conversion(folder)

    print("All DFFs converted to FLAC.\n----------------------")
        
    for parent_folder in parent_folders:
        process_flac_directory(parent_folder, fmt)


def converto_iso_to_dff_and_cue(iso_path, base_dir, disc_number):
    probe_result = run_command(["sacd_extract", "-P", "-i", str(iso_path)], cwd=str(base_dir))[0]

    channel_configs = [
    ("Speaker config: (Multichannel|5|6)", "Multichannel", ["-m", "-e", "-c", "-C"]),
    ("Speaker config: (Stereo|2)", "Stereo", ["-2", "-e", "-c", "-C"])
    ]

    out_dirs = []

    for pattern, suffix, cmd in channel_configs:
        if re.search(pattern, probe_result):
            logging.info(f"Creating {suffix} DFFs with CUE: {iso_path.name}")

            channel_dir = base_dir.parent / f"{base_dir.name} [{suffix}]"
            channel_dir.mkdir(exist_ok=True, parents=True)

            output_disc_dir = channel_dir / f"Disc {disc_number:02d}"

            if output_disc_dir.exists() and list(output_disc_dir.rglob("*.dff")) and list(output_disc_dir.rglob("*.cue")):
                logging.info(f"Skipping {suffix} DFF extraction for {iso_path.name}: DFF and CUE files already exist in {output_disc_dir}")
                out_dirs.append(output_disc_dir)
                continue

            before_dirs = set(d for d in channel_dir.iterdir() if d.is_dir())

            run_command(["sacd_extract", *cmd, "-i", str(iso_path)], cwd=str(channel_dir))

            new_dir = next((d for d in channel_dir.iterdir() if d not in before_dirs and d.is_dir()), None)

            if new_dir:
                new_dir.rename(output_disc_dir)
                out_dirs.append(output_disc_dir)

                for cue_file in output_disc_dir.rglob("*.cue"):
                    fix_cue_encoding(cue_file)

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

    try:
        splitter = FileSystemOperations(
            filename=str(cue_file),
            outputdir=str(cue_file.parent),
            outputformat='flac',
            overwrite='always',
            ffmpeg_loglevel='error',
            ffmpeg_add_params=f'-af volume={gain_db}dB',
        )

        splitter.work_on_temporary_directory()
        logging.info(f"Successfully split tracks from {dff_file.name}")
    
    except Exception as e:
        raise RuntimeError(f"Error processing CUE file {cue_file}: {e}")

    if dff_file.exists():
        dff_file.unlink()


def calculate_gain(dff_files, target_headroom_db=-0.5):
    peaks = []

    for dff in dff_files:
        out = run_command(["ffmpeg", "-nostats", "-i", str(dff), "-af", "volumedetect", "-f", "null", "-"])[1]
        
        if (m := re.search(r"max_volume: (-\d+\.?\d*) dB", out)):
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

    if fmt in ["mp3", "all"]:
        convert_flac_to_mp3(src)

    print("\nConversion successful.\n------------------")


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
        
        cmd = [
            "ffmpeg", "-i", str(f), "-c:a", 
            "libmp3lame", "-b:a", "320k", 
            "-f", "mp3", str(output)
            ]
        
        run_command(cmd)

        f.unlink()


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
    parser.add_argument("-f", "--format", choices=["flac", "mp3", "all"], default="all",
                        help="Output format (default: all)")
    parser.add_argument("directory", type=Path, help="Directory to process")

    args = parser.parse_args()

    directory = Path(args.directory.resolve())

    with directory_context(directory):
        flac_files = list(directory.rglob("*.flac"))
        iso_files = list(directory.rglob("*.iso"))

        if flac_files:
            process_flac_directory(directory, args.format)
        elif iso_files:
            process_sacd_directory(directory, args.format)
        else:
            logging.warning(f"No FLAC or ISO files found in {directory}")

    logging.info("Processing completed")


if __name__ == "__main__":
    main()