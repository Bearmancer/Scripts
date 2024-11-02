import subprocess
import sys
import pyperclip
from pathlib import Path
from pathvalidate import sanitize_filepath

output = []


def main(path, process_all):
    output_base_path = Path("C:/Users/Lance/Desktop/Music/MP3")

    if process_all:
        directories = [d for d in path.iterdir() if d.is_dir()]
    else:
        directories = [path]

    for directory in directories:
        output_path = Path(f"C:/Users/Lance/Desktop/Music/MP3/{directory.name} (MP3)")
        output_path.mkdir(parents=True, exist_ok=True)
        subprocess.run(["robocopy", str(directory), str(output_path), "/E", "/xf", "*.log", "*.cue", "*.md5", "*.m3u"], shell=True)

    flac_files = list(output_base_path.rglob('*.flac'))
    failed_files = []

    for flac in flac_files:
        if not convert_flac_to_mp3(flac):
            if not convert_flac_to_mp3(sanitize_filepath(flac)):
                failed_files.append(flac)

    if failed_files:
        failed_files_str = "filelist:\"{}\"".format('|'.join(failed_files))
        pyperclip.copy(failed_files_str)
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
    if len(sys.argv) > 1:
        location = Path(sys.argv[1]).resolve()
        process_subfolders = sys.argv[2].lower() == 'true' if len(sys.argv) > 2 else True
        main(location, process_subfolders)

    else:
        print("Invalid number of arguments entered.")
        exit()