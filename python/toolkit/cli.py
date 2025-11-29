import json
import logging
import uuid
from datetime import datetime, timezone
from pathlib import Path
from rich.console import Console
from rich.logging import RichHandler
import typer

console = Console()

REPO_ROOT = Path(__file__).parent.parent.parent
LOG_DIR = REPO_ROOT / "logs"
LOG_DIR.mkdir(exist_ok=True)


class JsonFileHandler(logging.Handler):
    def __init__(self, service_name):
        super().__init__()
        self.log_path = LOG_DIR / f"{service_name}.jsonl"
        self.session_id = uuid.uuid4().hex[:8]
        self.service_name = service_name

    def emit(self, record):
        log_entry = {
            "timestamp": datetime.now(timezone.utc).isoformat(),
            "level": record.levelname,
            "service": self.service_name,
            "sessionId": self.session_id,
            "operation": getattr(record, "operation", "log"),
            "message": record.getMessage(),
        }

        if hasattr(record, "data"):
            log_entry["data"] = record.data

        with open(self.log_path, "a", encoding="utf-8") as f:
            f.write(json.dumps(log_entry) + "\n")


def configure_logging(service_name="toolkit"):
    logger = logging.getLogger("toolkit")
    logger.setLevel(logging.INFO)

    for handler in logger.handlers[:]:
        logger.removeHandler(handler)

    logger.addHandler(RichHandler(console=console, rich_tracebacks=True, show_time=False))
    logger.addHandler(JsonFileHandler(service_name))

    return logger


logger = configure_logging()

app = typer.Typer(
    name="toolkit",
    help="Personal toolkit for audio, video, and file operations.",
    no_args_is_help=True,
)

audio_app = typer.Typer(help="Audio conversion and processing tools.")
video_app = typer.Typer(help="Video processing and extraction tools.")
filesystem_app = typer.Typer(help="Filesystem operations and torrent creation.")

app.add_typer(audio_app, name="audio")
app.add_typer(video_app, name="video")
app.add_typer(filesystem_app, name="filesystem")


@audio_app.command("convert")
def audio_convert(
    directory=typer.Option(
        Path("."), "--directory", "-d", help="Directory containing audio files"
    ),
    mode=typer.Option(
        "convert", "--mode", "-m", help="Mode: 'convert' for FLAC or 'extract' for SACD"
    ),
    format=typer.Option(
        "all", "--format", "-f", help="Output format: 24-bit, flac, mp3, all"
    ),
):
    """Convert audio files to various formats or extract SACD ISOs."""
    from toolkit.audio import convert_audio, process_sacd_directory, prepare_directory

    resolved = directory.resolve()
    if not resolved.exists():
        logger.error(f"Directory not found: {resolved}")
        raise typer.Exit(1)

    prepared = prepare_directory(resolved)

    match mode:
        case "convert":
            convert_audio(1, prepared, format)
        case "extract":
            process_sacd_directory(prepared, format)

    logger.info("Processing completed")


@audio_app.command("rename")
def audio_rename(
    directory=typer.Option(
        Path("."), "--directory", "-d", help="Directory containing audio files"
    ),
):
    """Rename files with excessively long paths."""
    from toolkit.audio import rename_file_red

    rename_file_red(directory.resolve())


@audio_app.command("art-report")
def audio_art_report(
    directory=typer.Option(
        Path("."), "--directory", "-d", help="Directory containing FLAC files"
    ),
):
    """Report embedded artwork sizes in FLAC files."""
    from toolkit.audio import calculate_image_size

    calculate_image_size(directory.resolve())


@video_app.command("remux")
def video_remux(
    path=typer.Option(Path("."), "--path", "-p", help="Path to disc folder"),
    skip_mediainfo=typer.Option(
        False, "--skip-mediainfo", help="Skip MediaInfo generation"
    ),
):
    """Remux DVD/Blu-ray discs to MKV."""
    from toolkit.video import remux_disc

    remux_disc(path.resolve(), not skip_mediainfo)


@video_app.command("compress")
def video_compress(
    directory=typer.Option(
        Path("."), "--directory", "-d", help="Directory containing MKV files"
    ),
):
    """Batch compress MKV files using HandBrake."""
    from toolkit.video import batch_compression

    batch_compression(directory.resolve())


@video_app.command("chapters")
def video_chapters(
    path=typer.Option(Path("."), "--path", "-p", help="Video file or directory"),
):
    """Extract chapters from video files."""
    from toolkit.video import extract_chapters, VIDEO_EXTENSIONS

    resolved = path.resolve()
    video_files = (
        [resolved]
        if resolved.is_file()
        else [f for f in resolved.rglob("*") if f.suffix.lower() in VIDEO_EXTENSIONS]
    )
    extract_chapters(video_files)


@video_app.command("resolutions")
def video_resolutions(
    path=typer.Option(Path("."), "--path", "-p", help="Video file or directory"),
):
    """Print resolution information for video files."""
    from toolkit.video import print_video_resolution, VIDEO_EXTENSIONS

    resolved = path.resolve()
    video_files = (
        [resolved]
        if resolved.is_file()
        else [f for f in resolved.rglob("*") if f.suffix.lower() in VIDEO_EXTENSIONS]
    )
    print_video_resolution(video_files)


@video_app.command("gif")
def video_gif(
    input_path=typer.Option(..., "--input", "-i", help="Input video file"),
    start=typer.Option("00:00", "--start", "-s", help="Start time (mm:ss)"),
    duration=typer.Option(30, "--duration", "-d", help="Duration in seconds"),
    max_size=typer.Option(300, "--max-size", "-m", help="Maximum GIF size in MiB"),
    output_dir=typer.Option(
        Path.home() / "Desktop", "--output", "-o", help="Output directory"
    ),
):
    """Create optimized GIF from video file."""
    from toolkit.video import create_gif_optimized

    create_gif_optimized(
        input_path.resolve(), start, duration, max_size, output_dir.resolve()
    )


@video_app.command("thumbnails")
def video_thumbnails(
    path=typer.Option(..., "--path", "-p", help="Video file"),
):
    """Extract thumbnail grid and full-size images from video."""
    from toolkit.video import extract_images

    extract_images(path.resolve())


@filesystem_app.command("tree")
def filesystem_tree(
    directory=typer.Option(Path("."), "--directory", "-d", help="Directory to list"),
    sort=typer.Option("size", "--sort", "-s", help="Sort by: size or name"),
    include_files=typer.Option(
        False, "--include-files", "-f", help="Include files in listing"
    ),
):
    """List directory tree with sizes."""
    from toolkit.filesystem import list_directories, list_files_and_directories

    resolved = directory.resolve()
    sort_by_name = sort == "name"

    if include_files:
        list_files_and_directories(resolved, sort_by_name)
    else:
        list_directories(resolved, "1" if sort_by_name else "0")


@filesystem_app.command("torrents")
def filesystem_torrents(
    directory=typer.Option(
        Path("."), "--directory", "-d", help="Directory to create torrent for"
    ),
    include_subdirectories=typer.Option(
        False, "--include-subdirectories", help="Create torrents for each subdirectory"
    ),
):
    """Create RED and OPS torrents for directory."""
    from toolkit.filesystem import make_torrents

    resolved = directory.resolve()

    if include_subdirectories:
        for entry in (e for e in resolved.iterdir() if e.is_dir()):
            make_torrents(entry)
    else:
        make_torrents(resolved)


def main():
    app()


def get_logger(service_name="toolkit"):
    return configure_logging(service_name)


if __name__ == "__main__":
    main()
