import subprocess
import sys
import unidecode
import chardet
import argparse
import logging
from pathlib import Path
from pathvalidate import sanitize_filename

logging.basicConfig(
    level=logging.DEBUG,
    format='[%(levelname)s] %(message)s',
    datefmt='%Y-%m-%d %H:%M:%S'
)

MP3_TIER = {
    'suffix': ' (MP3)',
    'bitrate': '320k',
    'format': 'mp3',
    'description': '320kbps MP3'
}

TIER_CONFIG = {
    44100: [('16-bit/44.1kHz FLAC', 16, 44100, ' (16-44.1)')],
    48000: [('16-bit/48kHz FLAC', 16, 48000, ' (16-48)')],
    88200: [('16-bit/44.1kHz FLAC', 16, 44100, ' (16-44.1)')],
    96000: [('16-bit/48kHz FLAC', 16, 48000, ' (16-48)')],
    176400: [
        ('24-bit/88.2kHz FLAC', 24, 88200, ' (24-88.2)'),
        ('16-bit/44.1kHz FLAC', 16, 44100, ' (16-44.1)')
    ],
    192000: [
        ('24-bit/96kHz FLAC', 24, 96000, ' (24-96)'),
        ('16-bit/48kHz FLAC', 16, 48000, ' (16-48)')
    ]
}


def get_conversion_tiers(original_bit_depth: int, original_sample_rate: int):
    if original_bit_depth == 16:
        logging.info("16-bit source: Direct MP3 conversion")
        return [MP3_TIER]

    try:
        config = TIER_CONFIG[original_sample_rate]
    except KeyError:
        raise ValueError(f"Unsupported sample rate: {original_sample_rate}")

    flac_tiers = [{
        'description': desc,
        'bit_depth': bd,
        'target_sr': sr,
        'suffix': suffix,
        'format': 'flac'
    } for desc, bd, sr, suffix in config]

    tiers = flac_tiers + [MP3_TIER]

    sample_rate_khz = original_sample_rate // 1000

    if original_sample_rate > 48000:
        tier_list = ", ".join([t['description'] for t in tiers])
        logging.info(f"Creating conversion tiers: {tier_list}")
    else:
        logging.debug(f"Converting {sample_rate_khz}kHz source to 16-bit")

    return tiers


def get_flac_metadata(flac_file: Path):
    flac_info = subprocess.run(['sox', '--i', str(flac_file)], capture_output=True)
    detected_encoding = chardet.detect(flac_info.stdout)['encoding']
    flac_info_output = flac_info.stdout.decode(detected_encoding)

    precision_str = next(
        line.split(":")[-1].strip() 
        for line in flac_info_output.splitlines() if "Precision" in line
    )
    sample_rate_str = next(
        line.split(":")[-1].strip() 
        for line in flac_info_output.splitlines() if "Sample Rate" in line
    )

    bit_depth = int(precision_str.split('-')[0])
    sample_rate = int(sample_rate_str)

    logging.debug(f"Detected metadata of: {flac_file.stem}: {bit_depth}-bit/{sample_rate}Hz")
    return bit_depth, sample_rate


def process_directory(src_dir: Path):
    logging.info(f"Processing directory: {src_dir}")

    all_dirs = {flac.parent for flac in src_dir.rglob('*.flac')}

    if not all_dirs:
        process_single_album(src_dir)
        return

    metadata = []

    for d in sorted(all_dirs):
        flacs = list(d.glob('*.flac'))

        if not flacs:
            continue

        try:
            metadata.append(get_flac_metadata(flacs[0]))
        except Exception as e:
            logging.error(f"Failed to process {flacs[0]}: {str(e)}")
            sys.exit(1)

    if not metadata:
        logging.error("No valid FLAC files found in any directories")
        return

    bit_depths = [bd for bd, sr in metadata]
    sample_rates = [sr for bd, sr in metadata]

    if len(set(bit_depths)) > 1 or len(set(sample_rates)) > 1:
        logging.warning("Mixed formats detected, using lowest common denominator")
        target_bd = min(bit_depths)
        target_sr = min(sample_rates)
        if target_sr in [44100, 48000]:
            target_bd = 16
    else:
        target_bd = bit_depths[0]
        target_sr = sample_rates[0]

    logging.info(f"Using conversion baseline: {target_bd}-bit/{target_sr}Hz")

    try:
        tiers = get_conversion_tiers(target_bd, target_sr)
    except ValueError as e:
        logging.error(str(e))
        sys.exit(1)

    process_tiers(src_dir, tiers)


def process_single_album(src_dir: Path):
    flacs = list(src_dir.rglob('*.flac'))

    if not flacs:
        logging.warning("No FLAC files found")
        return

    base_bd, base_sr = get_flac_metadata(flacs[0])
    
    for flac in flacs[1:]:
        current_bd, current_sr = get_flac_metadata(flac)
        if current_bd != base_bd or current_sr != base_sr:
            logging.error(f"Format mismatch in {flac}")
            logging.error(f"Expected: {base_bd}-bit/{base_sr}Hz, Found: {current_bd}-bit/{current_sr}Hz")
            sys.exit(1)

    try:
        tiers = get_conversion_tiers(base_bd, base_sr)
    except ValueError as e:
        logging.error(str(e))
        sys.exit(1)

    process_tiers(src_dir, tiers)


def process_tiers(src_dir: Path, tiers: list):
    for tier in tiers:
        logging.info(f"Processing: {tier['description']} tier")
        dest_dir = src_dir.parent / f"{src_dir.name}{tier['suffix']}"

        robocopy_directory(src_dir, dest_dir)
        sanitize_and_rename_files(dest_dir)

        if tier['format'] == 'flac':
            logging.info("Downsampling FLAC files")
            for flac in dest_dir.rglob('*.flac'):
                convert_flac(flac, tier)
        elif tier['format'] == 'mp3':
            logging.info("Converting to MP3")
            for flac in dest_dir.rglob('*.flac'):
                convert_to_mp3(flac, tier)


def robocopy_directory(src: Path, dest: Path):
    result = subprocess.run(['robocopy', str(src), str(dest), '/S', '/XF', '*.log', '*.m3u', '*.cue', '*.md5'])
    if result.returncode not in [0, 1]:
        raise Exception(f"Robocopy failed with code {result.returncode}")


def sanitize_and_rename_files(directory: Path):
    for file in directory.rglob('*'):
        if file.is_file():
            sanitized = sanitize_filename(unidecode.unidecode(file.name))
            if sanitized != file.name:
                file.rename(file.parent / sanitized)


def convert_flac(flac_file: Path, tier: dict):
    logging.info(f"Converting {flac_file} to {tier['description']}")
    temp_file = flac_file.with_name(f'temp_{flac_file.name}')

    cmd = [
        'sox', '-S', str(flac_file),
        '-R', '-G', '-b', str(tier['bit_depth']),
        str(temp_file),
        'rate', '-v', '-L', str(tier['target_sr'])
    ]
    if tier['bit_depth'] == 16:
        cmd.append('dither')

    subprocess.run(cmd, check=True)
    flac_file.unlink()
    temp_file.rename(flac_file)


def convert_to_mp3(flac_file: Path, tier: dict):
    logging.info(f"Converting {flac_file.stem} to MP3")
    mp3_file = flac_file.with_suffix('.mp3')
    subprocess.run([
        'ffmpeg', '-i', str(flac_file),
        '-codec:a', 'libmp3lame', '-b:a', tier['bitrate'], str(mp3_file)
    ], check=True)
    flac_file.unlink()


def main():
    parser = argparse.ArgumentParser(description='Audio conversion pipeline')
    parser.add_argument('root_dir', type=Path, help='Root directory to process')
    parser.add_argument('--recursive', action='store_true', help='Process subdirectories recursively')
    args = parser.parse_args()

    process_directory(args.root_dir)


if __name__ == "__main__":
    main()