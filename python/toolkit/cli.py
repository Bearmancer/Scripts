#!/usr/bin/env python3
# PYTHON_ARGCOMPLETE_OK
"""Personal toolkit for audio, video, and file operations.

This CLI provides unified access to common media and file operations:

Commands:
    audio       - Convert audio files, extract SACD ISOs, rename long paths
    video       - Remux discs, compress MKV, extract chapters/thumbnails, create GIFs
    filesystem  - Directory listings, torrent creation (RED/OPS)
    lastfm      - Sync Last.fm scrobbles to Google Sheets

Installation:
    pip install -e .              # Editable install with entry point
    
Usage:
    toolkit <command> [subcommand] [options]
    toolkit audio convert -d /path/to/flacs -f wav
    toolkit video remux -p /path/to/disc
    toolkit lastfm

Shell Completion (carapace):
    carapace toolkit bash | source  # Add to .bashrc
    carapace toolkit pwsh | iex     # Add to $PROFILE
"""

from __future__ import annotations

import argparse
from pathlib import Path
from typing import TYPE_CHECKING, Any, cast

import argcomplete
from argcomplete.completers import DirectoriesCompleter, FilesCompleter
from rich.console import Console

from toolkit.logging_config import get_logger

if TYPE_CHECKING:
    pass

console = Console()
logger = get_logger()

path_completer = FilesCompleter()
dir_completer = DirectoriesCompleter()


def set_completer(action: argparse.Action, completer: object) -> None:
    """Set argcomplete completer on an action (argcomplete adds this attribute at runtime)."""
    cast(Any, action).completer = completer


def cmd_audio_convert(args: argparse.Namespace) -> None:
    """Convert audio files to various formats or extract SACD ISOs."""
    from toolkit.audio import convert_audio, prepare_directory, process_sacd_directory

    resolved = args.directory.resolve()
    if not resolved.exists():
        logger.error(f"Directory not found: {resolved}")
        raise SystemExit(1)

    prepared = prepare_directory(resolved)

    match args.mode:
        case "convert":
            convert_audio(1, prepared, args.format)
        case "extract":
            process_sacd_directory(prepared, args.format)
        case _:
            raise ValueError(f"Unknown mode: {args.mode}")

    logger.info("Processing completed")


def cmd_audio_rename(args: argparse.Namespace) -> None:
    """Rename files with excessively long paths."""
    from toolkit.audio import rename_file_red
    rename_file_red(args.directory.resolve())


def cmd_audio_art_report(args: argparse.Namespace) -> None:
    """Report embedded artwork sizes in FLAC files."""
    from toolkit.audio import calculate_image_size
    calculate_image_size(args.directory.resolve())


def cmd_video_remux(args: argparse.Namespace) -> None:
    """Remux DVD/Blu-ray discs to MKV."""
    from toolkit.video import remux_disc
    remux_disc(args.path.resolve(), not args.skip_mediainfo)


def cmd_video_compress(args: argparse.Namespace) -> None:
    """Batch compress MKV files using HandBrake."""
    from toolkit.video import batch_compression
    batch_compression(args.directory.resolve())


def cmd_video_chapters(args: argparse.Namespace) -> None:
    """Extract chapters from video files."""
    from toolkit.video import VIDEO_EXTENSIONS, extract_chapters

    resolved = args.path.resolve()
    video_files = (
        [resolved]
        if resolved.is_file()
        else [f for f in resolved.rglob("*") if f.suffix.lower() in VIDEO_EXTENSIONS]
    )
    extract_chapters(video_files)


def cmd_video_resolutions(args: argparse.Namespace) -> None:
    """Print resolution information for video files."""
    from toolkit.video import VIDEO_EXTENSIONS, print_video_resolution

    resolved = args.path.resolve()
    video_files = (
        [resolved]
        if resolved.is_file()
        else [f for f in resolved.rglob("*") if f.suffix.lower() in VIDEO_EXTENSIONS]
    )
    print_video_resolution(video_files)


def cmd_video_gif(args: argparse.Namespace) -> None:
    """Create optimized GIF from video file."""
    from toolkit.video import create_gif_optimized
    create_gif_optimized(
        args.input.resolve(),
        args.start,
        args.duration,
        args.max_size,
        args.output.resolve(),
    )


def cmd_video_thumbnails(args: argparse.Namespace) -> None:
    """Extract thumbnail grid and full-size images from video."""
    from toolkit.video import extract_images
    extract_images(args.path.resolve())


def cmd_filesystem_tree(args: argparse.Namespace) -> None:
    """List directory tree with sizes."""
    from toolkit.filesystem import list_directories, list_files_and_directories

    resolved = args.directory.resolve()
    sort_by_name = args.sort == "name"

    if args.include_files:
        list_files_and_directories(resolved, sort_by_name)
    else:
        list_directories(resolved, "1" if sort_by_name else "0")


def cmd_filesystem_torrents(args: argparse.Namespace) -> None:
    """Create RED and OPS torrents for directory."""
    from toolkit.filesystem import make_torrents

    resolved = args.directory.resolve()

    if args.include_subdirectories:
        for entry in (e for e in resolved.iterdir() if e.is_dir()):
            make_torrents(entry)
    else:
        make_torrents(resolved)


def cmd_lastfm(args: argparse.Namespace) -> None:
    """Update Last.fm scrobbles to Google Sheets."""
    from toolkit.lastfm import update_scrobbles
    update_scrobbles()


def build_parser() -> argparse.ArgumentParser:
    """Build and return the argument parser with all subcommands."""
    parser = argparse.ArgumentParser(
        prog="toolkit",
        description="Personal toolkit for audio, video, and file operations.",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  toolkit audio convert -d /music -m convert -f flac   Convert audio files
  toolkit audio convert -d /sacd -m extract            Extract SACD ISOs
  toolkit video remux -p /disc/BDMV                    Remux Blu-ray to MKV
  toolkit video gif -i movie.mkv -s 1:23 -d 10         Create 10s GIF from 1:23
  toolkit filesystem tree -d /media -s size            List dirs by size
  toolkit filesystem torrents -d /album                Create RED/OPS torrents
  toolkit lastfm                                       Sync scrobbles to Sheets

Installation:
  pip install -e C:\\Users\\Lance\\Dev\\Scripts\\python   # Installs 'toolkit' command

Shell Completion (add to profile):
  carapace toolkit pwsh | Invoke-Expression            # PowerShell
  eval "$(carapace toolkit bash)"                      # Bash
""",
    )
    subparsers = parser.add_subparsers(dest="command", help="Available commands")

    audio = subparsers.add_parser("audio", help="Audio conversion and processing tools")
    audio_sub = audio.add_subparsers(dest="audio_command", help="Audio subcommands")

    convert = audio_sub.add_parser("convert", help="Convert audio files to various formats")
    set_completer(
        convert.add_argument(
            "-d", "--directory", type=Path, default=Path("."),
            help="Directory containing audio files"
        ),
        dir_completer,
    )
    convert.add_argument("-m", "--mode", choices=["convert", "extract"], default="convert",
                         help="Mode: convert for FLAC or extract for SACD")
    convert.add_argument("-f", "--format", default="all", help="Output format")
    convert.set_defaults(func=cmd_audio_convert)

    rename = audio_sub.add_parser("rename", help="Rename files with excessively long paths")
    set_completer(
        rename.add_argument(
            "-d", "--directory", type=Path, default=Path("."),
            help="Directory containing audio files"
        ),
        dir_completer,
    )
    rename.set_defaults(func=cmd_audio_rename)

    art_report = audio_sub.add_parser("art-report", help="Report embedded artwork sizes in FLAC files")
    set_completer(
        art_report.add_argument(
            "-d", "--directory", type=Path, default=Path("."),
            help="Directory containing FLAC files"
        ),
        dir_completer,
    )
    art_report.set_defaults(func=cmd_audio_art_report)

    video = subparsers.add_parser("video", help="Video processing and extraction tools")
    video_sub = video.add_subparsers(dest="video_command", help="Video subcommands")

    remux = video_sub.add_parser("remux", help="Remux DVD/Blu-ray discs to MKV")
    set_completer(
        remux.add_argument(
            "-p", "--path", type=Path, default=Path("."),
            help="Path to disc folder"
        ),
        dir_completer,
    )
    remux.add_argument("--skip-mediainfo", action="store_true",
                       help="Skip MediaInfo generation")
    remux.set_defaults(func=cmd_video_remux)

    compress = video_sub.add_parser("compress", help="Batch compress MKV files using HandBrake")
    set_completer(
        compress.add_argument(
            "-d", "--directory", type=Path, default=Path("."),
            help="Directory containing MKV files"
        ),
        dir_completer,
    )
    compress.set_defaults(func=cmd_video_compress)

    chapters = video_sub.add_parser("chapters", help="Extract chapters from video files")
    set_completer(
        chapters.add_argument(
            "-p", "--path", type=Path, default=Path("."),
            help="Video file or directory"
        ),
        path_completer,
    )
    chapters.set_defaults(func=cmd_video_chapters)

    resolutions = video_sub.add_parser("resolutions", help="Print resolution information for video files")
    set_completer(
        resolutions.add_argument(
            "-p", "--path", type=Path, default=Path("."),
            help="Video file or directory"
        ),
        path_completer,
    )
    resolutions.set_defaults(func=cmd_video_resolutions)

    gif = video_sub.add_parser("gif", help="Create optimized GIF from video file")
    set_completer(
        gif.add_argument("-i", "--input", type=Path, required=True, help="Input video file"),
        path_completer,
    )
    gif.add_argument("-s", "--start", default="00:00", help="Start time (mm:ss)")
    gif.add_argument("-d", "--duration", type=int, default=30, help="Duration in seconds")
    gif.add_argument("-m", "--max-size", type=int, default=300, help="Maximum GIF size in MiB")
    set_completer(
        gif.add_argument(
            "-o", "--output", type=Path, default=Path.home() / "Desktop",
            help="Output directory"
        ),
        dir_completer,
    )
    gif.set_defaults(func=cmd_video_gif)

    thumbnails = video_sub.add_parser("thumbnails", help="Extract thumbnail grid from video")
    set_completer(
        thumbnails.add_argument("-p", "--path", type=Path, required=True, help="Video file"),
        path_completer,
    )
    thumbnails.set_defaults(func=cmd_video_thumbnails)

    filesystem = subparsers.add_parser("filesystem", help="Filesystem operations and torrent creation")
    fs_sub = filesystem.add_subparsers(dest="filesystem_command", help="Filesystem subcommands")

    tree = fs_sub.add_parser("tree", help="List directory tree with sizes")
    set_completer(
        tree.add_argument(
            "-d", "--directory", type=Path, default=Path("."),
            help="Directory to list"
        ),
        dir_completer,
    )
    tree.add_argument("-s", "--sort", choices=["size", "name"], default="size",
                      help="Sort by: size or name")
    tree.add_argument("-f", "--include-files", action="store_true",
                      help="Include files in listing")
    tree.set_defaults(func=cmd_filesystem_tree)

    torrents = fs_sub.add_parser("torrents", help="Create RED and OPS torrents for directory")
    set_completer(
        torrents.add_argument(
            "-d", "--directory", type=Path, default=Path("."),
            help="Directory to create torrent for"
        ),
        dir_completer,
    )
    torrents.add_argument("--include-subdirectories", action="store_true",
                          help="Create torrents for each subdirectory")
    torrents.set_defaults(func=cmd_filesystem_torrents)

    lastfm = subparsers.add_parser("lastfm", help="Update Last.fm scrobbles to Google Sheets")
    lastfm.set_defaults(func=cmd_lastfm)

    return parser


def main() -> None:
    """Main entry point for the toolkit CLI."""
    parser = build_parser()
    argcomplete.autocomplete(parser)
    args = parser.parse_args()

    if not args.command:
        parser.print_help()
        raise SystemExit(0)

    if hasattr(args, "func"):
        args.func(args)
    else:
        parser.parse_args([args.command, "--help"])


if __name__ == "__main__":
    main()
