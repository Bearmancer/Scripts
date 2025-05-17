import logging
import chardet
import ffmpeg
from pathlib import Path
from dataclasses import dataclass, field
import sys
import os
import argparse
from pathvalidate import sanitize_filename
from deflacue.deflacue import CueParser

@dataclass
class TrackInfo:
    file: str
    parent: str
    title: str
    track_num: int
    start_sec: float
    duration: float = None
    metadata: dict = field(default_factory=dict)


def parse_cue_file(cuefile_path):
    with open(cuefile_path, 'rb') as file:
        cue_content = file.read()
        cue_encoding = chardet.detect(cue_content)['encoding']

    parser = CueParser.from_file(cuefile_path, encoding=cue_encoding)
    return parser.run()


def extract_track_data(cue_data):
    album_info = cue_data.meta.data
    tracks = cue_data.tracks
    source_file = str(tracks[0].file.path)
    track_list = []

    for track in tracks:
        start_time_sec = track.start / 44100

        metadata = {}
        metadata.update(album_info)
        metadata.update(track.data)

        track_info = TrackInfo(
            file=source_file,
            parent=str(Path(source_file).parent),
            title=sanitize_filename(track.title),
            track_num=track.num,
            start_sec=start_time_sec,
            metadata=metadata
        )

        track_list.append(track_info)

    return track_list


def calculate_track_durations(tracks):
    file = Path(tracks[0].file).resolve()

    probe_result = ffmpeg.probe(str(file))
    total_duration = float(probe_result['format']['duration'])

    for i in range(len(tracks) - 1):
        current_start = tracks[i].start_sec
        next_start = tracks[i + 1].start_sec
        tracks[i].duration = next_start - current_start

    last_start = tracks[-1].start_sec
    tracks[-1].duration = total_duration - last_start

    return tracks


def process_tracks(tracks, cue_directory, volume_adjustment=0.0):
    print("\nExtracting audio tracks:")
    track_count = len(tracks)

    for index, track in enumerate(tracks, 1):
        track_number = str(track.track_num).rjust(2, '0')
        output_filename = f"{track_number}. {track.title}.flac"
        output_path = cue_directory / output_filename

        logging.info(f"Track {index}/{track_count} - {output_filename}")

        metadata_mappings = {
            'title': track.title,
            'track': track.track_num,
            'album': track.metadata.get('ALBUM'),
            'artist': track.metadata.get('PERFORMER'),
            'genre': track.metadata.get('GENRE'),
            'date': track.metadata.get('DATE')
        }

        metadata = [f"{k}={v}" for k, v in metadata_mappings.items() if v]

        (
            ffmpeg
            .input(track.file, ss=f"{track.start_sec:.6f}")
            .audio
            .filter('volume', volume=f"{volume_adjustment}dB")
            .output(
                str(output_path),
                t=f"{track.duration:.6f}",
                acodec='flac',
                sample_fmt='s32',
                ar='88200',
                metadata=metadata,
            )
            .global_args('-y', '-loglevel', 'error')
            .run()
        )

    logging.info("Extraction completed successfully.\n----------------------")


def process_cue_file(cuefile_path, volume_adjustment=0.0):
    cuefile_path = Path(cuefile_path).absolute()
    cue_directory = cuefile_path.parent

    logging.info(f"Processing: {cuefile_path.stem}")

    original_dir = Path.cwd()
    os.chdir(cue_directory)

    cue_data = parse_cue_file(cuefile_path)
    tracks = extract_track_data(cue_data)
    tracks = calculate_track_durations(tracks)

    process_tracks(tracks, cue_directory, volume_adjustment)

    os.chdir(original_dir)


def main():
    parser = argparse.ArgumentParser(description='Split DFF audio files using CUE sheets')
    parser.add_argument('cuefile', help='Path to the CUE file')
    parser.add_argument('--volume', type=float, default=0.0, help='Volume adjustment in dB')

    args = parser.parse_args()

    try:
        process_cue_file(args.cuefile, args.volume)
    except Exception as e:
        logging.error(f"Error: {e}")
        sys.exit(1)


if __name__ == "__main__":
    main()