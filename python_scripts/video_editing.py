import argparse
import ffmpeg
import pyperclip
import subprocess
from operator import itemgetter
from pathlib import Path
from image_extraction import extract_images
from misc import run_command


VIDEO_EXTENSIONS = [".mp4", ".mkv", ".ts", ".avi"]


def extract_chapters(video_files):
    for video_file in video_files:
        try:
            probe = ffmpeg.probe(str(video_file), show_chapters=None)
            chapters = probe.get("chapters", [])
        except ffmpeg.Error as e:
            print(f"Failed to probe {video_file}: {e.stderr.decode()}")
            continue

        if len(chapters) <= 1:
            print(f"No chapters found in {video_file.name}.")
            continue

        parent_directory = video_file.parent

        for chapter_index, chapter in enumerate(chapters, 1):
            formatted_index = f"{chapter_index:02}"
            output_file_name = (
                parent_directory
                / f"{parent_directory.name} - Chapter {formatted_index}.{video_file.suffix}"
            )
            try:
                (
                    ffmpeg.input(
                        str(video_file),
                        ss=chapter["start_time"],
                        to=chapter["end_time"],
                    )
                    .output(
                        str(output_file_name), c="copy", avoid_negative_ts="make_zero"
                    )
                    .run()
                )
                print(f"Extracted chapter {formatted_index} from {video_file.name}.")
            except ffmpeg.Error as e:
                print(
                    f"Failed to extract chapter {formatted_index} from {video_file}: {e.stderr.decode()}"
                )


def batch_compression(path: Path):
    mkv_files = path.rglob("*.mkv")

    for file in mkv_files:
        output_file_path = file.with_suffix(".mp4")

        command = [
            "HandBrakeCLI",
            "--preset-import-gui",
            "-i",
            str(file),
            "-o",
            str(output_file_path),
        ]
        result = subprocess.run(command, capture_output=True, text=True)

        if result.returncode == 0:
            file.unlink()
            print(f"Successfully converted {file}.")
        else:
            print(f"Failed to convert {file}: {result.stderr.strip()}")


def remux_disc(path: Path, fetch_mediainfo: bool = True):
    remuxable_files = [
        f
        for f in path.rglob("*")
        if f.name in ("VIDEO_TS.IFO", "index.bdmv") and "BACKUP" not in f.parts
    ]

    if not remuxable_files:
        raise FileNotFoundError(f"No remuxable files found in {path}.")

    for file in remuxable_files:
        print(f"Converting file: {file.name}")
        convert_disc_to_mkv(file, file.parent)

        print(f"Successfully converted: {file}.")

        if fetch_mediainfo:
            for mkv_file in path.glob("*.mkv"):
                get_mediainfo(mkv_file)
                extract_images(mkv_file)


def convert_disc_to_mkv(file: Path, dvd_folder: Path):
    makemkv_path = r"C:\Program Files (x86)\MakeMKV\makemkvcon64.exe"

    makemkv_command = [
        makemkv_path,
        "mkv",
        f"file:{file}",
        "all",
        str(dvd_folder),
        "--minlength=180",
    ]

    run_command(makemkv_command, dvd_folder)


def get_mediainfo(video_path: Path):
    print(f"Getting MediaInfo for {video_path.absolute()}")
    output_file = (
        Path.home() / "Desktop" / f"{video_path.parent.name} - {video_path.name}.txt"
    )

    mediainfo_command = ["mediainfo", "--Output=TXT", str(video_path)]

    result = subprocess.run(mediainfo_command, capture_output=True, text=True).stdout

    cleaned_result = result.replace("Lance\\", "")

    with open(output_file, "w") as f:
        f.write(cleaned_result)

    pyperclip.copy(cleaned_result)

    print("MediaInfo successfully created.")


def extract_audio_commentary(video_files):
    for file in video_files:
        try:
            probe = ffmpeg.probe(str(file))
            audio_streams = [
                stream for stream in probe["streams"] if stream["codec_type"] == "audio"
            ]
        except ffmpeg.Error as e:
            print(f"Failed to probe {file}: {e.stderr.decode()}")
            continue

        if len(audio_streams) > 1:
            output_file = file.with_name(f"{file.stem} Audio Commentary.flac")
            try:
                (
                    ffmpeg.input(str(file))
                    .output(str(output_file), map="0:a:1", acodec="flac")
                    .run()
                )
                print(f"Extracted audio commentary from {file.name}.")
            except ffmpeg.Error as e:
                print(
                    f"Failed to extract audio commentary from {file}: {e.stderr.decode()}"
                )

    for flac_file in Path(".").glob("*.flac"):
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


def get_video_resolution(filepath: Path):
    try:
        probe = ffmpeg.probe(str(filepath))
        video_streams = [
            stream for stream in probe["streams"] if stream["codec_type"] == "video"
        ]
        width = int(video_streams[0]["width"])
        height = int(video_streams[0]["height"])
        return {"Width": width, "Height": height}

    except (ffmpeg.Error, IndexError, KeyError, ValueError) as e:
        print(f"Error processing file: {filepath}. Error: {e}")

    return {}


def calculate_mb_per_minute(file: Path):
    try:
        probe = ffmpeg.probe(str(file))
        format_info = probe.get("format", {})
        duration = float(format_info.get("duration", 0))
        size = float(format_info.get("size", 0))
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
    output_file_path = Path.home() / "Desktop" / "video_files_info.txt"

    with open(output_file_path, "w") as f:
        for i, (filename, mb_per_minute, size, duration) in enumerate(sorted_data, 1):
            f.write(
                f"{i}. Name: {filename}\n"
                f"MB/Minute: {mb_per_minute:.2f}\n"
                f"Size: {size / (1024 * 1024):.2f} MB\n"
                f"Duration: {duration / 60:.2f} minutes\n\n"
            )

    print(f"Output saved as '{output_file_path}'.")


def main():
    parser = argparse.ArgumentParser(description="Video processing tool.")
    parser.add_argument(
        "method",
        help=(
            "Methods: RemuxDisc, ExtractChapters, BatchCompression, "
            "ExtractAudioCommentary, PrintVideoResolution, "
            "CalculateMBPerMinute, CreateImages, GetMediaInfo"
        ),
    )
    parser.add_argument("path", type=Path, help="File or folder path to process.")
    parser.add_argument(
        "--get-mediainfo", action="store_true", help="Flag for MediaInfo in RemuxDisc"
    )

    args = parser.parse_args()

    method = args.method
    path = args.path
    video_files = (
        [path]
        if path.is_file()
        else [f for f in path.rglob("*") if f.suffix.lower() in VIDEO_EXTENSIONS]
    )

    match method:
        case "RemuxDisc":
            remux_disc(path, args.get_mediainfo)
        case "BatchCompression":
            batch_compression(path)
        case "ExtractChapters":
            extract_chapters(video_files)
        case "ExtractAudioCommentary":
            extract_audio_commentary(video_files)
        case "PrintVideoResolution":
            print_video_resolution(video_files)
        case "CalculateMBPerMinute":
            calculate_mb_for_directory(video_files)
        case "CreateImages":
            [extract_images(f) for f in video_files]
        case "GetMediaInfo":
            [get_mediainfo(f) for f in video_files]
        case _:
            print(f"Method '{method}' not recognized.")


if __name__ == "__main__":
    main()
