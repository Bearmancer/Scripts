import subprocess
import sys
import pyperclip
from pathlib import Path

output = []


def main(directory, process_all=False):
    output_base_path = Path("C:/Users/Lance/Desktop/Music/MP3")

    if process_all:
        directories_to_process = [d for d in directory.iterdir() if d.is_dir()]
    else:
        directories_to_process = [directory]

    for dir_to_process in directories_to_process:
        output_path = Path(f"C:/Users/Lance/Desktop/Music/MP3/{dir_to_process.name} (MP3)")
        output_path.mkdir(parents=True, exist_ok=True)
        subprocess.run(["robocopy", str(dir_to_process), str(output_path), "/E", "/xf", "*.log", "*.cue", "*.md5", "*.m3u"], shell=True)

    flac_files = list(output_base_path.rglob('*.flac'))
    failed_files = []

    for flac in flac_files:
        if not convert_flac_to_mp3(flac):
            failed_files.append(str(flac))

    if failed_files:
        failed_files_str = "filelist:\"{}\"".format('|'.join(failed_files))
        pyperclip.copy(failed_files_str)
        output.append(f"Not all FLAC files were converted to MP3: {failed_files_str}")
    else:
        output.append("All FLAC files successfully converted to MP3.")

    print(output)

    for folder in output_base_path.glob('*'):
        if folder.is_dir():
            new_folder_path = folder.parent / folder.name + " (MP3)"
            folder.rename(new_folder_path)


def convert_flac_to_mp3(flac):
    try:
        retain_tags = "ARTIST=TITLE=ALBUM=DATE=GENRE=COMPOSER=PERFORMER=ALBUMARTIST=TRACKNUMBER=TOTALTRACKS=DISCNUMBER=TOTALDISCS=COMMENT=RATING"

        subprocess.run(['metaflac', f'--remove-all-tags-except={retain_tags}', str(flac)], check=True, encoding='utf-8')

        subprocess.run(['metaflac', '--dont-use-padding', '--remove', '--block-type=PICTURE,PADDING', str(flac)], check=True, encoding='utf-8')
        subprocess.run(['metaflac', '--add-padding=8192', str(flac)], check=True, encoding='utf-8')

        subprocess.run(['ffmpeg', '-i', str(flac), '-codec:a', 'libmp3lame', '-map_metadata', '0' '-id3v2_version', '3', '-b:a', '320k', str(flac.with_suffix('.mp3')), '-y'], check=True, encoding='utf-8')

        flac.unlink()
        return True

    except subprocess.CalledProcessError as e:
        output.append(f"Error processing file: {flac} - {e}")
        return False


if __name__ == "__main__":
    if len(sys.argv) > 1:
        path = Path(sys.argv[1]).resolve()
        process_subfolders = str(sys.argv[2:3]).lower() == ['true']
        main(path, process_subfolders)
    else:
        print("Invalid number of arguments entered.")
        exit()