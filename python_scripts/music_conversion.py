import argparse
import contextlib
import logging
import re
import subprocess
import sys
import chardet
from tqdm import tqdm
from pathlib import Path
from pathvalidate import sanitize_filename

logging.basicConfig(
    level=logging.INFO,
    format="%(levelname)s - %(asctime)s - %(message)s",
    datefmt="%H:%M:%S",
)

MP3_TIER = {
    "desc": "320kbps MP3",
    "quality_setting": "320k",
    "suffix": "MP3",
    "format": "mp3",
}

BASE_FLAC_CONFIGS = {
    "44.1": {
        "desc": "16-bit/44.1kHz FLAC",
        "bit_depth": 16,
        "quality_setting": 44100,
        "suffix": "16-44.1",
        "format": "flac"
    },
    "48": {
        "desc": "16-bit/48kHz FLAC",
        "bit_depth": 16,
        "quality_setting": 48000,
        "suffix": "16-48",
        "format": "flac"
    }
}

TIER_CONFIG = {
    44100: [BASE_FLAC_CONFIGS["44.1"]],
    48000: [BASE_FLAC_CONFIGS["48"]],
    88200: [BASE_FLAC_CONFIGS["44.1"]],
    96000: [BASE_FLAC_CONFIGS["48"]],
    176400: [
        {
            "desc": "24-bit/88.2kHz FLAC",
            "bit_depth": 24,
            "quality_setting": 88200,
            "suffix": "24-88.2",
            "format": "flac"
        },
        BASE_FLAC_CONFIGS["44.1"]
    ],
    192000: [{
            "desc": "24-bit/96kHz FLAC",
            "bit_depth": 24,
            "quality_setting": 96000,
            "suffix": "24-96",
            "format": "flac"
        },
        BASE_FLAC_CONFIGS["48"]
    ]
}


@contextlib.contextmanager
def directory_context(directory):
    orig_path = directory
    sanitized = sanitize_filename(directory.name)
    renamed = sanitized != directory.name

    if renamed: directory = directory.rename(directory.parent / sanitized)

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
        logging.error(f"Command:\n{' '.join(cmd)}")
        logging.error(stderr)
        raise subprocess.CalledProcessError(result.returncode, cmd, output=stdout, stderr=stderr)

    return stdout, stderr


def get_metadata(file):
    out = run_command([
        "ffprobe", "-select_streams", "a:0",
        "-show_entries", "stream=bits_per_raw_sample,sample_rate",
        "-of", "default=nw=1", str(file)
    ])[0]

    extract = lambda key: int(m.group(1)) if (m := re.search(f"{key}=(\\d+)", out)) else None
    
    return extract("bits_per_raw_sample"), extract("sample_rate")


def convert(file, tier):
    if tier["format"] == "flac":
        temp_a = file.with_name("a.flac")
        temp_b = file.with_name("b.flac")

        try:
            file.rename(temp_a)
            cmd = ["sox", "-S", str(temp_a), "-R", "-G", "-b", str(tier["bit_depth"]),
                   str(temp_b), "rate", "-v", "-L", str(tier["quality_setting"])]

            if tier["bit_depth"] == 16:
                cmd.append("dither")

            run_command(cmd)
            temp_a.unlink()
            temp_b.rename(file)

        finally:
            if temp_a.exists():
                temp_a.rename(file)
            if temp_b.exists():
                temp_b.unlink()

    elif tier["format"] == "mp3":
        out_file = file.with_suffix(".mp3")
        
        run_command([
            "ffmpeg", "-nostats", "-i", str(file), "-codec:a", "libmp3lame",
            "-b:a", tier["quality_setting"], str(out_file)
            ])
        
        file.unlink()


def process_tier(src, tier):
    dest = src.parent / f"{src.name} [{tier['suffix']}]"
    logging.info(f"Converting {src.name} to {tier['desc']}.")

    exclusions = ["*.log", "*.m3u", "*.cue", "*.md5"]

    rc = subprocess.run(
        ["robocopy", str(src), str(dest), "/S", "/XF", *exclusions],
        stdout=subprocess.DEVNULL
    ).returncode

    if rc >= 8:
        raise RuntimeError(f"Robocopy failed with code {rc}")

    flac_files = list(dest.rglob("*.flac"))

    for f in tqdm(flac_files, desc=f"Converting {src.name} to {tier['desc']}"):
        convert(f, tier)

    print("\nConversion successful.\n------------------")


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

    if bd not in {16, 24} or sr not in TIER_CONFIG:
        logging.warning(f"Unsupported: {bd}-bit/{sr}Hz")
        return
    
    if fmt == "mp3":
        tiers = [MP3_TIER]
    elif fmt == "flac":
        tiers = [t for t in TIER_CONFIG[sr]]
    elif fmt == "all":
        tiers = TIER_CONFIG[sr] + [MP3_TIER]
    else:
        logging.warning(f"Unsupported format: {fmt}")
        return

    for tier in tiers:
        process_tier(src, tier)


def process_sacd_directory(src, fmt="all"):
    iso_files = list(src.rglob("*.iso"))
    
    output_dirs = [] 

    for disc_number, iso in enumerate(iso_files, 1):
        dirs = convert_iso_to_dff(iso, src)
        output_dirs.extend((folder, disc_number) for folder in dirs)

    logging.info("All ISOs converted to DFF.")
    print("----------------------")
    
    parent_folders = set(folder.parent for folder, _ in output_dirs)
    
    for folder, disc_number in output_dirs:
        dff_files   = list(folder.rglob("*.dff"))
        dff_folders = sorted({d.parent for d in dff_files})

        for dff_folder in dff_folders:
            dff_directory_conversion(dff_folder, disc_number)

    print("All DFFs converted to FLAC.\n----------------------")
        
    for parent_folder in parent_folders: 
        process_flac_directory(parent_folder, fmt)


def convert_iso_to_dff(iso_path, base_dir):
    probe_result = run_command(["sacd_extract", "-P", "-i", str(iso_path)], cwd=str(base_dir))[0]

    channel_configs = [
        ("Speaker config: (Multichannel|5|6)", "Multichannel", ["-m", "-p", "-c"]),
        ("Speaker config: (Stereo|2)", "Stereo", ["-2", "-p", "-c"])
    ]

    out_dirs = []
    iso_files = sorted(base_dir.rglob("*.iso"))
    disc_number = iso_files.index(iso_path) + 1

    for pattern, suffix, cmd in channel_configs:
        if re.search(pattern, probe_result):
            logging.info(f"Creating {suffix} DFFs: {iso_path.name}")

            channel_dir = base_dir.parent / f"{base_dir.name} [{suffix}]"
            channel_dir.mkdir(exist_ok=True, parents=True)

            before_dirs = set(d for d in channel_dir.iterdir() if d.is_dir())

            output_disc_dir = channel_dir / f"Disc {disc_number:02d}"
            
            if output_disc_dir.exists():
                logging.info(f"{output_disc_dir.name} already exists, skipping extraction.")
                out_dirs.append(output_disc_dir)
                continue

            run_command(["sacd_extract", *cmd, "-i", str(iso_path)], cwd=str(channel_dir))

            after_dirs = set(d for d in channel_dir.iterdir() if d.is_dir())
            new_dir = next(iter(after_dirs - before_dirs))

            disc_dir = new_dir.rename(output_disc_dir)

            out_dirs.append(disc_dir)
            
    return out_dirs


def dff_directory_conversion(dff_dir, disc_number):
    dff_dir = dff_dir.rename(dff_dir.parent / f"Disc {disc_number}")

    print()
    logging.info(f"Converting DFF to FLAC: {dff_dir.name}")

    dff_files = list(dff_dir.rglob("*.dff"))
    dr = calculate_gain(dff_files)

    for dff in tqdm(dff_files, desc=f"Converting DFFs in {dff_dir.name}"):
        convert_dff_to_flac(dff, dr)

    return dff_dir


def calculate_gain(dff_files, target_headroom_db=-1.0):
    peaks = []

    for dff in dff_files:
        out = run_command(["ffmpeg", "-nostats", "-i", str(dff), "-af", "volumedetect", "-f", "null", "-"])[1]

        m = re.search(r"max_volume: (-\d+\.?\d*) dB", out)
        
        if m:
            peaks.append(float(m.group(1)))
    
    if not peaks:
        raise RuntimeError("Could not detect any peak levels")
    
    peak = max(peaks)
    
    return target_headroom_db - peak 


def convert_dff_to_flac(dff, gain_db):
    temp_pcm = dff.with_suffix(".pcm.flac")
    final = dff.with_suffix(".flac")

    try:
        run_command([
            "ffmpeg", "-y", "-nostats", "-i", str(dff),
            "-sample_fmt", "s32",
            "-ar", "88200",
            "-af", f"volume={gain_db}dB",
            str(temp_pcm)
        ])

        run_command([
            "sox", "-b", "24", 
            "-S", str(temp_pcm), str(final),
            "trim", "0.0065", "reverse",
            "silence", "1", "0", "0%",
            "trim", "0.0065", "reverse",
            "pad", "0.0065", "0.2"
        ])

    finally:
        if temp_pcm.exists():
            temp_pcm.unlink()
        if dff.exists():
            dff.unlink()


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