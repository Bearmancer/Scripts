# pyright: reportMissingTypeStubs=false, reportUnknownMemberType=false, reportUnknownVariableType=false, reportUnknownArgumentType=false, reportAny=false, reportUnknownLambdaType=false, reportUnknownParameterType=false
import io
import random
import subprocess
import textwrap
from pathlib import Path
from typing import TypedDict

import ffmpeg  # type: ignore[import-untyped]
import pyperclip  # type: ignore[import-untyped]
from PIL import Image, ImageDraw, ImageFont  # type: ignore[import-untyped]

from toolkit.filesystem import run_command
from toolkit.logging_config import get_logger

logger = get_logger("video")

# region Types and Constants


class VideoInfo(TypedDict):
    duration: float
    width: int
    height: int


VIDEO_EXTENSIONS = [".mp4", ".mkv", ".ts", ".avi", ".webm"]
HANDBRAKE_PATH = (
    r"C:\Users\Lance\AppData\Local\Personal\HandBrakeCLI 1.8.0\HandBrakeCLI.exe"
)
MAKEMKV_PATH = r"C:\Program Files (x86)\MakeMKV\makemkvcon64.exe"

# endregion

# region Chapter Extraction


def extract_chapters(video_files: list[Path]) -> None:
    """Extract individual chapters from video files."""
    for video_file in video_files:
        probe = ffmpeg.probe(str(video_file), show_chapters=None)
        chapters = probe.get("chapters", [])

        if len(chapters) <= 1:
            logger.info(f"No chapters in {video_file.name}")
            continue

        parent_directory = video_file.parent

        for chapter_index, chapter in enumerate(chapters, 1):
            formatted_index = f"{chapter_index:02}"
            output_file_name = (
                parent_directory
                / f"{parent_directory.name} - Chapter {formatted_index}{video_file.suffix}"
            )

            (
                ffmpeg.input(
                    str(video_file), ss=chapter["start_time"], to=chapter["end_time"]
                )
                .output(str(output_file_name), c="copy", avoid_negative_ts="make_zero")
                .run(quiet=True)
            )
            logger.info(f"Extracted chapter {formatted_index} from {video_file.name}")


# endregion

# region Compression


def batch_compression(path: Path) -> None:
    """Batch compress MKV files using HandBrake."""
    mkv_files = list(path.rglob("*.mkv"))

    for file in mkv_files:
        output_file_path = file.with_suffix(".mp4")

        command = [
            HANDBRAKE_PATH,
            "--preset-import-gui",
            "-i",
            str(file),
            "-o",
            str(output_file_path),
        ]

        result = subprocess.run(command, capture_output=True, text=True)

        if result.returncode == 0:
            file.unlink()
            logger.info(f"Converted: {file.name}")
        else:
            logger.error(f"Failed: {file.name}")


# endregion

# region Disc Remuxing


def remux_disc(path: Path, fetch_mediainfo: bool = True) -> None:
    """Remux DVD/Blu-ray discs to MKV using MakeMKV."""
    remuxable_files: list[Path] = [
        f
        for f in path.rglob("*")
        if f.name in ("VIDEO_TS.IFO", "index.bdmv") and "BACKUP" not in f.parts
    ]

    if not remuxable_files:
        raise FileNotFoundError(f"No remuxable files found in {path}")

    for file in remuxable_files:
        logger.info(f"Converting: {file.name}")
        convert_disc_to_mkv(file, file.parent)
        logger.info(f"Converted: {file}")

        if fetch_mediainfo:
            for mkv_file in path.glob("*.mkv"):
                get_mediainfo(mkv_file)
                extract_images(mkv_file)


def convert_disc_to_mkv(file: Path, dvd_folder: Path) -> None:
    """Convert disc to MKV using MakeMKV CLI."""
    makemkv_command = [
        MAKEMKV_PATH,
        "mkv",
        f"file:{file}",
        "all",
        str(dvd_folder),
        "--minlength=180",
    ]
    run_command(makemkv_command, cwd=str(dvd_folder))


def get_mediainfo(video_path: Path) -> None:
    """Get MediaInfo and copy to clipboard."""
    logger.info(f"Getting MediaInfo for {video_path.name}")
    output_file = (
        Path.home() / "Desktop" / f"{video_path.parent.name} - {video_path.name}.txt"
    )

    result = subprocess.run(
        ["mediainfo", "--Output=TXT", str(video_path)], capture_output=True, text=True
    ).stdout
    cleaned_result = result.replace("Lance\\", "")

    with open(output_file, "w") as f:
        f.write(cleaned_result)

    pyperclip.copy(cleaned_result)
    logger.info("MediaInfo generated")


# endregion

# region Resolution Analysis


def print_video_resolution(video_files: list[Path]) -> None:
    """Print resolution information for video files, grouped by HD status."""
    files_hd: list[str] = []
    files_below_hd: list[tuple[str, dict[str, int]]] = []

    for file in video_files:
        resolution = get_video_resolution(file)
        if resolution:
            if resolution["Width"] >= 1920 and resolution["Height"] >= 1080:
                files_hd.append(file.name)
            else:
                files_below_hd.append((file.name, resolution))

    logger.info("=== 1920x1080 or higher ===")
    for name in files_hd:
        print(name)

    logger.info("=== Below 1920x1080 ===")
    for name, res in files_below_hd:
        print(f"{name}: {res['Width']}x{res['Height']}")


def get_video_resolution(filepath: Path) -> dict[str, int] | None:
    """Get video resolution using ffprobe."""
    probe = ffmpeg.probe(str(filepath))
    video_streams = [s for s in probe["streams"] if s["codec_type"] == "video"]

    if not video_streams:
        return None

    return {
        "Width": int(video_streams[0]["width"]),
        "Height": int(video_streams[0]["height"]),
    }


def get_video_info(video_path: Path) -> VideoInfo:
    """Get comprehensive video info (duration, width, height)."""
    probe = ffmpeg.probe(
        str(video_path),
        v="error",
        select_streams="v:0",
        show_entries="format=duration,stream=width,height",
    )
    return {
        "duration": float(probe["format"]["duration"]),
        "width": int(probe["streams"][0]["width"]),
        "height": int(probe["streams"][0]["height"]),
    }


# endregion

# region GIF Creation


def create_gif_optimized(
    input_path: Path, start: str, duration: int, max_size: int, output_dir: Path
) -> None:
    """Create optimized GIF with automatic quality reduction to meet size target."""
    if not input_path.is_file():
        raise FileNotFoundError(f"Input file not found: {input_path}")

    output_dir.mkdir(parents=True, exist_ok=True)

    original_fps, original_width = get_video_info_for_gif(input_path)
    fps = original_fps
    scale = original_width
    min_fps = 10
    min_scale = 160

    while True:
        timestamp = start.replace(":", "")
        output_name = f"{input_path.stem} - {timestamp} - {duration}.gif"
        output_path = output_dir / output_name

        size = create_gif(input_path, start, duration, output_path, fps, scale)

        if size <= max_size:
            logger.info("GIF created successfully")
            break

        if fps > min_fps:
            fps -= 1
            logger.info(f"Reducing FPS to {fps}")
        elif scale > min_scale:
            scale = max(scale - 40, min_scale)
            logger.info(f"Reducing scale to {scale}")
        else:
            logger.warning("Cannot compress further")
            break


def get_video_info_for_gif(input_path: Path) -> tuple[float, int]:
    """Get FPS and width for GIF creation."""
    probe = ffmpeg.probe(str(input_path))
    video_stream = next(
        (s for s in probe["streams"] if s["codec_type"] == "video"), None
    )

    if not video_stream:
        return 10, 320

    num, den = map(int, video_stream["r_frame_rate"].split("/"))
    fps = num / den if den != 0 else 0.0
    width = int(video_stream["width"])
    return fps, width


def create_gif(
    input_path: Path,
    start: str,
    duration: int,
    output_path: Path,
    fps: float,
    scale: int,
) -> float:
    """Create a single GIF and return its size in MiB."""
    (
        ffmpeg.input(str(input_path), ss=start, t=duration)
        .filter("fps", fps=fps)
        .filter("scale", scale, -1, flags="lanczos")
        .output(str(output_path), format="gif", gifflags="+transdiff", y=True)
        .run(quiet=True)
    )

    size = output_path.stat().st_size / (1024 * 1024)
    logger.info(f"GIF: {output_path.name} ({size:.2f} MiB)")
    return size


# endregion

# region Thumbnail Extraction


def extract_images(video_path: Path) -> None:
    """Extract thumbnail grid and full-size images from video."""
    logger.info(f"Extracting images from {video_path.name}")

    if not video_path.is_file():
        raise ValueError("Invalid file path")

    video_info = get_video_info(video_path)
    thumbnail_timestamps = create_thumbnail_grid(video_path, video_info)
    save_full_size_images(video_path, video_info, thumbnail_timestamps)

    logger.info("Images extracted")


def add_timestamp(
    image: Image.Image,
    timestamp: float,
    font_path: str = "calibri.ttf",
    font_size: int = 20,
) -> Image.Image:
    """Add timestamp overlay to image."""
    draw = ImageDraw.Draw(image)
    font = ImageFont.truetype(font_path, font_size)
    timestamp_text = f"{int(timestamp // 60):02}:{int(timestamp % 60):02}"

    text_width, text_height = font.getbbox(timestamp_text)[2:]
    text_position = (image.width - text_width - 20, image.height - text_height - 20)
    draw.text(text_position, timestamp_text, font=font, fill=(255, 255, 255, 255))
    return image


def extract_frame(
    video_path: Path,
    timestamp: float,
    video_info: VideoInfo,
    target_width: int | None = None,
) -> Image.Image:
    """Extract a single frame from video at specified timestamp."""
    input_stream = ffmpeg.input(str(video_path), ss=timestamp)
    if target_width:
        aspect_ratio = video_info["height"] / video_info["width"]
        target_height = int(target_width * aspect_ratio)
        input_stream = input_stream.filter("scale", target_width, target_height)

    out, _ = input_stream.output(
        "pipe:", vframes=1, format="image2", vcodec="mjpeg"
    ).run(capture_stdout=True, capture_stderr=True)
    img = Image.open(io.BytesIO(out))
    return add_timestamp(img, timestamp)


def add_filename_to_header(
    draw: ImageDraw.ImageDraw, filename: str, header_size: int, image_width: int
) -> None:
    """Add filename header to thumbnail grid."""
    font = ImageFont.truetype("calibri.ttf", 60)
    text_lines = textwrap.wrap(filename, width=40)

    draw.rectangle([(0, 0), (image_width, header_size)], fill=(240, 240, 240))

    y_offset = (header_size - (len(text_lines) * (font.size + 5))) // 2
    for line in text_lines:
        text_width, _ = font.getbbox(line)[2:]
        text_position = ((image_width - text_width) // 2, y_offset)
        draw.text(text_position, line, font=font, fill=(0, 0, 0, 255))
        y_offset += font.size + 5


def create_thumbnail_grid(
    video_path: Path,
    video_info: VideoInfo,
    width: int = 800,
    rows: int = 8,
    columns: int = 4,
) -> list[int]:
    """Create a grid of thumbnails from video."""
    output_path = Path.home() / "Desktop" / f"{video_path.stem} - Thumbnails.jpg"
    duration = video_info["duration"]
    timestamps = [int(duration * i / (rows * columns)) for i in range(rows * columns)]

    if output_path.exists():
        logger.info(f"Thumbnail exists: {output_path.name}")
        return timestamps

    aspect_ratio = video_info["height"] / video_info["width"]
    target_height = int(width * aspect_ratio)

    grid_width = width * columns
    grid_height = target_height * rows + 100
    grid_image = Image.new("RGB", (grid_width, grid_height), (255, 255, 255))
    draw = ImageDraw.Draw(grid_image)

    add_filename_to_header(draw, video_path.stem, 100, grid_width)

    for idx, timestamp in enumerate(timestamps):
        img = extract_frame(video_path, timestamp, video_info, width)
        if img:
            col = idx % columns
            row = idx // columns
            x = col * width
            y = 100 + row * target_height
            grid_image.paste(img, (x, y))

    grid_image.save(output_path)
    return timestamps


def save_full_size_images(
    video_path: Path, video_info: VideoInfo, thumbnail_timestamps: list[int]
) -> None:
    """Save random full-size images from video, excluding thumbnail timestamps."""
    duration = video_info["duration"]
    thumbnail_timestamps_set = set(thumbnail_timestamps)

    possible_timestamps = sorted(
        random.sample(
            [t for t in range(int(duration)) if t not in thumbnail_timestamps_set], 12
        )
    )

    for idx, timestamp in enumerate(possible_timestamps):
        img = extract_frame(video_path, timestamp, video_info)
        img.save(Path.home() / "Desktop" / f"{video_path.stem} - Image {idx + 1}.jpg")


# endregion
