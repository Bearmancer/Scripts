import subprocess, sys
from pathlib import Path

def get_video_resolution(filepath):
    command = ["ffprobe", "-v", "error", "-select_streams", "v:0", "-show_entries", "stream=width,height", "-of", "csv=s=x:p=0", str(filepath)]
    
    try:
        result = subprocess.run(command, capture_output=True, check=True)
        output = result.stdout.decode("utf-8").strip()
        dimensions = output.split("x")
        if len(dimensions) != 2: return None
        width, height = dimensions
        return {"Width": int(width), "Height": int(height)}
    
    except (subprocess.CalledProcessError, ValueError) as e:
        print(f"Error processing file: {filepath}. Error: {e}")
        return None

def get_video_files(path):
    extensions = [".mkv", ".mp4", ".ts", ".flv"]
    video_files = []
    
    for file in Path(path).rglob('*'):
        if file.suffix.lower() in extensions:
            video_files.append(file)
                
    return video_files

if __name__ == "__main__":
    if len(sys.argv) != 2:
        print("Usage: python script.py <path_to_directory>")
        sys.exit(1)

    video_path = Path(sys.argv[1])
    if not video_path.is_dir():
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
        print(file.name)

    print("\nFiles with resolution below 1920x1080:")
    for file, resolution in files_below_1920_1080:
        print(f"{file.name}, Resolution: {resolution['Width']}x{resolution['Height']}")

    print("\nFiles with unresolved resolution:")
    for file in files_unresolved_resolution:
        print(file.name)