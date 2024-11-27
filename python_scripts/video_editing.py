import sys
import subprocess
import ffmpeg
import io
import pyperclip
from operator import itemgetter
from pathlib import Path
from PIL import Image

VIDEO_EXTENSIONS = [".mp4", ".mkv", ".ts", ".avi"]


def extract_chapters(video_files):
    for video_file in video_files:
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

        for chapter_index, chapter in enumerate(chapters, 1):
            formatted_index = f"{chapter_index:02}"
            output_file_name = parent_directory / f"{parent_directory.name} - Chapter {formatted_index}.{video_file.suffix}"
            try:
                (
                    ffmpeg
                    .input(str(video_file), ss=chapter['start_time'], to=chapter['end_time'])
                    .output(str(output_file_name), c='copy', avoid_negative_ts='make_zero')
                    .run()
                )
                print(f"Extracted chapter {formatted_index} from {video_file.name}.")
            except ffmpeg.Error as e:
                print(f"Failed to extract chapter {formatted_index} from {video_file}: {e.stderr.decode()}")


def batch_compression(path):
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


def remux_disc(path):
    remuxable_files = [
        f for f in path.rglob('*')
        if f.name in ('VIDEO_TS.IFO', 'index.bdmv') and 'BACKUP' not in f.parts
    ]

    if not remuxable_files:
        return print(f"No remuxable files found in {path}.")

    for file in remuxable_files:
        print(f"Converting file: {file.name}")
        result = convert_disc_to_mkv(file, path)

        if result.returncode == 0:
            print(f"File successfully converted.")
            for mkv_file in path.glob("*.mkv"):
                get_mediainfo(mkv_file)
                create_thumbnail_grid(mkv_file)

        else:
            return print(f"Could not convert the file. Error: {result.stderr.strip()}")


def convert_disc_to_mkv(file, dvd_folder):
    makemkv_path = r"C:\Program Files (x86)\MakeMKV\makemkvcon64.exe"

    makemkv_command = [
        makemkv_path, "mkv",
        f"file:{file}", "all", str(dvd_folder), "--minlength=180"
    ]

    return subprocess.run(makemkv_command, capture_output=True, text=True)


def create_thumbnail_grid(video_path, width=800, rows=8, columns=4, spacing=1):
    output_file = Path.home() / 'Desktop' / f"{video_path.parent.name} - {video_path.name}.jpg"

    try:
        video = ffmpeg.probe(str(video_path.absolute()))
    except ffmpeg.Error as e:
        print(f"Error probing video file: {e.stderr.decode()}")
        return

    duration = float(video['format']['duration'])
    timestamps = [duration * i / (rows * columns) for i in range(rows * columns)]

    def extract_thumbnail(timestamp):
        out, _ = (
            ffmpeg
            .input(str(video_path), ss=timestamp)
            .filter('scale', width, -1)
            .output('pipe:', vframes=1, format='image2', vcodec='mjpeg')
            .run(capture_stdout=True, capture_stderr=True)
        )
        return Image.open(io.BytesIO(out))

    images = [extract_thumbnail(ts) for ts in timestamps]
    max_height = max(img.height for img in images)
    grid_img = Image.new('RGB', (width * columns + spacing * (columns - 1), max_height * rows + spacing * (rows - 1)),
                         color='white')

    for idx, img in enumerate(images):
        x = (idx % columns) * (width + spacing)
        y = (idx // columns) * (max_height + spacing)
        grid_img.paste(img, (x, y))
        img.close()

    grid_img.save(output_file)
    print(f"Thumbnail grid saved to {output_file}")


def get_mediainfo(video_path):
    print(f"Getting MediaInfo for {video_path.absolute()}")
    output_file = Path.home() / 'Desktop' / f"{video_path.parent.name} - {video_path.name}.txt"

    mediainfo_command = ["mediainfo", "--Output=TXT", str(video_path)]

    result = (subprocess.run(mediainfo_command, capture_output=True, text=True)).stdout

    with open(output_file, 'w') as f:
        f.write(result)

    pyperclip.copy(result)

    print("MediaInfo successfully created.")


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
            try:
                (
                    ffmpeg
                    .input(str(file))
                    .output(str(output_file), map='0:a:1', acodec='flac')
                    .run()
                )
                print(f"Extracted audio commentary from {file.name}.")
            except ffmpeg.Error as e:
                print(f"Failed to extract audio commentary from {file}: {e.stderr.decode()}")

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
        width = int(video_streams[0]['width'])
        height = int(video_streams[0]['height'])
        return {"Width": width, "Height": height}

    except (ffmpeg.Error, IndexError, KeyError, ValueError) as e:
        print(f"Error processing file: {filepath}. Error: {e}")

    return None


def calculate_mb_per_minute(file):
    try:
        probe = ffmpeg.probe(str(file))
        format_info = probe.get('format', {})
        duration = float(format_info.get('duration', 0))
        size = float(format_info.get('size', 0))
        mb_per_minute = (size / (1024 * 1024)) / (duration / 60)
        return mb_per_minute, size, duration

    except ffmpeg.Error as e:
        print(f"Error processing file: {file}. Error: {e.stderr.decode()}")
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
            f.write(
                f"{i}. Name: {filename}\n"
                f"MB/Minute: {mb_per_minute:.2f}\n"
                f"Size: {size / (1024 * 1024):.2f} MB\n"
                f"Duration: {duration / 60:.2f} minutes\n\n"
            )

    print(f"Output saved as '{output_file_path}'.")


def main():
    if len(sys.argv) != 3:
        print("Usage: script.py <Method> <FolderPath>")
        sys.exit(1)

    method, folder = sys.argv[1], Path(sys.argv[2])
    video_files = [file for file in folder.rglob("*") if file.suffix in VIDEO_EXTENSIONS]

    if method == "RemuxDisc":
        remux_disc(folder)
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
    elif method == "CreateThumbnailGrid":
        create_thumbnail_grid(folder)
    elif method == "GetMediaInfo":
        get_mediainfo(folder)
    else:
        print("Invalid method specified.")


if __name__ == "__main__":
    main()
