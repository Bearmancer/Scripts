import sys
import subprocess
import ffmpeg
from operator import itemgetter
from pathlib import Path

video_extensions = [".mp4", ".mkv", ".ts", ".avi"]


def extract_chapters(video_files):
    for i, video_file in enumerate(video_files, 1):
        try:
            probe = ffmpeg.probe(str(video_file), show_chapters=None)
            chapters = probe.get('chapters', [])
        except ffmpeg.Error as e:
            print(f"Failed to probe {video_file}: {e.stderr.decode()}")
            continue

        if len(chapters) <= 1:
            print(f"No chapters found in {video_file.name}.")
            continue

        parent_directory = video_file.parent
        for chapter in chapters:
            formatted_index = f"{i:02}"
            output_file_name = parent_directory / \
                f"{parent_directory.name} - Chapter {formatted_index}{video_file.suffix}"
            (
                ffmpeg
                .input(str(video_file), ss=chapter['start_time'], to=chapter['end_time'])
                .output(str(output_file_name), c='copy', avoid_negative_ts='make_zero', y=None)
                .run()
            )


def batch_compression(path):
    for file in path.rglob("*.mkv"):

        output_file_path = file.with_suffix(".mp4")
        (
            ffmpeg.input(str(file))
            .output(str(output_file_path), preset='medium')
            .run()
        )
        file.unlink()
        print(f"Successfully converted {file}.")


def remux_dvd(path):
    directories = [path] + [d for d in path.rglob("*") if d.is_dir() and "BACKUP" not in d.name]
    non_remuxable = []

    for dvd_path in directories:
        remuxable = [f for f in dvd_path.rglob("*") if f.name in ('VIDEO_TS.IFO', 'index.bdmv')]
        if remuxable:
            for file in remuxable:
                print(f"Converting file: {file} in {dvd_path}")
                convert_dvd_to_mkv(file, dvd_path)
        else:
            print(f"No remuxable files found in {dvd_path}.")
            non_remuxable.append(dvd_path)

    if non_remuxable:
        print("Folders that couldn't be remuxed:")
        for folder in non_remuxable:
            print(folder)


def convert_dvd_to_mkv(file, dvd_folder):
    output_path = dvd_folder
    output_path.mkdir(exist_ok=True)

    command = [r"C:\Program Files (x86)\MakeMKV\makemkvcon64.exe",
               "mkv", f'file:{file}', "all", str(dvd_folder), "--minlength=180"]

    result = subprocess.run(command, capture_output=True, text=True)

    if result.returncode == 0:
        print(f"Successfully converted {file}.")
    else:
        print(f"Failed to convert {file}: {result.stderr}")


def extract_audio_commentary(video_files):
    for file in video_files:
        try:
            probe = ffmpeg.probe(str(file))
            audio_streams = [stream for stream in probe['streams']
                             if stream['codec_type'] == 'audio']
        except ffmpeg.Error as e:
            print(f"Failed to probe {file}: {e.stderr.decode()}")
            continue

        if len(audio_streams) > 1:
            output_file = file.with_name(f"{file.stem} Audio Commentary.flac")
            (
                ffmpeg
                .input(str(file))
                .output(str(output_file), map='0:a:1', acodec='flac')
                .run()
            )

    for flac_file in Path('.').glob('*.flac'):
        print(flac_file.name)


def print_video_resolution(video_files):
    files_1920_1080 = []
    files_below_1920_1080 = []
    files_unresolved_resolution = []

    for file in video_files:
        resolution = get_video_resolution(file)
        if resolution:
            if resolution["Width"] >= 1920 and resolution["Height"] >= 1080:
                files_1920_1080.append(file.name)
            else:
                files_below_1920_1080.append((file.name, resolution))
        else:
            files_unresolved_resolution.append(file.name)

    print("Files with a resolution of 1920x1080:")
    for name in files_1920_1080:
        print(name)

    print("\nFiles with resolution below 1920x1080:")
    for name, res in files_below_1920_1080:
        print(f"{name}, Resolution: {res['Width']}x{res['Height']}")

    print("\nFiles with unresolved resolution:")
    for name in files_unresolved_resolution:
        print(name)


def get_video_resolution(filepath):
    try:
        probe = ffmpeg.probe(str(filepath))
        video_streams = [stream for stream in probe['streams']
                         if stream['codec_type'] == 'video']
        if video_streams:
            width = int(video_streams[0]['width'])
            height = int(video_streams[0]['height'])
            return {"Width": width, "Height": height}
    except ffmpeg.Error as e:
        print(f"Error processing file: {filepath}. Error: {e.stderr.decode()}")
    return None


def calculate_mb_per_minute(file):
    try:
        probe = ffmpeg.probe(str(file))
        format_info = probe.get('format', {})
        duration = float(format_info.get('duration', 0))
        size = float(format_info.get('size', 0))
        mb_per_minute = (size / (1024 * 1024)) / (duration / 60)
        return mb_per_minute, size, duration
    except Exception as e:
        print(f"Error processing file: {file}. Error: {e}")
        return 0, 0, 0


def calculate_mb_for_directory(video_files):
    data = []
    for file in video_files:
        mb_per_minute, size, duration = calculate_mb_per_minute(file)
        data.append((file.name, mb_per_minute, size, duration))

    sorted_data = sorted(data, key=itemgetter(1), reverse=True)
    output_file_path = Path.home() / 'Desktop' / 'video_files_info.txt'

    with open(output_file_path, 'w') as f:
        for i, (filename, mb_per_minute, size, duration) in enumerate(sorted_data, 1):
            f.write(f"{i}. Name: {filename}\nMB/Minute: {mb_per_minute:.2f}\nSize: {size / (1024 * 1024):.2f} MB\nDuration: {duration / 60:.2f} minutes\n\n")

    print(f"Output saved as '{output_file_path}'.")


def main():
    if len(sys.argv) != 3:
        print("Usage: script.py <Method> <FolderPath>")
        sys.exit(1)

    method, folder = sys.argv[1], Path(sys.argv[2])
    video_files = [file for file in folder.rglob(
        "*") if file.suffix in video_extensions]

    if method == "RemuxDVD":
        remux_dvd(folder)
    elif method == "ExtractChapters":
        extract_chapters(video_files)
    elif method == "BatchCompression":
        batch_compression(folder)
    elif method == "ExtractAudioCommentary":
        extract_audio_commentary(video_files)
    elif method == "PrintVideoResolution":
        print_video_resolution(video_files)
    elif method == "CalculateMBPerMinute":
        calculate_mb_for_directory(video_files)
    else:
        print("Invalid method specified.")


if __name__ == "__main__":
    main()
