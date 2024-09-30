import subprocess, sys, json
from operator import itemgetter
from pathlib import Path

video_extensions = ["mp4", "mkv", "ts", "avi"]
path: Path
video_files = None

def extract_chapters():
    print(video_files)

    i = 1

    for video_file in video_files:
        parent_directory = video_file.parent
        command = ["ffprobe.exe", "-v", "error", "-i", str(video_file), "-print_format", "json", "-show_chapters"]
        result = subprocess.run(command, capture_output=True, text=True)

        if result.returncode != 0:
            print(f"Failed to probe {video_file}: {result.stderr.strip()}")
            continue

        json_chapters = json.loads(result.stdout)

        if len(json_chapters.get("chapters", [])) <= 1:
            print(f"No chapters found in {video_file.name}.")
            continue

        for chapter in json_chapters["chapters"]:
            formatted_index = f"{i:02}"
            output_file_name = parent_directory / f"{parent_directory.name} - Chapter {formatted_index}{video_file.suffix}"
            subprocess.run(["ffmpeg.exe", "-i", str(video_file), "-ss", str(chapter["start_time"]), "-to", str(chapter["end_time"]), "-c", "copy", "-avoid_negative_ts", "make_zero", "-y", str(output_file_name)])
            i += 1

def batch_compression():
    mkv_files = path.rglob("*.mkv")

    for file in mkv_files:
        output_file_path = file.with_suffix(".mp4")

        command = ["HandBrakeCLI", "--preset-import-gui", "-i", str(file), "-o", str(output_file_path)]
        result = subprocess.run(command, capture_output=True, text=True)

        if result.returncode == 0:
            file.unlink()
            print(f"Successfully converted {file}.")
        else:
            print(f"Failed to convert {file}: {result.stderr.strip()}")

def remux_dvd():
    directories = [path] + [d for d in path.rglob("*") if d.is_dir() and "BACKUP" not in d.name]

    non_remuxable = []

    for dvd_path in directories:
        if dvd_path.is_dir():
            remuxable = list(dvd_path.rglob("*"))
            remuxable = [f for f in remuxable if f.name in ('VIDEO_TS.IFO', 'index.bdmv')]

            for file in remuxable:
                print(f"Converting file: {file} in {dvd_path}")
                convert_dvd_to_mkv(file, dvd_path)

            if not remuxable:
                print(f"No remuxable files found in {dvd_path}.")
                non_remuxable.append(dvd_path)

    if non_remuxable:
        print("Folders that couldn't be remuxed:")
        for folder in non_remuxable:
            print(folder)

def convert_dvd_to_mkv(file, dvd_folder):
    output_path = dvd_folder / "Converted"
    output_path.mkdir(exist_ok=True)

    command = [r"C:\Program Files (x86)\MakeMKV\makemkvcon64.exe", "mkv", f'file:{file}', "all", str(dvd_folder), "--minlength=180"]

    result = subprocess.run(command, capture_output=True, text=True)

    if result.returncode == 0:
        print(f"Successfully converted {file}.")
    else:
        print(f"Failed to convert {file}: {result.stderr}")

def extract_audio_commentary():
    for file in video_files:
        result = subprocess.run(['ffmpeg', '-i', str(file)], stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True)
        audio_tracks = [line for line in result.stderr.splitlines() if "Stream #" in line and "Audio" in line]

        if len(audio_tracks) > 1:
            output_file = file.with_name(f"{file.stem} Audio Commentary.flac")
            subprocess.run(
                ['ffmpeg', '-i', str(file), '-map', '0:a:1', '-sample_fmt', 's16', '-acodec', 'flac', str(output_file)])

    for flac_file in Path('.').glob('*.flac'):
        print(flac_file.name)

def print_video_resolution():
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

def get_video_resolution(filepath):
    command = ["ffprobe", "-v", "error", "-select_streams", "v:0", "-show_entries", "stream=width,height", "-of",
               "csv=s=x:p=0", str(filepath)]

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

def calculate_mb_per_minute(video_file):
    command = ['ffprobe', '-v', 'error', '-select_streams', 'v:0', '-show_entries', 'format=duration,size', '-of',
           'default=noprint_wrappers=1:nokey=1', str(video_file)]
    output = subprocess.check_output(command, universal_newlines=True)
    duration, size = map(float, output.strip().split('\n'))
    mb_per_minute = (size / 1024 / 1024) / (duration / 60)
    return mb_per_minute, size, duration

def calculate_mb_for_directory():
    data = []

    for video_file in video_files:
        mb_per_minute, size, duration = calculate_mb_per_minute(video_file)
        data.append((video_file.name, mb_per_minute, size, duration))

    sorted_data = sorted(data, key=itemgetter(1), reverse=True)

    output_file_path = Path.home() / 'Desktop' / 'video_files_info.txt'

    with open(output_file_path, 'w') as f:
        for i, (filename, mb_per_minute, size, duration) in enumerate(sorted_data, 1):
            f.write(
            f"""
            {i}. Name: {filename}
            MB/Minute: {mb_per_minute:.2f}
            Size: {size / 1024 / 1024:.2f} MB
            Duration: {duration / 60:.2f} minutes
            """)

    print(f"Output saved as '{output_file_path}'.")

if __name__ == "__main__":
    if len(sys.argv) != 3:
        print("Usage: script.py <root_dir> <FolderPath>")
        sys.exit(1)

    method = sys.argv[1]
    path = Path(sys.argv[2])

    video_files = [file for file in path.rglob("*") if file.suffix in video_extensions]

    if method == "RemuxDVD":
        remux_dvd()
    elif method == "ExtractChapters":
        extract_chapters()
    elif method == "BatchCompression":
        batch_compression()
    elif method == "ExtractAudioCommentary":
        extract_audio_commentary()
    elif method == "PrintVideoResolution":
        print_video_resolution()
    elif method == "CalculateMBPerMinute":
        calculate_mb_for_directory()
    else:
        print("Invalid arguments passed.")