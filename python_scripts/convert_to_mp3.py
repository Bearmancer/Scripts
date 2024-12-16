import argparse
import subprocess
from pathlib import Path
from pathvalidate import sanitize_filepath

output = []


def main(directory):
    output_base_directory = Path("C:/Users/Lance/Desktop/Music/MP3")

    output_directory = Path(f"C:/Users/Lance/Desktop/Music/MP3/{directory.name} (MP3)")
    output_directory.mkdir(parents=True, exist_ok=True)
    subprocess.run(["robocopy", str(directory), str(output_directory), "/E", "/xf", "*.log", "*.cue", "*.md5", "*.m3u"], shell=True)

    flac_files = list(output_base_directory.rglob('*.flac'))
    failed_files = []

    for flac in flac_files:
        if not convert_flac_to_mp3(flac):
            if not convert_flac_to_mp3(sanitize_filepath(flac)):
                failed_files.append(str(flac))

    if failed_files:
        failed_files_str = "filelist:\"{}\"".format('|'.join(failed_files))
        output.append(f"Not all FLAC files were converted to MP3:\n{failed_files_str}")
    else:
        output.append("All FLAC files successfully converted to MP3.")

    print("\n".join(output))


def convert_flac_to_mp3(flac):
    try:
        retain_tags = "ARTIST=TITLE=ALBUM=DATE=GENRE=COMPOSER=PERFORMER=ALBUMARTIST=TRACKNUMBER=TOTALTRACKS=DISCNUMBER=TOTALDISCS=COMMENT=RATING"

        subprocess.run(['metaflac', f'--remove-all-tags-except={retain_tags}', str(flac)], check=True, encoding='utf-8')
        subprocess.run(['metaflac', '--dont-use-padding', '--remove', '--block-type=PICTURE,PADDING', str(flac)], check=True, encoding='utf-8')
        subprocess.run(['metaflac', '--add-padding=8192', str(flac)], check=True, encoding='utf-8')

        subprocess.run(['ffmpeg', '-i', str(flac), '-codec:a', 'libmp3lame', '-map_metadata', '0', '-id3v2_version', '3', '-b:a', '320k', str(flac.with_suffix('.mp3')), '-y'], check=True, encoding='utf-8')

        flac.unlink()
        return True

    except subprocess.CalledProcessError as e:
        output.append(f"Error processing file: {flac} - {e}")
        return False


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Convert FLAC files to MP3 and manage directories.")
    parser.add_argument("path", default=".", help="Path to the directory containing FLAC files.")
    parser.add_argument("--process_all", action="store_true", help="Process all subdirectories (default: process all subdirectories.)")

    args = parser.parse_args()
    path = Path(args.path).resolve()

    if args.process_all:
        [main(directory) for directory in path.iterdir() if directory.is_dir()]
    else:
        main(path)