from dataclasses import dataclass, field
from pathlib import Path
from typing import Any

import chardet
import ffmpeg
from deflacue.deflacue import CueParser
from pathvalidate import sanitize_filename
from tqdm import tqdm

from toolkit.exceptions import ConversionError
from toolkit.logging_config import get_logger

logger = get_logger("cuesheet")


@dataclass
class TrackInfo:
    """Represents a single track parsed from a CUE sheet."""

    file: str
    parent: str
    title: str
    track_num: int
    start_sec: float
    duration: float | None = None
    metadata: dict[str, str] = field(default_factory=dict)


def parse_cue_file(cuefile_path: Path) -> Any:
    """Parse a CUE file with automatic encoding detection."""
    try:
        cue_content = cuefile_path.read_bytes()
    except FileNotFoundError as e:
        logger.error(f"CUE file not found: {e.filename}")
        raise
    except PermissionError as e:
        logger.error(f"Cannot read CUE file (permission denied): {e.filename}")
        raise

    try:
        cue_encoding: str = chardet.detect(cue_content)["encoding"] or "utf-8"
        parser = CueParser.from_file(cuefile_path, encoding=cue_encoding)
        return parser.run()
    except UnicodeDecodeError as e:
        logger.error(
            f"Cannot decode {cuefile_path.name} with {e.encoding}: {e.reason} at bytes {e.start}-{e.end}"
        )
        raise
    except ValueError as e:
        logger.error(f"Malformed CUE file {cuefile_path.name}: {e}")
        raise


def extract_track_data(cue_data: Any) -> list[TrackInfo]:
    """Extract track information from parsed CUE data."""
    album_info: dict[str, str] = cue_data.meta.data
    tracks: list[Any] = cue_data.tracks
    source_file: str = str(tracks[0].file.path)
    track_list: list[TrackInfo] = []

    for track in tracks:
        start_time_sec: float = track.start / 44100
        metadata: dict[str, str] = album_info | track.data

        track_info = TrackInfo(
            file=source_file,
            parent=str(Path(source_file).parent),
            title=str(track.title),
            track_num=int(track.num),
            start_sec=start_time_sec,
            metadata=metadata,
        )

        track_list.append(track_info)

    return track_list


def calculate_track_durations(
    tracks: list[TrackInfo], cue_file: Path
) -> list[TrackInfo]:
    """Calculate duration for each track based on start times and total file duration."""
    p = Path(tracks[0].file)
    file = (cue_file.parent / p if not p.is_absolute() else p).resolve()

    try:
        probe_result = ffmpeg.probe(str(file))
    except ffmpeg.Error as e:
        stderr = e.stderr.decode("utf-8", errors="replace") if e.stderr else "no stderr"
        logger.error(f"ffprobe failed on {file.name}: {stderr[-300:]}")
        raise ConversionError(f"ffprobe failed on {file.name}", file) from e
    total_duration = float(probe_result["format"]["duration"])

    for i, track in enumerate(tracks[:-1]):
        track.duration = tracks[i + 1].start_sec - track.start_sec

    tracks[-1].duration = total_duration - tracks[-1].start_sec
    return tracks


def process_tracks(
    tracks: list[TrackInfo],
    cue_file: Path,
    volume_adjustment: float = 0.0,
    sample_fmt: str = "s32",
    sample_rate: int = 88200,
    position: int = 0,
) -> None:
    """Extract individual FLAC files from CUE sheet with volume adjustment."""
    track_count = len(tracks)
    cue_directory = cue_file.parent

    for track in tqdm(
        tracks,
        total=track_count,
        desc=f"Converting {cue_file.parent.name}",
        position=position,
        leave=False,
    ):
        output_path = cue_directory / "temp.flac"
        try:
            track_number = str(track.track_num).rjust(2, "0")
            output_filename = f"{track_number}. {sanitize_filename(track.title)}.flac"
            output_path = cue_directory / output_filename

            metadata_mappings: dict[str, str | int | None] = {
                "title": track.title,
                "track": track.track_num,
                "album": track.metadata.get("ALBUM"),
                "artist": track.metadata.get("PERFORMER"),
                "genre": track.metadata.get("GENRE"),
                "date": track.metadata.get("DATE"),
            }

            metadata = [f"{k}={v}" for k, v in metadata_mappings.items() if v]

            p = Path(track.file)
            stream: Any = ffmpeg.input(
                str((cue_file.parent / p if not p.is_absolute() else p).resolve()),
                ss=f"{track.start_sec:.6f}",
            )
            (
                stream.audio.filter("volume", volume=f"{volume_adjustment}dB")
                .output(
                    str(output_path),
                    t=f"{track.duration:.6f}" if track.duration else "0",
                    acodec="flac",
                    sample_fmt=sample_fmt,
                    ar=str(sample_rate),
                    metadata=metadata,
                )
                .global_args("-y", "-loglevel", "error")
                .run()
            )
        except ffmpeg.Error as e:
            stderr = (
                e.stderr.decode("utf-8", errors="replace") if e.stderr else "no stderr"
            )
            logger.error(
                f"ffmpeg failed on track {track.track_num} '{track.title}': {stderr[-300:]}"
            )
            raise ConversionError(
                f"ffmpeg failed on track {track.track_num} '{track.title}'", output_path
            ) from e
        except OSError as e:
            logger.error(
                f"I/O error on track {track.track_num} '{track.title}': {e.strerror} [{e.errno}]"
            )
            raise ConversionError(
                f"I/O error on track {track.track_num} '{track.title}'", output_path
            ) from e
        except ValueError as e:
            logger.error(
                f"Invalid value on track {track.track_num} '{track.title}': {e}"
            )
            raise ConversionError(
                f"Invalid value on track {track.track_num} '{track.title}'", output_path
            ) from e


def process_cue_file(
    cue_file: Path,
    volume_adjustment: float = 0.0,
    sample_fmt: str = "s32",
    sample_rate: int = 88200,
    position: int = 0,
) -> None:
    """Process a CUE file: parse, extract tracks, and convert to FLAC."""
    cue_file = Path(cue_file).absolute()

    if not cue_file.exists():
        raise FileNotFoundError(f"CUE file not found: {cue_file}")

    cue_data = parse_cue_file(cue_file)
    tracks = extract_track_data(cue_data)
    tracks = calculate_track_durations(tracks, cue_file)
    process_tracks(
        tracks, cue_file, volume_adjustment, sample_fmt, sample_rate, position
    )
