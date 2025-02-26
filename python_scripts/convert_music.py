import subprocess
import sys
import argparse
import logging
from pathlib import Path
from typing import Dict
from pathvalidate import sanitize_filename

logging.basicConfig(
    level=logging.INFO,
    format='%(levelname)s - %(message)s',
    handlers=[logging.StreamHandler(sys.stdout)]
)

MP3_TIER = {
    'desc': '320kbps MP3', 'bitrate': '320k', 'suffix': ' (MP3)', 'format': 'mp3'
}

TIER_CONFIG = {
    44100: [{'desc': '16-bit/44.1kHz FLAC', 'bit_depth': 16, 'target_sr': 44100, 'suffix': ' (16-44.1)', 'format': 'flac'}],
    48000: [{'desc': '16-bit/48kHz FLAC', 'bit_depth': 16, 'target_sr': 48000, 'suffix': ' (16-48)', 'format': 'flac'}],
    88200: [{'desc': '16-bit/44.1kHz FLAC', 'bit_depth': 16, 'target_sr': 44100, 'suffix': ' (16-44.1)', 'format': 'flac'}],
    96000: [{'desc': '16-bit/48kHz FLAC', 'bit_depth': 16, 'target_src': 48000, 'suffix': ' (16-48)', 'format': 'flac'}],
    176400: [
        {'desc': '24-bit/88.2kHz FLAC', 'bit_depth': 24, 'target_sr': 88200, 'suffix': ' (24-88.2)', 'format': 'flac'},
        {'desc': '16-bit/44.1kHz FLAC', 'bit_depth': 16, 'target_sr': 44100, 'suffix': ' (16-44.1)', 'format': 'flac'}
    ],
    192000: [
        {'desc': '24-bit/96kHz FLAC', 'bit_depth': 24, 'target_sr': 96000, 'suffix': ' (24-96)', 'format': 'flac'},
        {'desc': '16-bit/48kHz FLAC', 'bit_depth': 16, 'target_sr': 48000, 'suffix': ' (16-48)', 'format': 'flac'}
    ]
}


def main():
    parser = argparse.ArgumentParser(description='Audio conversion pipeline')
    parser.add_argument('root_dir', type=Path, help='Source directory with FLAC files')
    parser.add_argument('-f', '--format', choices=['flac', 'mp3', 'all'], default='all')
    
    args = parser.parse_args()

    if not args.root_dir.exists():
        logging.error(f"Directory not found: {args.root_dir}")
        sys.exit(1)

    process_directory(args.root_dir, args.format)
    logging.info("\nProcessing completed")


def get_metadata(file: Path):
    output = subprocess.run(['sox', '--i', str(file)], capture_output=True, text=True, check=True).stdout
    
    extract = lambda p: int(next(l for l in output.splitlines() if p in l).split(':')[-1].strip().split()[0].split('-')[0])
    
    return extract('Precision'), extract('Sample Rate')

def process_directory(src_dir: Path, format_choice: str):
    logging.info(f"Processing: {src_dir.name}")
    
    with DirectoryContext(src_dir):
        if not (flac_files := list(src_dir.rglob('*.flac'))):
            return logging.warning("No FLAC files found")
        
        common_bd, common_sr = (min(x) for x in zip(*[get_metadata(f) for f in flac_files]))
        logging.info(f"Detected format: {common_bd}-bit/{common_sr}Hz")

        if common_bd not in {16,24} or common_sr not in TIER_CONFIG:
            return logging.warning(f"Unsupported format: {common_bd}-bit/{common_sr}Hz")

        if tier.get("bit_depth") == common_bd and tier.get("target_sr") == common_sr:
            return logging.info(f"No conversion required. Bit depth: {common_bd}, Sample rate: {common_sr}")

        tiers = [
            tier for tier in (
                [MP3_TIER] if format_choice == "mp3"
                else TIER_CONFIG[common_sr] + ([MP3_TIER] if format_choice == "all" else [])
            )
        ]
                
        for tier in tiers:
            process_tier(src_dir, tier)


def process_tier(src_dir: Path, tier: Dict):
    dest_dir = src_dir.parent / f"{src_dir.name}{tier['suffix']}"
    logging.info(f"Converting to: {tier['desc']}")

    if (rc := subprocess.run(['robocopy', str(src_dir), str(dest_dir), '/S', '/XF', '*.log', '*.m3u', '*.cue', '*.md5'],
                            stdout=subprocess.DEVNULL).returncode) >= 8:
        raise RuntimeError(f"Robocopy failed: {rc}")

    with DirectoryContext(dest_dir):
        if flac_files := list(dest_dir.rglob('*.flac')):
            conv = convert_flac if tier['format'] == 'flac' else convert_to_mp3
            
            for i, f in enumerate(flac_files, 1):
                try: 
                    conv(f, tier); print(f"\rConverted {i}/{len(flac_files)}", end='')
                except Exception as e: 
                    logging.error(f"Failed {f.name}: {e}")

def convert_flac(file: Path, tier: Dict):
    temp = file.with_name(f'temp_{file.name}')

    cmd = ['sox', '-S', str(file), '-R', '-G', '-b', str(tier['bit_depth']), str(temp), 'rate', '-v', '-L', str(tier['target_sr'])]
    
    if tier['bit_depth'] == 16: cmd.append('dither')
    
    subprocess.run(cmd, check=True)
    
    file.unlink(); temp.rename(file)

def convert_to_mp3(file: Path, tier: Dict):
    subprocess.run(['ffmpeg', '-i', str(file), '-codec:a', 'libmp3lame', '-b:a', tier['bitrate'], str(file.with_suffix('.mp3'))], check=True)
    
    file.unlink()

class DirectoryContext:
    def __init__(self, directory: Path):
        self.dir = directory
        self.rename_map = {}

    def __enter__(self):
        self.rename_map = {p: p.with_name(sanitize_filename(p.name)) for p in self.dir.rglob('*') 
                          if p.is_file() and (s := sanitize_filename(p.name)) != p.name and not p.with_name(s).exists()}
        for o, n in self.rename_map.items(): o.rename(n)
        return self

    def __exit__(self, *_):
        for o, n in self.rename_map.items():
            if n.exists() and not o.exists(): n.rename(o)

if __name__ == '__main__': main()