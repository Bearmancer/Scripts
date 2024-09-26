import subprocess, sys
from pathlib import Path

def main():
    video_files = list(Path('.').glob('*.mkv')) + list(Path('.').glob('*.mp4'))
    
    for file in video_files:
        result = subprocess.run(['ffmpeg', '-i', str(file)], stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True)
        audio_tracks = [line for line in result.stderr.splitlines() if "Stream #" in line and "Audio" in line]
        
        if len(audio_tracks) > 1:
            output_file = file.with_name(f"{file.stem} Audio Commentary.flac")
            subprocess.run(['ffmpeg', '-i', str(file), '-map', '0:a:1', '-sample_fmt', 's16', '-acodec', 'flac', str(output_file)])
    
    for flac_file in Path('.').glob('*.flac'):
        print(flac_file.name)

if __name__ == "__main__":
    if len(sys.argv) > 0:
        main(Path(sys.argv[1]))
    else:
        print("Invalid number of arguments supplied.")