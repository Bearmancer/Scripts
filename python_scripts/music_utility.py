import subprocess
import sys
import unidecode
import argparse
import logging
from pathlib import Path
from pathvalidate import sanitize_filename

logging.basicConfig(
    level=logging.INFO,
    format='%(levelname)s - %(message)s',
    handlers=[logging.StreamHandler(sys.stdout)]
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

def get_conversion_tiers(original_sample_rate: int):
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
    
    logging.debug("Available FLAC tiers:")
    for tier in flac_tiers:
        logging.debug(f"  - {tier['description']}")
    
    return flac_tiers + [MP3_TIER]

def get_flac_metadata(flac_file: Path):
    result = subprocess.run(
        ['sox', '--i', str(flac_file)],
        capture_output=True,
        text=True
    )
    output = result.stdout

    def parse_value(keyword):
        for line in output.splitlines():
            if keyword in line:
                return line.split(':', 1)[-1].strip()
        raise ValueError(f"'{keyword}' not found in sox output")

    precision = parse_value('Precision')
    sample_rate = parse_value('Sample Rate')
    return int(precision.split('-')[0]), int(sample_rate.split()[0])

def process_directory(src_dir: Path):
    logging.info(f"\nProcessing directory: {src_dir.name}")
    flac_files = list(src_dir.rglob('*.flac'))
    
    if not flac_files:
        logging.warning("No FLAC files found. Exiting.")
        return

    formats = [get_flac_metadata(f) for f in flac_files]
    
    all_bit_depths = set(bd for bd, sr in formats)
    all_sample_rates = set(sr for bd, sr in formats)
    
    common_bd = min(all_bit_depths)
    common_sr = min(all_sample_rates)

    if len(all_bit_depths) > 1 or len(all_sample_rates) > 1:
        logging.warning(f"Mixed formats detected - Bit depths: {all_bit_depths}, Sample rates: {all_sample_rates}")
        logging.info(f"Using lowest common denominator: {common_bd}-bit/{common_sr}Hz")
    else:
        logging.info(f"Uniform format detected: {common_bd}-bit/{common_sr}Hz")

    if common_bd == 16 and common_sr in (44100, 48000):
        logging.info("Direct MP3 conversion selected (16-bit CD quality source)")
        process_single_tier(src_dir, MP3_TIER)
        return

    try:
        tiers = get_conversion_tiers(common_sr)
    except ValueError as e:
        logging.error(str(e))
        sys.exit(1)

    flac_tiers = [t for t in tiers if t['format'] == 'flac']
    selected_tier = max(
        flac_tiers,
        key=lambda t: (-t['bit_depth'], -t['target_sr']),
        default=MP3_TIER
    )
    
    logging.info(f"Selected conversion tier: {selected_tier['description']}")
    process_single_tier(src_dir, selected_tier)

def process_single_tier(src_dir: Path, tier: dict):
    dest_dir = src_dir.parent / f"{src_dir.name}{tier['suffix']}"
    logging.info(f"Processing tier: {tier['description']}")
    logging.info(f"Creating destination directory: {dest_dir.name}")

    robocopy_directory(src_dir, dest_dir)
    stem_mapping = sanitize_and_rename_files(dest_dir)
    convert_files(dest_dir, tier)
    restore_filenames(dest_dir, stem_mapping)

def robocopy_directory(src: Path, dest: Path):
    logging.info(f"Copying directory structure with robocopy...")
    result = subprocess.run(
        ['robocopy', str(src), str(dest), '/S', '/XF', '*.log', '*.m3u', '*.cue', '*.md5'],
        stdout=subprocess.DEVNULL
    )
    if result.returncode not in [0, 1]:
        raise RuntimeError(f"Robocopy failed with code {result.returncode}")

def sanitize_and_rename_files(directory: Path) -> dict:
    stem_mapping = {}
    for file in directory.rglob('*'):
        if file.is_file():
            original_stem = file.stem
            original_suffix = file.suffix
            sanitized_name = sanitize_filename(unidecode.unidecode(file.name))
            sanitized_path = file.parent / sanitized_name

            if sanitized_path != file:
                try:
                    file.rename(sanitized_path)
                    new_stem = sanitized_path.stem
                    if new_stem != original_stem:
                        stem_mapping[new_stem] = original_stem
                    logging.debug(f"Sanitized: {file.name} -> {sanitized_path.name}")
                except Exception as e:
                    logging.error(f"Error renaming {file.name}: {e}")
    
    return stem_mapping

def convert_files(dest_dir: Path, tier: dict):
    flac_files = list(dest_dir.rglob('*.flac'))
    logging.info(f"Found {len(flac_files)} FLAC files for conversion")
    
    for flac in flac_files:
        try:
            if tier['format'] == 'flac':
                convert_flac(flac, tier)
            elif tier['format'] == 'mp3':
                convert_to_mp3(flac, tier)
        except Exception as e:
            logging.error(f"Failed to process {flac.name}: {e}")

def convert_flac(flac_file: Path, tier: dict):
    logging.info(f"Converting FLAC: {flac_file.name}")
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
    logging.info(f"Converting to MP3: {flac_file.name}")
    mp3_file = flac_file.with_suffix('.mp3')
    
    cmd = [
        'ffmpeg', '-hide_banner', '-loglevel', 'error',
        '-i', str(flac_file),
        '-codec:a', 'libmp3lame', '-b:a', tier['bitrate'], str(mp3_file)
    ]
    subprocess.run(cmd, check=True)
    flac_file.unlink()

def restore_filenames(directory: Path, stem_mapping: dict):
    logging.info("Restoring original filenames...")
    
    for file in directory.rglob('*'):
        if file.is_file():
            current_stem = file.stem
            if current_stem in stem_mapping:
                original_name = f"{stem_mapping[current_stem]}{file.suffix}"
                original_path = file.with_name(original_name)
                
                if original_path.exists():
                    logging.warning(f"Skipping restore - exists: {original_name}")
                    continue
                
                try:
                    file.rename(original_path)
                    logging.debug(f"Restored: {file.name} -> {original_name}")
                except Exception as e:
                    logging.error(f"Error restoring {file.name}: {e}")

def main():
    parser = argparse.ArgumentParser(
        description='Audio conversion pipeline with tiered quality output'
    )
    parser.add_argument('root_dir', type=Path, help='Root directory containing FLAC files')
    args = parser.parse_args()
    
    if not args.root_dir.exists():
        logging.error(f"Directory not found: {args.root_dir}")
        sys.exit(1)
    
    try:
        process_directory(args.root_dir)
        logging.info("\nProcessing completed successfully")
    except Exception as e:
        logging.error(f"\nCritical error: {e}")
        sys.exit(1)

if __name__ == "__main__":
    main()