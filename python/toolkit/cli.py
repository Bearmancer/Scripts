# pyright: reportMissingTypeStubs=false, reportUnknownMemberType=false, reportUnknownVariableType=false, reportUnknownArgumentType=false, reportAny=false
from pathlib import Path
from typing import Annotated

import typer
from rich.console import Console

from toolkit.logging_config import get_logger

# region App Setup

app = typer.Typer(
    name="toolkit",
    help="Personal toolkit for audio, video, and file operations.",
    no_args_is_help=True,
)
audio_app = typer.Typer(
    help="Audio conversion and processing tools", no_args_is_help=True
)
video_app = typer.Typer(
    help="Video processing and extraction tools", no_args_is_help=True
)
filesystem_app = typer.Typer(
    help="Filesystem operations and torrent creation", no_args_is_help=True
)

app.add_typer(audio_app, name="audio")
app.add_typer(video_app, name="video")
app.add_typer(filesystem_app, name="filesystem")

console = Console()
logger = get_logger()

# endregion

# region Audio Commands


@audio_app.command("convert")
def audio_convert(
    directory: Annotated[
        Path, typer.Option("-d", "--directory", help="Directory containing audio files")
    ] = Path("."),
    mode: Annotated[
        str,
        typer.Option("-m", "--mode", help="Mode: convert for FLAC or extract for SACD"),
    ] = "convert",
    format: Annotated[
        str, typer.Option("-f", "--format", help="Output format")
    ] = "all",
) -> None:
    """Convert audio files to various formats or extract SACD ISOs."""
    from toolkit.audio import convert_audio, prepare_directory, process_sacd_directory

    resolved = directory.resolve()
    if not resolved.exists():
        logger.error(f"Directory not found: {resolved}")
        raise typer.Exit(code=1)

    prepared = prepare_directory(resolved)

    match mode:
        case "convert":
            convert_audio(1, prepared, format)
        case "extract":
            process_sacd_directory(prepared, format)
        case _:
            raise ValueError(f"Unknown mode: {mode}")

    logger.info("Processing completed")


@audio_app.command("rename")
def audio_rename(
    directory: Annotated[
        Path, typer.Option("-d", "--directory", help="Directory containing audio files")
    ] = Path("."),
) -> None:
    """Rename files with excessively long paths."""
    from toolkit.audio import rename_file_red

    rename_file_red(directory.resolve())


@audio_app.command("art-report")
def audio_art_report(
    directory: Annotated[
        Path, typer.Option("-d", "--directory", help="Directory containing FLAC files")
    ] = Path("."),
) -> None:
    """Report embedded artwork sizes in FLAC files."""
    from toolkit.audio import calculate_image_size

    calculate_image_size(directory.resolve())


# endregion

# region Video Commands


@video_app.command("remux")
def video_remux(
    path: Annotated[
        Path, typer.Option("-p", "--path", help="Path to disc folder")
    ] = Path("."),
    skip_mediainfo: Annotated[
        bool, typer.Option("--skip-mediainfo", help="Skip MediaInfo generation")
    ] = False,
) -> None:
    """Remux DVD/Blu-ray discs to MKV."""
    from toolkit.video import remux_disc

    remux_disc(path.resolve(), not skip_mediainfo)


@video_app.command("compress")
def video_compress(
    directory: Annotated[
        Path, typer.Option("-d", "--directory", help="Directory containing MKV files")
    ] = Path("."),
) -> None:
    """Batch compress MKV files using HandBrake."""
    from toolkit.video import batch_compression

    batch_compression(directory.resolve())


@video_app.command("chapters")
def video_chapters(
    path: Annotated[
        Path, typer.Option("-p", "--path", help="Video file or directory")
    ] = Path("."),
) -> None:
    """Extract chapters from video files."""
    from toolkit.video import VIDEO_EXTENSIONS, extract_chapters

    resolved = path.resolve()
    video_files = (
        [resolved]
        if resolved.is_file()
        else [f for f in resolved.rglob("*") if f.suffix.lower() in VIDEO_EXTENSIONS]
    )
    extract_chapters(video_files)


@video_app.command("resolutions")
def video_resolutions(
    path: Annotated[
        Path, typer.Option("-p", "--path", help="Video file or directory")
    ] = Path("."),
) -> None:
    """Print resolution information for video files."""
    from toolkit.video import VIDEO_EXTENSIONS, print_video_resolution

    resolved = path.resolve()
    video_files = (
        [resolved]
        if resolved.is_file()
        else [f for f in resolved.rglob("*") if f.suffix.lower() in VIDEO_EXTENSIONS]
    )
    print_video_resolution(video_files)


@video_app.command("gif")
def video_gif(
    input: Annotated[Path, typer.Option("-i", "--input", help="Input video file")],
    start: Annotated[
        str, typer.Option("-s", "--start", help="Start time (mm:ss)")
    ] = "00:00",
    duration: Annotated[
        int, typer.Option("-d", "--duration", help="Duration in seconds")
    ] = 30,
    max_size: Annotated[
        int, typer.Option("-m", "--max-size", help="Maximum GIF size in MiB")
    ] = 300,
    output: Annotated[
        Path, typer.Option("-o", "--output", help="Output directory")
    ] = Path.home()
    / "Desktop",
) -> None:
    """Create optimized GIF from video file."""
    from toolkit.video import create_gif_optimized

    create_gif_optimized(input.resolve(), start, duration, max_size, output.resolve())


@video_app.command("thumbnails")
def video_thumbnails(
    path: Annotated[Path, typer.Option("-p", "--path", help="Video file")],
) -> None:
    """Extract thumbnail grid and full-size images from video."""
    from toolkit.video import extract_images

    extract_images(path.resolve())


# endregion

# region Filesystem Commands


@filesystem_app.command("tree")
def filesystem_tree(
    directory: Annotated[
        Path, typer.Option("-d", "--directory", help="Directory to list")
    ] = Path("."),
    sort: Annotated[
        str, typer.Option("-s", "--sort", help="Sort by: size or name")
    ] = "size",
    include_files: Annotated[
        bool, typer.Option("-f", "--include-files", help="Include files in listing")
    ] = False,
) -> None:
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
    directory: Annotated[
        Path, typer.Option("-d", "--directory", help="Directory to create torrent for")
    ] = Path("."),
    include_subdirectories: Annotated[
        bool,
        typer.Option(
            "--include-subdirectories", help="Create torrents for each subdirectory"
        ),
    ] = False,
) -> None:
    """Create RED and OPS torrents for directory."""
    from toolkit.filesystem import make_torrents

    resolved = directory.resolve()

    if include_subdirectories:
        for entry in (e for e in resolved.iterdir() if e.is_dir()):
            make_torrents(entry)
    else:
        make_torrents(resolved)


# endregion

# region Last.fm Commands


@app.command("lastfm")
def lastfm_update() -> None:
    """Update Last.fm scrobbles to Google Sheets."""
    from toolkit.lastfm import update_scrobbles

    update_scrobbles()


# endregion

# region Entry Point


def main() -> None:
    app()


if __name__ == "__main__":
    main()

# endregion
