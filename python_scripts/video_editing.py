import sys
import subprocess
import ffmpeg
import logging
from operator import itemgetter
from pathlib import Path

logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(levelname)s - %(message)s')

video_extensions = [".mp4", ".mkv", ".ts", ".avi"]


def extract_chapters(video_files):
    for video_file in video_files:
        try:
            probe = ffmpeg.probe(str(video_file), show_chapters=None)
            chapters = probe.get('chapters', [])
        except ffmpeg.Error as e:
            logging.error(f"Failed to probe {video_file}: {e.stderr.decode()}")
            continue

        if len(chapters) <= 1:
            logging.info(f"No chapters found in {video_file.name}.")
            continue

        parent_directory = video_file.parent
        
        for chapter_index, chapter in enumerate(chapters, 1):
            formatted_index = f"{chapter_index:02}"
            output_file_name = parent_directory / f"{parent_directory.name} - Chapter {formatted_index}{video_file.suffix}"
            try:
                (
                    ffmpeg
                    .input(str(video_file), ss=chapter['start_time'], to=chapter['end_time'])
                    .output(str(output_file_name), c='copy', avoid_negative_ts='make_zero')
                    .run()
                )
                logging.info(f"Extracted chapter {formatted_index} from {video_file.name}.")
            except ffmpeg.Error as e:
                logging.error(f"Failed to extract chapter {formatted_index} from {video_file}: {e.stderr.decode()}")


def batch_compression(path):
    for file in path.rglob("*.mkv"):
        output_file_path = file.with_suffix(".mp4")
        try:
            (
                ffmpeg
                .input(str(file))
                .output(str(output_file_path), preset='medium')
                .run()
            )
            file.unlink()
            logging.info(f"Successfully converted {file}.")
        except ffmpeg.Error as e:
            logging.error(f"Failed to convert {file}: {e.stderr.decode()}")


def remux_dvd(path):
    directories = [path] + [d for d in path.rglob("*") if d.is_dir() and "BACKUP" not in d.name]
    non_remuxable = []

    for dvd_path in directories:
        remuxable = [f for f in dvd_path.rglob("*") if f.name in ('VIDEO_TS.IFO', 'index.bdmv')]
        if remuxable:
            for file in remuxable:
                logging.info(f"Converting file: {file} in {dvd_path}")
                convert_dvd_to_mkv(file, dvd_path)
        else:
            logging.info(f"No remuxable files found in {dvd_path}.")
            non_remuxable.append(dvd_path)

    if non_remuxable:
        logging.info("Folders that couldn't be remuxed:")
        for folder in non_remuxable:
            logging.info(folder)


def convert_dvd_to_mkv(file, dvd_folder):
    output_path = dvd_folder
    output_path.mkdir(exist_ok=True)

    command = [r"C:\Program Files (x86)\MakeMKV\makemkvcon64.exe", "mkv", f'file:{file}', "all", str(dvd_folder), "--minlength=180"]

    result = subprocess.run(command, capture_output=True, text=True)
    if result.returncode == 0:
        logging.info(f"Successfully converted {file}.")
    else:
        logging.error(f"Failed to convert {file}: {result.stderr}")


def extract_audio_commentary(video_files):
    for file in video_files:
        try:
            probe = ffmpeg.probe(str(file))
            audio_streams = [stream for stream in probe['streams'] if stream['codec_type'] == 'audio']
        except ffmpeg.Error as e:
            logging.error(f"Failed to probe {file}: {e.stderr.decode()}")
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
                logging.info(f"Extracted audio commentary from {file.name}.")
            except ffmpeg.Error as e:
                logging.error(f"Failed to extract audio commentary from {file}: {e.stderr.decode()}")

    for flac_file in Path('.').glob('*.flac'):
        logging.info(flac_file.name)


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

    logging.info("Files with a resolution of 1920x1080:")
    for name in files_1920_1080:
        logging.info(name)

    logging.info("\nFiles with resolution below 1920x1080:")
    for name, res in files_below_1920_1080:
        logging.info(f"{name}, Resolution: {res['Width']}x{res['Height']}")

    logging.info("\nFiles with unresolved resolution:")
    for name in files_unresolved_resolution:
        logging.info(name)


def get_video_resolution(filepath):
    try:
        probe = ffmpeg.probe(str(filepath))
        video_streams = [stream for stream in probe['streams'] if stream['codec_type'] == 'video']
        width = int(video_streams[0]['width'])
        height = int(video_streams[0]['height'])
        return {"Width": width, "Height": height}
    
    except (ffmpeg.Error, IndexError, KeyError, ValueError) as e:
        logging.error(f"Error processing file: {filepath}. Error: {e}")
    
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
        logging.error(f"Error processing file: {file}. Error: {e.stderr.decode()}")
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

    logging.info(f"Output saved as '{output_file_path}'.")


def main():
    if len(sys.argv) != 3:
        logging.error("Usage: script.py <Method> <FolderPath>")
        sys.exit(1)

    method, folder = sys.argv[1], Path(sys.argv[2])
    video_files = [file for file in folder.rglob("*") if file.suffix in video_extensions]

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
        logging.error("Invalid method specified.")


if __name__ == "__main__":
    main()