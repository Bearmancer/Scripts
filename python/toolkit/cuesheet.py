# pyright: reportMissingTypeStubs=false, reportUnknownMemberType=false, reportUnknownVariableType=false, reportUnknownArgumentType=false, reportAny=false
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any

import chardet  # type: ignore[import-untyped]
import ffmpeg  # type: ignore[import-untyped]
from deflacue.deflacue import CueParser  # type: ignore[import-untyped]
from pathvalidate import sanitize_filename  # type: ignore[import-untyped]
from tqdm import tqdm  # type: ignore[import-untyped]

# region Data Model


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


# endregion

# region CUE Parsing


def parse_cue_file(cuefile_path: Path) -> Any:
    """Parse a CUE file with automatic encoding detection."""
    cue_content = cuefile_path.read_bytes()
    cue_encoding: str = chardet.detect(cue_content)["encoding"] or "utf-8"

    parser = CueParser.from_file(cuefile_path, encoding=cue_encoding)
    return parser.run()


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

    probe_result: dict[str, Any] = ffmpeg.probe(str(file))
    total_duration = float(probe_result["format"]["duration"])

    for i, track in enumerate(tracks[:-1]):
        track.duration = tracks[i + 1].start_sec - track.start_sec

    tracks[-1].duration = total_duration - tracks[-1].start_sec
    return tracks


# endregion

# region Track Processing


def process_tracks(
    tracks: list[TrackInfo], cue_file: Path, volume_adjustment: float = 0.0
) -> None:
    """Extract individual FLAC files from CUE sheet with volume adjustment."""
    track_count = len(tracks)
    cue_directory = cue_file.parent

    for track in tqdm(
        tracks, total=track_count, desc=f"Converting {cue_file.parent.name}"
    ):
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
                sample_fmt="s32",
                ar="88200",
                metadata=metadata,
            )
            .global_args("-y", "-loglevel", "error")
            .run()
        )


def process_cue_file(cue_file: Path, volume_adjustment: float = 0.0) -> None:
    """Process a CUE file: parse, extract tracks, and convert to FLAC."""
    cue_file = Path(cue_file).absolute()

    if not cue_file.exists():
        raise FileNotFoundError(f"CUE file not found: {cue_file}")

    cue_data = parse_cue_file(cue_file)
    tracks = extract_track_data(cue_data)
    tracks = calculate_track_durations(tracks, cue_file)
    process_tracks(tracks, cue_file, volume_adjustment)


# endregion
