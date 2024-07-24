import os
import subprocess
from operator import itemgetter
import sys

def get_video_files(folder):
    video_files = []
    for root, _, files in os.walk(folder):
        for file in files:
            if file.lower().endswith(('.mp4', '.avi', '.mkv', '.mov')):
                video_files.append(os.path.join(root, file))
    return video_files

def calculate_mb_per_minute(video_file):
    cmd = ['ffprobe', '-v', 'error', '-select_streams', 'v:0', '-show_entries', 'format=duration,size', '-of', 'default=noprint_wrappers=1:nokey=1', video_file]
    output = subprocess.check_output(cmd, universal_newlines=True)
    duration, size = map(float, output.strip().split('\n'))
    mb_per_minute = (size / 1024 / 1024) / (duration / 60)
    return mb_per_minute, size, duration

def main(folder):
    video_files = get_video_files(folder)
    if not video_files:
        print("No video files found in the specified folder.")
        return

    data = []
    for video_file in video_files:
        mb_per_minute, size, duration = calculate_mb_per_minute(video_file)
        filename = os.path.basename(video_file)
        data.append((filename, mb_per_minute, size, duration))

    sorted_data = sorted(data, key=itemgetter(1), reverse=True)

    desktop_path = os.path.join(os.path.join(os.environ['USERPROFILE']), 'Desktop')  # Path to Desktop
    output_file_path = os.path.join(desktop_path, 'video_files_info.txt')

    with open(output_file_path, 'w') as f:
        for i, (filename, mb_per_minute, size, duration) in enumerate(sorted_data, 1):
            f.write(f"""
            {i}. Name: {filename}
            MB/Minute: {mb_per_minute:.2f}
            Size: {size / 1024 / 1024:.2f} MB
            Duration: {duration / 60:.2f} minutes\n""")
    print(f"Output saved as '{output_file_path}'.")

if __name__ == "__main__":
    if len(sys.argv) != 2:
        print("Usage: python script.py <folder_path>")
        sys.exit(1)
    folder_path = sys.argv[1]
    if not os.path.isdir(folder_path):
        print("Invalid folder path.")
        sys.exit(1)
    main(folder_path)