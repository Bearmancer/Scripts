import subprocess
import os
import sys


def get_video_resolution(filepath):
    command = [
        "ffprobe",
        "-v",
        "error",
        "-select_streams",
        "v:0",
        "-show_entries",
        "stream=width,height",
        "-of",
        "csv=s=x:p=0",
        filepath,
    ]
    
    try:
        result = subprocess.run(command, capture_output=True, check=True)
        output = result.stdout.decode("utf-8").strip()
        dimensions = output.strip().split("x")
        if len(dimensions) != 2:    
            return None
        width, height = dimensions
        return {"Width": int(width), "Height": int(height)}
    
    except (subprocess.CalledProcessError, ValueError) as e:
        print(f"Error processing file: {filepath}. Error: {e}")
        return None


def get_video_files(path):
    extensions = [".mkv", ".mp4", ".ts", ".flv"]
    video_files = []
    
    for root, _, files in os.walk(path):
        for file in files:
            if file.lower().endswith(tuple(extensions)):
                video_files.append(os.path.join(root, file))
                
    return video_files


if __name__ == "__main__":
    if len(sys.argv) != 2:
        print("Usage: python script.py <path_to_directory>")
        sys.exit(1)

    video_path = sys.argv[1]
    if not os.path.isdir(video_path):
        print("Invalid directory path.")
        sys.exit(1)

    video_files = get_video_files(video_path)

    files_1920_1080 = []
    files_below_1920_1080 = []
    files_unresolved_resolution = []

    for file in video_files:
        resolution = get_video_resolution(file)
        if resolution:
            if resolution["Width"] >= 1920 and resolution["Height"] >= 1080:
                files_1920_1080.append((file, resolution))
            else:
                files_below_1920_1080.append((file, resolution))
        else:
            files_unresolved_resolution.append(file)

    print("Files with a resolution of 1920x1080:")
    for file, resolution in files_1920_1080:
        print(f"{os.path.basename(file)}")

    print("\nFiles with resolution below 1920x1080:")
    for file, resolution in files_below_1920_1080:
        print(f"{os.path.basename(file)}, Resolution: {resolution['Width']}x{resolution['Height']}")

    print("\nFiles with unresolved resolution:")
    for file in files_unresolved_resolution:
        print(f"{os.path.basename(file)}")
