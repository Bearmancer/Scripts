import argparse
import contextlib
import logging
import re
import subprocess
import sys
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
    44100: [{"desc": "16-bit/44.1kHz FLAC", "bit_depth": 16, "quality_setting": 44100, "suffix": " (16-44.1)",
             "format": "flac"}],
    48000: [{"desc": "16-bit/48kHz FLAC", "bit_depth": 16, "quality_setting": 48000, "suffix": " (16-48)",
             "format": "flac"}],
    88200: [{"desc": "16-bit/44.1kHz FLAC", "bit_depth": 16, "quality_setting": 44100, "suffix": " (16-44.1)",
             "format": "flac"}],
    96000: [{"desc": "16-bit/48kHz FLAC", "bit_depth": 16, "quality_setting": 48000, "suffix": " (16-48)",
             "format": "flac"}],
    176400: [
        {"desc": "24-bit/88.2kHz FLAC", "bit_depth": 24, "quality_setting": 88200, "suffix": " (24-88.2)",
         "format": "flac"},
        {"desc": "16-bit/44.1kHz FLAC", "bit_depth": 16, "quality_setting": 44100, "suffix": " (16-44.1)",
         "format": "flac"},
    ],
    192000: [
        {"desc": "24-bit/96kHz FLAC", "bit_depth": 24, "quality_setting": 96000, "suffix": " (24-96)",
         "format": "flac"},
        {"desc": "16-bit/48kHz FLAC", "bit_depth": 16, "quality_setting": 48000, "suffix": " (16-48)",
         "format": "flac"},
    ],
}


@contextlib.contextmanager
def directory_context(directory):
    rename_map = {
        p: p.with_name(sanitize_filename(p.name))
        for p in directory.rglob("*")
        if p.is_file() and ((s := sanitize_filename(p.name)) != p.name) and not p.with_name(s).exists()
    }
    for orig, new in rename_map.items():
        logging.info(f"Renaming: {orig} -> {new}")
        orig.rename(new)
    try:
        yield directory
    finally:
        for orig, new in rename_map.items():
            if new.exists() and not orig.exists():
                logging.info(f"Reverting: {new} -> {orig}")
                new.rename(orig)


def run_command(cmd, cwd=None):
    result = subprocess.run(
        cmd,
        cwd=str(cwd) if cwd else None,
        capture_output=True,
        text=True,
    )

    if result.returncode != 0:
        logging.error(f"\nError: Command {' '.join(cmd)} failed with return code {result.returncode}")
        logging.error("Standard Error:")
        logging.error(result.stderr)
        raise subprocess.CalledProcessError(result.returncode, cmd, output=result.stdout, stderr=result.stderr)

    return result


def get_metadata(file):
    out = run_command(["sox", "--i", str(file)]).stdout
    extract = lambda key: int(next(
        line.split(":")[-1].strip().split()[0].split("-")[0]
        for line in out.splitlines() if key in line
    ))
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
    logging.info(f"Converting {dest} to {tier['desc']}")
    rc = subprocess.run(["robocopy", str(src), str(dest), "/S", "/XF", "*.log", "*.m3u", "*.cue", "*.md5"],
                        stdout=subprocess.DEVNULL).returncode
    if rc >= 8:
        raise RuntimeError(f"Robocopy failed with code {rc}")

    with directory_context(dest):
        flac_files = list(dest.rglob("*.flac"))
        for f in flac_files:
            try:
                converter = convert_flac if tier["format"] == "flac" else convert_to_mp3
                converter(f, tier)

            except Exception as e:
                logging.error(f"Failed {f.name}: {e}")


def process_flac_directory(src, fmt="all"):
    logging.info(f"Processing FLAC directory: {src}")

    with directory_context(src):
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

        tiers = TIER_CONFIG[sr] + ([MP3_TIER] if fmt in ["mp3", "all"] else [])

        if fmt == "mp3":
            tiers = [MP3_TIER]

        for tier in tiers:
            process_tier(src, tier)


def process_sacd_directory(src, fmt="all"):
    with directory_context(src):
        iso_files = list(src.rglob("*.iso"))

        for iso in iso_files:
            logging.info(f"Processing: {iso.name}")
            dff_dirs = convert_iso_to_dff(iso, src)
            for dff_dir in dff_dirs:
                flac_dir = dff_directory_conversion(dff_dir)
                process_flac_directory(flac_dir, fmt)


def convert_iso_to_dff(iso_path, base_dir):
    with directory_context(base_dir):
        probe_result = run_command(
            ["sacd_extract", "-P", "-i", str(iso_path)], cwd=str(base_dir)
        )

        output_dirs = []
        channel_configs = [
            ("Speaker config: (Multichannel|5|6)", "Multichannel", ["-m", "-p", "-c"]),
            ("Speaker config: (Stereo|2)", "Stereo", ["-2", "-p", "-c"])
        ]

        for pattern, suffix, cmd in channel_configs:
            if re.search(pattern, probe_result.stdout):
                out_dir = base_dir.parent / f"{base_dir.name} ({suffix})"
                out_dir.mkdir(exist_ok=True, parents=True)

                existing_dirs = set(d for d in out_dir.iterdir() if d.is_dir())

                logging.info(f"Found {suffix} channels on {iso_path.name}")
                run_command(["sacd_extract", *cmd, "-i", str(iso_path)], cwd=str(out_dir))

                new_dirs_with_dffs = [d for d in out_dir.iterdir()
                                      if d.is_dir() and d not in existing_dirs]

                for d in new_dirs_with_dffs:
                    output_dirs.append(d)

        if not output_dirs:
            logging.error(f"No new folder was created through {iso_path.name}")

        return output_dirs


def dff_directory_conversion(dff_dir):
    dff_files = list(dff_dir.rglob("*.dff"))
    total_files = len(dff_files)
    dr = calculate_dynamic_range(dff_files)

    for i, dff in enumerate(dff_files, 1):
        flac_path = dff_to_flac(dff, dr)
        trim_flac(flac_path)

        progress = f"\r{i}/{total_files} DFF files converted to FLAC"
        print(progress, flush=True, end="\n")

    for dff in dff_files:
        dff.unlink()

    return dff_dir


def calculate_dynamic_range(dff_files):
    dr_values = []

    for dff in dff_files:
        result = run_command(
            ["ffmpeg", "-nostats", "-i", str(dff), "-af", "volumedetect", "-f",
             "null", "-"])
        match = re.search(r"max_volume: (-\d+\.?\d*) dB", result.stderr)

        if match:
            dr_values.append(float(match.group(1)))

    db = max(dr_values, default=0.0) - 0.5 if dr_values else 0.0
    logging.info(f"Dynamic range: {db:.2f} dB")

    return db


def dff_to_flac(dff, dr):
    flac_path = dff.with_suffix(".flac")

    try:
        run_command([
            "ffmpeg", "-nostats", "-i", str(dff),
            "-c:a", "flac", "-sample_fmt", "s32",
            "-ar", "88200", "-af", f"volume={dr}",
            str(flac_path)
        ])
    except Exception as e:
        raise Exception(f"Conversion of {dff.stem} failed.\nError: {e}.")

    return flac_path


def trim_flac(flac_path):
    temp_path = flac_path.with_name(f"temp_{flac_path.name}")

    try:
        run_command([
            "sox", str(flac_path), str(temp_path),
            "trim", "0.0065", "reverse",
            "silence", "1", "0", "0%",
            "trim", "0.0065", "reverse", "pad", "0.0065", "0.2"
        ])
    except Exception as e:
        raise Exception(f"Trimming {flac_path.stem} failed.\nError: {e}.")

    flac_path.unlink()
    temp_path.rename(flac_path)


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

    if args.cmd == "extract_sacd":
        process_sacd_directory(directory, args.format)
    elif args.cmd == "convert":
        process_flac_directory(directory, args.format)

    logging.info("Processing completed")


if __name__ == "__main__":
    main()