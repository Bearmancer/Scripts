import shutil, subprocess, sys
from pathlib import Path

def mp3_conversion(directory=Path.cwd()):
    log_path = Path("C:/Users/Lance/Desktop/Conversion Log.txt")
    output_path = Path("C:/Users/Lance/Desktop/Torrents/MP3") / directory.name

    output_path.mkdir(parents=True, exist_ok=True)

    flac_files = list(directory.rglob('*.flac'))

    for flac_path in flac_files:
        new_flac_path = output_path / flac_path.relative_to(directory)
        
        new_flac_path.parent.mkdir(parents=True, exist_ok=True)

        shutil.copy(flac_path, new_flac_path)

        subprocess.run(['metaflac', '--dont-use-padding', '--remove', '--block-type=PICTURE,PADDING', str(new_flac_path)])
        subprocess.run(['metaflac', '--add-padding=8192', str(new_flac_path)])

        mp3_path = new_flac_path.with_suffix('.mp3')
        mp3_path.parent.mkdir(parents=True, exist_ok=True)

        try:
            subprocess.run(['ffmpeg', '-i', str(new_flac_path), '-codec:a', 'libmp3lame', '-map_metadata', '0', '-id3v2_version', '3', '-b:a', '320k', str(mp3_path), '-y'])
        except Exception as e:
            error_msg = f"Exception while converting: {new_flac_path} - {e}"
            print(error_msg)
            with log_path.open('a') as log_file:
                log_file.write(error_msg + '\n')

        print(f"Now deleting: {new_flac_path}")
        new_flac_path.unlink()

    mp3_files = list(output_path.rglob('*.mp3'))
    mp3_set = {mp3.relative_to(output_path) for mp3 in mp3_files}

    missing_mp3s = [flac.relative_to(directory) for flac in flac_files if flac.with_suffix('.mp3') not in mp3_set]

    if missing_mp3s:
        message = f"Problematic Files:\nfilelist: {'|'.join(map(str, missing_mp3s))}"
        print(message)
        with log_path.open('a') as log_file:
            log_file.write(message + '\n')
    else:
        message = f"All FLAC Files for {directory} were successfully converted to MP3.\n------------------\n"
        print(message)
        with log_path.open('a') as log_file:
            log_file.write(message + '\n')

if __name__ == "__main__":
    if len(sys.argv) > 1:
        mp3_conversion(Path(sys.argv[1]))
    else:
        print("Please provide a directory path.")