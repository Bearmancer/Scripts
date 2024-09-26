import subprocess, sys
from operator import itemgetter
from pathlib import Path

def get_video_files(folder):
    return [file for file in Path(folder).rglob('*') if file.suffix.lower() in {'.mp4', '.avi', '.mkv', '.mov'}]

def calculate_mb_per_minute(video_file):
    cmd = ['ffprobe', '-v', 'error', '-select_streams', 'v:0', '-show_entries', 'format=duration,size', '-of', 'default=noprint_wrappers=1:nokey=1', str(video_file)]
    output = subprocess.check_output(cmd, universal_newlines=True)
    duration, size = map(float, output.strip().split('\n'))
    mb_per_minute = (size / 1024 / 1024) / (duration / 60)
    return mb_per_minute, size, duration

def main(folder: Path):
    video_files = get_video_files(folder)
    if not video_files:
        print("No video files found in the specified folder.")
        return

    data = []
    for video_file in video_files:
        mb_per_minute, size, duration = calculate_mb_per_minute(video_file)
        filename = video_file.name
        data.append((filename, mb_per_minute, size, duration))

    sorted_data = sorted(data, key=itemgetter(1), reverse=True)

    output_file_path = Path.home() / 'Desktop' / 'video_files_info.txt'
    
    with open(output_file_path, 'w') as f:
        for i, (filename, mb_per_minute, size, duration) in enumerate(sorted_data, 1):
            f.write(f"""
            {i}. Name: {filename}
            MB/Minute: {mb_per_minute:.2f}
            Size: {size / 1024 / 1024:.2f} MB
            Duration: {duration / 60:.2f} minutes""")
    
    print(f"Output saved as '{output_file_path}'.")

if __name__ == "__main__":
    if len(sys.argv) > 0:
        main(Path(sys.argv[1]))
        
    else:
        print("Usage: python script.py <folder_path>")
        sys.exit(1)