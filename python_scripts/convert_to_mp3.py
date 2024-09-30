import shutil, subprocess, sys
from pathlib import Path

def main(directory):
    log_path = Path("C:/Users/Lance/Desktop/Conversion Log.txt")
    output_base_path = Path("C:/Users/Lance/Desktop/Torrents/MP3")

    robocopy(directory, output_base_path)

    for subfolder in directory.iterdir():
        if subfolder.is_dir():
            output_path = output_base_path / subfolder.name
            output_path.mkdir(parents=True, exist_ok=True)

            flac_files = list(subfolder.rglob('*.flac'))

            for flac_path in flac_files:
                new_flac_path = output_path / flac_path.relative_to(subfolder)

                new_flac_path.parent.mkdir(parents=True, exist_ok=True)

                shutil.copy(flac_path, new_flac_path)

                subprocess.run(['metaflac', '--dont-use-padding', '--remove', '--block-type=PICTURE,PADDING', str(new_flac_path)], encoding='utf-8')
                subprocess.run(['metaflac', '--add-padding=8192', str(new_flac_path)], encoding='utf-8')

                mp3_path = new_flac_path.with_suffix('.mp3')
                mp3_path.parent.mkdir(parents=True, exist_ok=True)

                try:
                    subprocess.run(['ffmpeg', '-i', str(new_flac_path), '-codec:a', 'libmp3lame', '-map_metadata', '0', '-id3v2_version', '3', '-b:a', '320k', str(mp3_path), '-y'], encoding='utf-8')
                except Exception as e:
                    with log_path.open('a', encoding='utf-8') as log_file:
                        log_file.write(f"Exception while converting: {new_flac_path} - {e}")

                print(f"Now deleting: {new_flac_path}")
                new_flac_path.unlink()

            mp3_files = list(output_path.rglob('*.mp3'))
            mp3_set = {mp3.relative_to(output_path) for mp3 in mp3_files}

            missing_mp3s = [flac for flac in flac_files if flac.with_suffix('.mp3').relative_to(subfolder) not in mp3_set]

            if missing_mp3s:
                with log_path.open('a', encoding='utf-8') as log_file:
                    log_file.write(f"Problematic Files in {subfolder}:\nfilelist: {'|'.join(map(str, missing_mp3s))}\n------------------\n")
            else:
                with log_path.open('a', encoding='utf-8') as log_file:
                    log_file.write(f"All FLAC Files for {subfolder} were successfully converted to MP3.\n------------------\n")

def robocopy(src, dst):
    dst.mkdir(parents=True, exist_ok=True)

    excluded_extensions = {'.log', '.cue', '.md5', '.flac', '.m3u'}

    for file in src.rglob('*'):
        if file.is_file() and file.suffix.lower() not in excluded_extensions:
            relative_path = file.relative_to(src)
            destination_file_path = dst / relative_path

            destination_file_path.parent.mkdir(parents=True, exist_ok=True)

            shutil.copy2(file, destination_file_path)
            print(f"Copied: {file} to {destination_file_path}")

if __name__ == "__main__":
    if len(sys.argv) == 3:
        robocopy(Path(sys.argv[1]), Path(sys.argv[2]))
    elif len(sys.argv) == 2:
        main(Path(sys.argv[1]))
    else:
        print("Invalid number of arguments entered.")