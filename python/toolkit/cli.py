import argparse
import json
import logging
import sys
import uuid
from datetime import datetime, timezone
from pathlib import Path
from rich.console import Console
from rich.logging import RichHandler

console = Console()

REPO_ROOT = Path(__file__).parent.parent.parent
LOG_DIR = REPO_ROOT / "logs"
LOG_DIR.mkdir(exist_ok=True)


class JsonFileHandler(logging.Handler):
    def __init__(self, service_name):
        super().__init__()
        self.log_path = LOG_DIR / f"{service_name}.json"
        self.lock_path = LOG_DIR / f"{service_name}.lock"
        self.session_id = uuid.uuid4().hex[:8]
        self.service_name = service_name
        self.started_at = datetime.now(timezone.utc).isoformat()
        self.session_closed = False
        self._handle_stale_lock()
        self._write_lock()
        self._append_entry(
            {
                "timestamp": self.started_at,
                "level": "INFO",
                "service": self.service_name,
                "sessionId": self.session_id,
                "operation": "SessionStart",
                "message": "Session started",
            }
        )

    def emit(self, record):
        log_entry = {
            "timestamp": datetime.now(timezone.utc).isoformat(),
            "level": record.levelname,
            "service": self.service_name,
            "sessionId": self.session_id,
            "operation": getattr(record, "operation", "log"),
            "message": record.getMessage(),
            "context": {
                "module": record.module,
                "function": record.funcName,
                "line": record.lineno,
            },
        }

        if hasattr(record, "data"):
            log_entry["data"] = record.data

        self._append_entry(log_entry)

    def close(self):
        if not self.session_closed:
            self._append_entry(
                {
                    "timestamp": datetime.now(timezone.utc).isoformat(),
                    "level": "INFO",
                    "service": self.service_name,
                    "sessionId": self.session_id,
                    "operation": "SessionEnd",
                    "message": "Session ended",
                }
            )
            self.session_closed = True
        self._delete_lock()
        super().close()

    def _handle_stale_lock(self):
        if not self.lock_path.exists():
            return
        raw = self.lock_path.read_text(encoding="utf-8").strip()
        parts = raw.split("|") if raw else []
        crashed_session = parts[0] if len(parts) > 0 else "unknown"
        start_time = parts[1] if len(parts) > 1 else "unknown"
        self._append_entry(
            {
                "timestamp": datetime.now(timezone.utc).isoformat(),
                "level": "ERROR",
                "service": self.service_name,
                "sessionId": crashed_session,
                "operation": "SessionCrashed",
                "message": "Previous session did not shut down cleanly",
                "data": {
                    "originalStart": start_time,
                },
            }
        )
        self._delete_lock()

    def _write_lock(self):
        self.lock_path.write_text(
            f"{self.session_id}|{self.started_at}", encoding="utf-8"
        )

    def _delete_lock(self):
        if self.lock_path.exists():
            self.lock_path.unlink()

    def _append_entry(self, entry):
        entries = self._load_entries()
        entries.append(entry)
        temp_path = self.log_path.with_name(self.log_path.name + ".tmp")
        temp_path.write_text(json.dumps(entries, indent=2), encoding="utf-8")
        temp_path.replace(self.log_path)

    def _load_entries(self):
        if not self.log_path.exists():
            return []
        content = self.log_path.read_text(encoding="utf-8").strip()
        if not content:
            return []
        data = json.loads(content)
        return data if isinstance(data, list) else []


def configure_logging(service_name="toolkit"):
    logger = logging.getLogger("toolkit")
    logger.setLevel(logging.INFO)

    for handler in logger.handlers[:]:
        logger.removeHandler(handler)

    logger.addHandler(
        RichHandler(console=console, rich_tracebacks=True, show_time=False)
    )
    logger.addHandler(JsonFileHandler(service_name))

    return logger


logger = configure_logging()


def get_logger(service_name="toolkit"):
    return configure_logging(service_name)


def cmd_audio_convert(args):
    from toolkit.audio import convert_audio, process_sacd_directory, prepare_directory

    resolved = args.directory.resolve()
    if not resolved.exists():
        logger.error(f"Directory not found: {resolved}")
        sys.exit(1)

    prepared = prepare_directory(resolved)

    match args.mode:
        case "convert":
            convert_audio(1, prepared, args.format)
        case "extract":
            process_sacd_directory(prepared, args.format)

    logger.info("Processing completed")


def cmd_audio_rename(args):
    from toolkit.filesystem import rename_file_red

    rename_file_red(args.directory.resolve())


def cmd_audio_art_report(args):
    from toolkit.audio import calculate_image_size

    calculate_image_size(args.directory.resolve())


def cmd_video_remux(args):
    from toolkit.video import remux_disc

    remux_disc(args.path.resolve(), not args.skip_mediainfo)


def cmd_video_compress(args):
    from toolkit.video import batch_compression

    batch_compression(args.directory.resolve())


def cmd_video_chapters(args):
    from toolkit.video import extract_chapters, VIDEO_EXTENSIONS

    resolved = args.path.resolve()
    video_files = (
        [resolved]
        if resolved.is_file()
        else [f for f in resolved.rglob("*") if f.suffix.lower() in VIDEO_EXTENSIONS]
    )
    extract_chapters(video_files)


def cmd_video_resolutions(args):
    from toolkit.video import print_video_resolution, VIDEO_EXTENSIONS

    resolved = args.path.resolve()
    video_files = (
        [resolved]
        if resolved.is_file()
        else [f for f in resolved.rglob("*") if f.suffix.lower() in VIDEO_EXTENSIONS]
    )
    print_video_resolution(video_files)


def cmd_video_gif(args):
    from toolkit.video import create_gif_optimized

    create_gif_optimized(
        args.input.resolve(),
        args.start,
        args.duration,
        args.max_size,
        args.output.resolve(),
    )


def cmd_video_thumbnails(args):
    from toolkit.video import extract_images

    extract_images(args.path.resolve())


def cmd_filesystem_tree(args):
    from toolkit.filesystem import list_directories, list_files_and_directories

    resolved = args.directory.resolve()
    sort_by_name = args.sort == "name"

    if args.include_files:
        list_files_and_directories(resolved, sort_by_name)
    else:
        list_directories(resolved, "1" if sort_by_name else "0")


def cmd_filesystem_torrents(args):
    from toolkit.filesystem import make_torrents

    resolved = args.directory.resolve()

    if args.include_subdirectories:
        for entry in (e for e in resolved.iterdir() if e.is_dir()):
            make_torrents(entry)
    else:
        make_torrents(resolved)


def build_parser():
    parser = argparse.ArgumentParser(
        prog="toolkit",
        description="Personal toolkit for audio, video, and file operations.",
    )
    subparsers = parser.add_subparsers(dest="command", required=True)

    audio = subparsers.add_parser("audio", help="Audio conversion and processing tools")
    audio_sub = audio.add_subparsers(dest="audio_command", required=True)

    audio_convert = audio_sub.add_parser(
        "convert", help="Convert audio files to various formats or extract SACD ISOs"
    )
    audio_convert.add_argument(
        "-d",
        "--directory",
        type=Path,
        default=Path("."),
        help="Directory containing audio files",
    )
    audio_convert.add_argument(
        "-m",
        "--mode",
        default="convert",
        choices=["convert", "extract"],
        help="Mode: convert for FLAC or extract for SACD",
    )
    audio_convert.add_argument(
        "-f",
        "--format",
        default="all",
        choices=["24-bit", "flac", "mp3", "all"],
        help="Output format",
    )
    audio_convert.set_defaults(func=cmd_audio_convert)

    audio_rename = audio_sub.add_parser(
        "rename", help="Rename files with excessively long paths"
    )
    audio_rename.add_argument(
        "-d",
        "--directory",
        type=Path,
        default=Path("."),
        help="Directory containing audio files",
    )
    audio_rename.set_defaults(func=cmd_audio_rename)

    audio_art = audio_sub.add_parser(
        "art-report", help="Report embedded artwork sizes in FLAC files"
    )
    audio_art.add_argument(
        "-d",
        "--directory",
        type=Path,
        default=Path("."),
        help="Directory containing FLAC files",
    )
    audio_art.set_defaults(func=cmd_audio_art_report)

    video = subparsers.add_parser("video", help="Video processing and extraction tools")
    video_sub = video.add_subparsers(dest="video_command", required=True)

    video_remux = video_sub.add_parser("remux", help="Remux DVD/Blu-ray discs to MKV")
    video_remux.add_argument(
        "-p", "--path", type=Path, default=Path("."), help="Path to disc folder"
    )
    video_remux.add_argument(
        "--skip-mediainfo", action="store_true", help="Skip MediaInfo generation"
    )
    video_remux.set_defaults(func=cmd_video_remux)

    video_compress = video_sub.add_parser(
        "compress", help="Batch compress MKV files using HandBrake"
    )
    video_compress.add_argument(
        "-d",
        "--directory",
        type=Path,
        default=Path("."),
        help="Directory containing MKV files",
    )
    video_compress.set_defaults(func=cmd_video_compress)

    video_chapters = video_sub.add_parser(
        "chapters", help="Extract chapters from video files"
    )
    video_chapters.add_argument(
        "-p", "--path", type=Path, default=Path("."), help="Video file or directory"
    )
    video_chapters.set_defaults(func=cmd_video_chapters)

    video_resolutions = video_sub.add_parser(
        "resolutions", help="Print resolution information for video files"
    )
    video_resolutions.add_argument(
        "-p", "--path", type=Path, default=Path("."), help="Video file or directory"
    )
    video_resolutions.set_defaults(func=cmd_video_resolutions)

    video_gif = video_sub.add_parser("gif", help="Create optimized GIF from video file")
    video_gif.add_argument(
        "-i", "--input", type=Path, required=True, help="Input video file"
    )
    video_gif.add_argument("-s", "--start", default="00:00", help="Start time (mm:ss)")
    video_gif.add_argument(
        "-d", "--duration", type=int, default=30, help="Duration in seconds"
    )
    video_gif.add_argument(
        "-m", "--max-size", type=int, default=300, help="Maximum GIF size in MiB"
    )
    video_gif.add_argument(
        "-o",
        "--output",
        type=Path,
        default=Path.home() / "Desktop",
        help="Output directory",
    )
    video_gif.set_defaults(func=cmd_video_gif)

    video_thumbnails = video_sub.add_parser(
        "thumbnails", help="Extract thumbnail grid and full-size images from video"
    )
    video_thumbnails.add_argument(
        "-p", "--path", type=Path, required=True, help="Video file"
    )
    video_thumbnails.set_defaults(func=cmd_video_thumbnails)

    filesystem = subparsers.add_parser(
        "filesystem", help="Filesystem operations and torrent creation"
    )
    filesystem_sub = filesystem.add_subparsers(dest="filesystem_command", required=True)

    filesystem_tree = filesystem_sub.add_parser(
        "tree", help="List directory tree with sizes"
    )
    filesystem_tree.add_argument(
        "-d", "--directory", type=Path, default=Path("."), help="Directory to list"
    )
    filesystem_tree.add_argument(
        "-s",
        "--sort",
        default="size",
        choices=["size", "name"],
        help="Sort by: size or name",
    )
    filesystem_tree.add_argument(
        "-f", "--include-files", action="store_true", help="Include files in listing"
    )
    filesystem_tree.set_defaults(func=cmd_filesystem_tree)

    filesystem_torrents = filesystem_sub.add_parser(
        "torrents", help="Create RED and OPS torrents for directory"
    )
    filesystem_torrents.add_argument(
        "-d",
        "--directory",
        type=Path,
        default=Path("."),
        help="Directory to create torrent for",
    )
    filesystem_torrents.add_argument(
        "--include-subdirectories",
        action="store_true",
        help="Create torrents for each subdirectory",
    )
    filesystem_torrents.set_defaults(func=cmd_filesystem_torrents)

    return parser


def main():
    parser = build_parser()
    args = parser.parse_args()
    args.func(args)


if __name__ == "__main__":
    main()
