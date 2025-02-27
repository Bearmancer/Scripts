import subprocess
import re
import sys
import unicodedata, logging, time
from pathlib import Path
from convert_music import process_directory

logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(levelname)s - %(message)s',
    handlers=[
        logging.FileHandler("sacd_processing.log"),
        logging.StreamHandler(sys.stdout)
    ]
)


def run_cmd(cmd, **kwargs):
    return subprocess.run(cmd, capture_output=True, text=True, **kwargs)


def extract_sacds(p: Path):
    try:
        normalized = ''.join(c for c in unicodedata.normalize('NFD', p.name)
                             if unicodedata.category(c) != 'Mn')
        if normalized != p.name:
            p = p.rename(p.with_name(normalized))

        isos = list(p.rglob("*.iso"))
        logging.info(f"Found {len(isos)} SACD images in {p.name}")

        for i, iso in enumerate(isos, 1):
            logging.info(f"Processing SACD {i}/{len(isos)}: {iso.name}")
            for flac_dir in iso_to_flac(iso, p):
                process_directory(flac_dir)
    except Exception:
        logging.error("Extraction failed", exc_info=True)
        raise


def iso_to_flac(iso: Path, base: Path):
    result = run_cmd(['sacd_extract', '-P', '-i', str(iso)], cwd=base)
    logging.info(f"Probe result: output={result.stdout[:100]}")
    if result.returncode != 0:
        logging.error(f"SACD extraction probe failed for {iso.name}")
        return []

    extracted_dirs = []

    formats = [
        ('Multichannel|5|6', ' (Multichannel)', ['-m', '-p', '-c']),
        ('Stereo|2', ' (Stereo)', ['-2', '-p', '-c'])
    ]

    for pattern, suffix, cmd in formats:
        if re.search(pattern, result.stdout):
            logging.info(f"Found {suffix.strip()} version")
            out_dir = base.parent / f"{base.name}{suffix}"
            out_dir.mkdir(exist_ok=True)

            logging.info(f"Extracting to {out_dir}")
            extract_result = run_cmd(
                ['sacd_extract', *cmd, '-i', str(iso)],
                cwd=out_dir
            )
            logging.info(f"Extraction result: code={extract_result.returncode}")

            if flac_dir := next((d for d in out_dir.iterdir() if d.is_dir()), None):
                logging.info(f"Found directory {flac_dir.name}")
                dff_count = len(list(flac_dir.rglob('*.dff')))
                if dff_count > 0:
                    extracted_dirs.append(dff_to_flac(flac_dir))
            else:
                logging.warning(f"No subdirectories found in {out_dir}")

    logging.info(f"Processed {len(extracted_dirs)} directories")
    return extracted_dirs


def dff_to_flac(f: Path):
    dffs = list(f.rglob("*.dff"))

    if not dffs:
        logging.warning(f"No DFF files found in {f}")
        return f

    logging.info(f"Processing {len(dffs)} DFF files in {f.name}")

    dr = max([float(m.group(1)) for d in dffs
              if (m := re.search(r'max_volume: (-\d+\.?\d*) dB',
                                 run_cmd(['ffmpeg', '-i', str(d), '-af', 'volumedetect',
                                          '-f', 'null', '-']).stderr))] + [0.0]) - 0.5
    logging.info(f"Calculated dynamic range adjustment: {dr}dB")

    for i, d in enumerate(dffs, 1):
        flac_path = d.with_suffix('.flac')
        logging.info(f"Converting DFF {i}/{len(dffs)}: {d.name}")

        while True:
            try:
                run_cmd(['ffmpeg', '-i', str(d), '-c:a', 'flac', '-sample_fmt', 's32',
                         '-ar', '88200', '-af', f'volume={dr}', str(flac_path)], check=True)
                break
            except Exception:
                logging.warning(f"Conversion failed for {d.name}, retrying in 10 seconds")
                time.sleep(10)

        trim = flac_path.with_stem(f'temp_{flac_path.stem}')

        while True:
            try:
                run_cmd(['sox', str(flac_path), str(trim), 'trim', '0.0065', 'reverse',
                         'silence', '1', '0', '0%', 'trim', '0.0065', 'reverse',
                         'pad', '0.0065', '0.2'], check=True)
                flac_path.unlink()
                trim.rename(flac_path)
                logging.info(f"Successfully processed: {flac_path.stem}")
                break
            except Exception:
                logging.warning(f"Trim failed for {flac_path.name}, retrying in 10 seconds")
                trim.unlink(missing_ok=True)
                time.sleep(10)

    return f


if __name__ == '__main__':
    if len(sys.argv) != 2:
        logging.error("Invalid arguments. Usage: python script.py <folder>")
        sys.exit(1)

    extract_sacds(Path(sys.argv[1]))