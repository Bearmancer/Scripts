import subprocess
import sys
from pathlib import Path
from misc import log_to_file
import pyperclip

def main(directory):
    output_base_path = Path("C:/Users/Lance/Desktop/Music/MP3")
    subprocess.run( ["robocopy", str(directory), str(output_base_path), "/E", "/xf", "*.log", "*.cue", "*.md5", "*.m3u"], shell=True)
    flac_files = list(output_base_path.rglob('*.flac'))
    failed_files = []

    for flac in flac_files:
        if not convert_flac_to_mp3(flac):
            failed_files.append(str(flac))

    if failed_files:
        failed_files = "filelist:\"{'|'.join(failed_files)}\""
        pyperclip.copy(failed_files)
        output = f"Not all FLAC files were converted to MP3: {failed_files}"
        print(output)
        log_to_file(output)
    else:
        output = "All FLAC files successfully converted to MP3."
        print(output)


def convert_flac_to_mp3(flac):
    try:
        retain_tags = "ARTIST=TITLE=ALBUM=DATE=GENRE=COMPOSER=PERFORMER=ALBUMARTIST=TRACKNUMBER=TOTALTRACKS=DISCNUMBER=TOTALDISCS=COMMENT=RATING"

        subprocess.run(['metaflac', f'--remove-all-tags-except={retain_tags}', str(flac)], check=True, encoding='utf-8')

        subprocess.run(['metaflac', '--dont-use-padding', '--remove', '--block-type=PICTURE,PADDING', str(flac)], check=True, encoding='utf-8')
        subprocess.run(['metaflac', '--add-padding=8192', str(flac)], check=True, encoding='utf-8')

        subprocess.run(['ffmpeg', '-i', str(flac), '-codec:a', 'libmp3lame', '-map_metadata', '0', '-id3v2_version', '3', '-b:a',
                        '320k', str(flac.with_suffix('.mp3')), '-y'], check=True, encoding='utf-8')
        flac.unlink()
        return True
    except subprocess.CalledProcessError as e:
        log_to_file(f"Error processing file: {flac} - {e}")
        return False


if __name__ == "__main__":
    if len(sys.argv) == 2:
        main(Path(sys.argv[1]))
    else:
        print("Invalid number of arguments entered.")
        exit()