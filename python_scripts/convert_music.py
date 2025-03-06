import argparse
import contextlib
import logging
import re
import subprocess
import sys
import chardet
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
    "suffix": " (MP3)",
    "format": "mp3",
}

TIER_CONFIG = {
    44100: [{"desc": "16-bit/44.1kHz FLAC", "bit_depth": 16, "quality_setting": 44100, "suffix": " [16-44.1]",
             "format": "flac"}],
    48000: [{"desc": "16-bit/48kHz FLAC", "bit_depth": 16, "quality_setting": 48000, "suffix": " [16-48]",
             "format": "flac"}],
    88200: [{"desc": "16-bit/44.1kHz FLAC", "bit_depth": 16, "quality_setting": 44100, "suffix": " [16-44.1]",
             "format": "flac"}],
    96000: [{"desc": "16-bit/48kHz FLAC", "bit_depth": 16, "quality_setting": 48000, "suffix": " [16-48]",
             "format": "flac"}],
    176400: [
        {"desc": "24-bit/88.2kHz FLAC", "bit_depth": 24, "quality_setting": 88200, "suffix": " [24-88.2]",
         "format": "flac"},
        {"desc": "16-bit/44.1kHz FLAC", "bit_depth": 16, "quality_setting": 44100, "suffix": " [16-44.1]",
         "format": "flac"},
    ],
    192000: [
        {"desc": "24-bit/96kHz FLAC", "bit_depth": 24, "quality_setting": 96000, "suffix": " [24-96)",
         "format": "flac"},
        {"desc": "16-bit/48kHz FLAC", "bit_depth": 16, "quality_setting": 48000, "suffix": " [16-48]",
         "format": "flac"},
    ],
}


@contextlib.contextmanager
def directory_context(directory):
    rename_map = {
        p: p.with_name(s) for p in directory.rglob("*")
        if p.is_file() and (s := sanitize_filename(p.name)) != p.name
           and not p.with_name(s).exists()
    }

    try:
        yield directory

    finally:
        for orig, new in rename_map.items():
            if new.exists() and not orig.exists():
                new.rename(orig)


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
        logging.error({stderr})
        raise subprocess.CalledProcessError(result.returncode, cmd, output=stdout, stderr=stderr)

    return stdout, stderr


def get_metadata(file):
    out = run_command(["sox", "--i", str(file)])[0]
    extract = lambda key: int(re.search(f"{key}.*?(\d+)", out).group(1))
    return extract("Precision"), extract("Sample Rate")


def convert_flac(file, tier):
    tmp = file.with_name(f"temp_{file.name}")
    cmd = ["sox", "-S", str(file), "-R", "-G", "-b", str(tier["bit_depth"]),
           str(tmp), "rate", "-v", "-L", str(tier["quality_setting"])]

    if tier["bit_depth"] == 16:
        cmd.append("dither")

    run_command(cmd)
    file.unlink()
    tmp.rename(file)


def convert_to_mp3(file, tier):
    out_file = file.with_suffix(".mp3")

    run_command(["ffmpeg", "-nostats", "-i", str(file), "-codec:a", "libmp3lame",
                 "-b:a", tier["quality_setting"], str(out_file)])
    file.unlink()


def process_tier(src, tier):
    dest = src.parent / f"{src.name}{tier['suffix']}"
    logging.info(f"Converting {src.name} to {tier['desc']}")

    exclusions = ["*.log", "*.m3u", "*.cue", "*.md5"]
    rc = subprocess.run(
        ["robocopy", str(src), str(dest), "/S", "/XF", *exclusions],
        stdout=subprocess.DEVNULL
    ).returncode

    if rc >= 8:
        raise RuntimeError(f"Robocopy failed with code {rc}")

    flac_files = list(dest.rglob("*.flac"))

    for idx, f in enumerate(flac_files, start=1):
        if tier["format"] == "flac":
            convert_flac(f, tier)
        else:
            convert_to_mp3(f, tier)

        end_char = "\n" if idx == len(flac_files) else ""
        print(f"\rProcessed {idx}/{len(flac_files)} files", flush=True, end=end_char)


def process_flac_directory(src, fmt="all"):
    logging.info(f"Processing FLAC directory: {src.stem}")

    flac_files = list(src.rglob("*.flac"))

    if not flac_files:
        logging.warning("No FLAC files found")
        return

    bit_depths = []
    sample_rates = []

    for f in flac_files:
        prec, rate = get_metadata(f)
        bit_depths.append(prec)
        sample_rates.append(rate)

    bd, sr = min(bit_depths), min(sample_rates)

    logging.info(f"Detected: {bd}-bit/{sr}Hz")

    if bd not in {16, 24} or sr not in TIER_CONFIG:
        logging.warning(f"Unsupported: {bd}-bit/{sr}Hz")
        return

    if fmt == "mp3":
        tiers = [MP3_TIER]
    elif fmt == "flac":
        tiers = TIER_CONFIG[sr]
    else:
        tiers = TIER_CONFIG[sr] + [MP3_TIER]

    for tier in tiers:
        process_tier(src, tier)


def process_sacd_directory(src, fmt="all"):
    iso_files = list(src.rglob("*.iso"))

    output_dirs = []

    for iso in iso_files:
        logging.info(f"Processing: {iso.name}")
        output_dirs = convert_iso_to_dff(iso, src)

    for folder in output_dirs:
        dff_files = folder.rglob("*.dff")
        dff_folders = sorted(set(d.parent for d in dff_files))

        for dff_folder in dff_folders:
            dff_directory_conversion(dff_folder)

        process_flac_directory(folder, fmt)


def convert_iso_to_dff(iso_path, base_dir):
    probe_result = run_command(
        ["sacd_extract", "-P", "-i", str(iso_path)], cwd=str(base_dir)
    )[0]

    channel_configs = [
        ("Speaker config: (Multichannel|5|6)", "Multichannel", ["-m", "-p", "-c"]),
        ("Speaker config: (Stereo|2)", "Stereo", ["-2", "-p", "-c"])
    ]

    out_dirs = []

    for pattern, suffix, cmd in channel_configs:
        if re.search(pattern, probe_result):
            out_dir = base_dir.parent / f"{base_dir.name} [{suffix}] [24-88.2]"
            out_dir.mkdir(exist_ok=True, parents=True)

            logging.info(f"{iso_path.name} contains {suffix} sound")
            run_command(["sacd_extract", *cmd, "-i", str(iso_path)], cwd=str(out_dir))

            out_dirs.append(out_dir)

    return out_dirs


def dff_directory_conversion(dff_dir):
    logging.info(f"Processing DFF directory: {dff_dir.stem}")
    dff_files = list(dff_dir.rglob("*.dff"))
    dr = calculate_dynamic_range(dff_files)

    for i, dff in enumerate(dff_files, 1):
        process_dff(dff, dr)
        end_char = "\n" if i == len(dff_files) else ""
        print(f"\r{i}/{len(dff_files)} DFF files converted to FLAC", flush=True, end=end_char)

    return dff_dir


def calculate_dynamic_range(dff_files):
    dr_values = []

    for dff in dff_files:
        result = run_command(
            ["ffmpeg", "-nostats", "-i", str(dff), "-af", "volumedetect", "-f",
             "null", "-"])[1]
        match = re.search(r"max_volume: (-\d+\.?\d*) dB", result)

        if match:
            dr_values.append(float(match.group(1)))

    db = max(dr_values, default=0.0) - 0.5
    logging.info(f"Dynamic range: {db:.2f} dB")

    return db


def process_dff(dff, dr):
    orig = dff
    dff = dff.rename(dff.with_name(dff.stem[:20] + dff.suffix))
    flac = dff.with_suffix(".flac")

    run_command(["ffmpeg", "-nostats", "-i", str(dff), "-c:a", "flac", "-sample_fmt", "s32",
                "-ar", "88200", "-af", f"volume={dr}", str(flac)])
    temp = flac.with_name("temp_" + flac.name)

    run_command(["sox", str(flac), str(temp), "trim", "0.0065", "reverse", "silence",
                "1", "0", "0%", "trim", "0.0065", "reverse", "pad", "0.0065", "0.2"])

    flac.unlink()

    temp.rename(orig.with_suffix(".flac"))
    dff.unlink()

    return temp

def main():
    parser = argparse.ArgumentParser(description="Audio processing tool")

    parser.add_argument("cmd", choices=["extract_sacd", "convert"], help="Command to execute (extract_sacd or convert)")

    parser.add_argument("-f", "--format", choices=["flac", "mp3", "all"], default="all", help="Output format (default: all)")
    parser.add_argument("directory", type=Path, help="Directory to process")

    args = parser.parse_args()
    directory = Path(args.directory.resolve())

    if not directory.exists():
        logging.error(f"Directory not found: {args.directory}")
        sys.exit(1)

    with directory_context(directory):
        try:
            if args.cmd == "extract_sacd":
                process_sacd_directory(directory, args.format)
            elif args.cmd == "convert":
                process_flac_directory(directory, args.format)
        except Exception as e:
            logging.error({e})
            sys.exit(1)

    logging.info("Processing completed")


if __name__ == "__main__":
    main()