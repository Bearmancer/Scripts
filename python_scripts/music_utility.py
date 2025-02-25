import subprocess
import sys
import unidecode
import argparse
import logging
from pathlib import Path
from contextlib import contextmanager
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

def robocopy_directory(src: Path, dest: Path):
    logging.info(f"Copying structure to {dest.name}")
    result = subprocess.run(
        ['robocopy', str(src), str(dest), '/S', '/XF', '*.log', '*.m3u', '*.cue', '*.md5'],
        stdout=subprocess.DEVNULL
    )
    if result.returncode not in [0, 1]:
        raise RuntimeError(f"Robocopy failed: {result.returncode}")


@contextmanager
def temporary_sanitized_names(directory: Path):
    name_map = {}
    try:
        for path in directory.rglob('*'):
            if path.is_file() and path.exists():
                original_name = path.name
                sanitized = sanitize_filename(unidecode.unidecode(original_name))

                if sanitized != original_name:
                    new_path = path.with_name(sanitized)
                    if new_path.exists():
                        logging.warning(f"Skipping existing sanitized name: {new_path}")
                        continue

                    path.rename(new_path)
                    name_map[new_path] = path.name
                    logging.debug(f"Sanitized: {original_name} → {sanitized}")

        yield

    finally:
        for new_path, original_name in name_map.items():
            target_path = new_path.with_name(original_name)
            try:
                if new_path.exists():
                    if target_path.exists():
                        logging.warning(f"Can't restore - target exists: {target_path}")
                    else:
                        new_path.rename(target_path)
                        logging.debug(f"Restored: {new_path.name} → {original_name}")
            except FileNotFoundError:
                logging.warning(f"Missing file during restoration: {new_path}")


def get_metadata(flac_file: Path):
    result = subprocess.run(
        ['sox', '--i', str(flac_file)],
        capture_output=True, text=True, check=True
    )
    output = result.stdout

    def extract_value(pattern: str) -> int:
        for line in output.splitlines():
            if pattern in line:
                value = line.split(':', 1)[-1].strip()
                return int(value.split()[0]) if 'Sample Rate' in pattern else int(value.split('-')[0])
        raise ValueError(f"Pattern '{pattern}' not found")

    return (
        extract_value('Precision'),
        extract_value('Sample Rate')
    )


def process_directory(src_dir: Path):
    logging.info(f"Processing: {src_dir.name}")

    with temporary_sanitized_names(src_dir):
        flac_files = list(src_dir.rglob('*.flac'))

        if not flac_files:
            logging.warning("No FLAC files found")
            return

    try:
        formats = [get_metadata(f) for f in flac_files]
        
        common_bd = min(bd for bd, _ in formats)
        common_sr = min(sr for _, sr in formats)
        logging.info(f"Detected format: {common_bd}-bit/{common_sr}Hz")
        
        if common_bd not in (16, 24) or common_sr not in TIER_CONFIG:
            logging.warning(f"Unsupported format: {common_bd}-bit/{common_sr}Hz")
            return
        
        if common_bd == 16 and common_sr in (44100, 48000):
            process_tier(src_dir, MP3_TIER)
            return
        
        tiers = get_conversion_tiers(common_sr)
        
        for tier in tiers:
            process_tier(src_dir, tier)
            
    except Exception as e:
        logging.error(f"Processing failed: {e}")
        raise


def get_conversion_tiers(sample_rate: int):
    try:
        flac_tiers = [
            {
                'description': desc,
                'bit_depth': bd,
                'target_sr': sr,
                'suffix': suffix,
                'format': 'flac'
            } for desc, bd, sr, suffix in TIER_CONFIG[sample_rate]
        ]
        return flac_tiers + [MP3_TIER]

    except KeyError:
        raise ValueError(f"Unsupported sample rate: {sample_rate}")


def process_tier(src_dir: Path, tier: dict):
    dest_dir = src_dir.parent / f"{src_dir.name}{tier['suffix']}"

    logging.info(f"Processing {tier['description']}")

    robocopy_directory(src_dir, dest_dir)

    with temporary_sanitized_names(dest_dir):
        convert_files(dest_dir, tier)


def convert_files(directory: Path, tier: dict):
    flac_files = list(directory.rglob('*.flac'))
    logging.info(f"Converting {len(flac_files)} files to {tier['format'].upper()}")

    for flac in flac_files:
        try:
            if tier['format'] == 'flac':
                convert_flac(flac, tier)
            elif tier['format'] == 'mp3':
                convert_to_mp3(flac)
        except Exception as e:
            logging.error(f"Conversion failed for {flac.name}: {e}")


def convert_flac(flac_file: Path, tier: dict):
    temp_file = flac_file.with_name(f'temp_{flac_file.name}')
    cmd = [
        'sox', '-S', str(flac_file),
        '-R', '-G', '-b', str(tier['bit_depth']),
        str(temp_file), 'rate', '-v', '-L', str(tier['target_sr'])
    ]
    if tier['bit_depth'] == 16:
        cmd.append('dither')

    subprocess.run(cmd, check=True)
    flac_file.unlink()
    temp_file.rename(flac_file)


def convert_to_mp3(flac_file: Path):
    mp3_file = flac_file.with_suffix('.mp3')
    subprocess.run([
        'ffmpeg', '-hide_banner', '-loglevel', 'error',
        '-i', str(flac_file), '-codec:a', 'libmp3lame',
        '-b:a', MP3_TIER['bitrate'], str(mp3_file)
    ], check=True)
    
    flac_file.unlink()


def main():
    parser = argparse.ArgumentParser(description='Audio conversion pipeline')
    parser.add_argument('root_dir', type=Path, help='Source directory with FLAC files')
    args = parser.parse_args()

    if not args.root_dir.exists():
        logging.error(f"Directory not found: {args.root_dir}")
        sys.exit(1)

    process_directory(args.root_dir.resolve())
    logging.info("\nProcessing completed successfully")


if __name__ == '__main__':
    main()