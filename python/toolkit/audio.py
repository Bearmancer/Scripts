# pyright: reportMissingTypeStubs=false, reportUnknownMemberType=false, reportUnknownVariableType=false, reportUnknownArgumentType=false, reportAny=false
import os
import re
import subprocess
import pyperclip
import ffmpeg
from pathlib import Path
from pathvalidate import sanitize_filename
from tqdm import tqdm
from unidecode import unidecode

from toolkit.filesystem import run_command
from toolkit.cuesheet import process_cue_file
from toolkit.logging_config import get_logger

logger = get_logger("audio")

FLAC_44 = [(176400, 24), (88200, 24), (44100, 16)]
FLAC_48 = [(192000, 24), (96000, 24), (48000, 16)]


def prepare_directory(directory: Path) -> Path:
    def sanitize_name(p: Path) -> Path:
        return p.rename(p.with_name(sanitize_filename(unidecode(p.name)))) or p

    folder_pattern = re.compile(r"^(Disc|CD|Disk)\s?(\d+)$", re.IGNORECASE)

    for path in directory.rglob("*"):
        sanitize_name(path)

    for folder in (f for f in directory.glob("**/*") if f.is_dir()):
        if match := folder_pattern.match(folder.name):
            _, number = match.groups()
            new_name = f"Disc {int(number):02d}"
            new_path = folder.parent / new_name

            if folder != new_path:
                folder.rename(new_path)

    return directory


def progress_indicator(step: int, message: str) -> None:
    terminal_width = os.get_terminal_size().columns
    border = "=" * terminal_width
    core = f"STEP {step}: {message}"

    print(
        f"{border}\n{core.center(terminal_width)}\n{border}\n"
    )


def create_output_directory(directory: Path, suffix: str) -> Path:
    destination = directory.parent / f"{directory.name} [{suffix}]"
    exclusions = ["*.log", "*.m3u", "*.cue", "*.md5"]

    rc = subprocess.run(
        ["robocopy", str(directory), str(destination), "/S", "/XF", *exclusions],
        stdout=subprocess.DEVNULL,
    ).returncode

    if rc >= 8:
        raise RuntimeError(f"Robocopy failed with code {rc}")

    return destination


def rename_file_red(path: Path) -> None:
    if not path.exists() or not path.is_dir():
        logger.error(f"Path does not exist: {path}")
        return

    root_path = path.parent
    old_files_list: list[Path] = []
    new_files_list: list[Path] = []

    for file in path.rglob("*"):
        relative_path_length = len(str(file.relative_to(root_path)))

        if relative_path_length > 180:
            old_files_list.append(file)
            new_length = (
                180 - (relative_path_length - len(file.name)) - len(file.suffix)
            )
            new_name = file.stem[:new_length] + file.suffix
            new_file_path = file.with_name(new_name)
            file.rename(new_file_path)
            new_files_list.append(new_file_path)

            logger.info(f"Renamed: {file.name} -> {new_file_path.name}")

    if new_files_list:
        new_file_names = f"filelist:\"{'|'.join(map(str, new_files_list))}\""
        pyperclip.copy(new_file_names)
        logger.info(f"Renamed {len(new_files_list)} files")
    else:
        logger.info("No files needed renaming")


def calculate_image_size(path: Path) -> None:
    exif_tool = r"C:\Users\Lance\Desktop\exiftool-12.96_64\exiftool.exe"
    problematic_files: list[Path] = []

    for flac_file in path.glob("*.flac"):
        result = subprocess.run(
            [exif_tool, "-PictureLength", "-s", "-s", "-s", str(flac_file)],
            capture_output=True,
            text=True,
        )

        if not result.stdout.strip():
            continue

        image_size_kb = round(int(result.stdout.strip()) / 1024, 2)

        if image_size_kb > 1024:
            logger.warning(f"{flac_file.name}: {image_size_kb} KB")
            problematic_files.append(flac_file)

    if problematic_files:
        logger.warning(f"Found {len(problematic_files)} files with artwork > 1MB")
    else:
        logger.info("No files with embedded artwork larger than 1MB")


def process_sacd_directory(directory: Path, fmt: str = "all") -> None:
    iso_files = list(directory.rglob("*.iso"))
    output_dirs: list[tuple[Path, int]] = []

    progress_indicator(1, "Converting all ISOs -> DFF + CUE sheets")

    for disc_number, iso in enumerate(iso_files, 1):
        dirs = convert_iso_to_dff_and_cue(iso, directory, disc_number)
        output_dirs.extend((folder, disc_number) for folder in dirs)

    progress_indicator(2, "CONVERTING DFF + CUE sheet -> FLAC")

    parent_folders = set(folder.parent for folder, _ in output_dirs)

    for folder, _ in output_dirs:
        convert_dff_to_flac(folder)

    for parent_folder in parent_folders:
        convert_audio(3, parent_folder, fmt)


def convert_iso_to_dff_and_cue(
    iso_path: Path, base_dir: Path, disc_number: int
) -> list[Path]:
    probe_result = run_command(
        ["sacd_extract", "-P", "-i", str(iso_path)], cwd=str(base_dir)
    )[0]

    channel_configs = [
        ("Speaker config: (Stereo|2)", "Stereo", ["-2", "-e", "-c", "-C"]),
        (
            "Speaker config: (Multichannel|5|6)",
            "Multichannel",
            ["-m", "-e", "-c", "-C"],
        ),
    ]

    out_dirs: list[Path] = []

    for pattern, suffix, cmd in channel_configs:
        if re.search(pattern, probe_result):
            channel_dir = base_dir.parent / f"{base_dir.name} [{suffix}]"
            channel_dir.mkdir(exist_ok=True, parents=True)

            output_disc_dir = channel_dir / f"Disc {disc_number:02d}"
            old_dirs = set(Path(channel_dir).glob("*/"))

            run_command(
                ["sacd_extract", *cmd, "-i", str(iso_path)], cwd=str(channel_dir)
            )
            logger.info(f"{suffix} audio extracted from {iso_path.name}")

            new_dirs = set(Path(channel_dir).glob("*/"))
            new_dir = next(iter(new_dirs - old_dirs), None)

            if new_dir:
                new_dir.rename(output_disc_dir)
                out_dirs.append(output_disc_dir)

    return out_dirs


def convert_dff_to_flac(dff_dir: Path) -> None:
    cue_file = next(dff_dir.rglob("*.cue"))
    dff_file = next(dff_dir.rglob("*.dff"))

    if not cue_file.exists():
        raise FileNotFoundError(f"CUE file not found: {cue_file}")

    gain_db = calculate_gain(dff_file)
    process_cue_file(cue_file, gain_db)

    if dff_file.exists():
        dff_file.unlink()


def calculate_gain(dff_file: Path, target_headroom_db: float = -0.5) -> float:
    if not dff_file.exists():
        raise FileNotFoundError(f"DFF file not found: {dff_file}")

    peaks: list[float] = []

    _, error = (
        ffmpeg.input(str(dff_file))
        .audio.filter("volumedetect")
        .output("null", format="null")
        .run(capture_stderr=True)
    )

    if isinstance(error, (bytes, bytearray)):
        error = error.decode("utf-8", errors="ignore")

    if m := re.search(r"max_volume: (-\d+\.?\d*) dB", error):
        peaks.append(float(m.group(1)))

    if not peaks:
        raise RuntimeError("Could not detect peak levels")

    return target_headroom_db - max(peaks)


def convert_audio(current_step: int, directory: Path, fmt: str = "all") -> None:
    flac_files = list(directory.rglob("*.flac"))

    if not flac_files:
        logger.warning("No FLAC files found")
        return

    bd, sr = next(
        (int(m["bits_per_raw_sample"]), int(m["sample_rate"]))
        for f in flac_files
        if (m := get_metadata(f)) and m["bits_per_raw_sample"] and m["sample_rate"]
    )

    flac_tiers = get_flac_tiers(sr, bd, fmt)

    for i, t in enumerate(flac_tiers, start=current_step):
        progress_indicator(i, f"Converting {directory} from {bd}-bit/{sr}Hz to {t}")
        flac_directory_conversion(directory, t)

    if fmt in ["mp3", "all"]:
        progress_indicator(current_step + len(flac_tiers), "Converting FLAC to MP3")
        convert_to_mp3(directory)


def flac_directory_conversion(directory: Path, tier: tuple[int, int]) -> None:
    sample_rate, bit_depth = tier
    suffix = f"{bit_depth} - {sample_rate / 1000:.1f}"

    destination = create_output_directory(directory, suffix)

    flac_files = list(destination.rglob("*.flac"))

    for f in tqdm(flac_files, desc=f"Converting to {bit_depth}-bit/{sample_rate} Hz"):
        downsample_flac(f, tier)


def downsample_flac(file: Path, tier: tuple[int, int]) -> None:
    sample_rate, bit_depth = tier
    temp_a = file.with_name("a.flac")
    temp_b = file.with_name("b.flac")

    file.rename(temp_a)

    cmd = [
        "sox",
        "-S",
        str(temp_a),
        "-b",
        str(bit_depth),
        "-R",
        "-G",
        str(temp_b),
        "rate",
        "-v",
        "-L",
        str(sample_rate),
    ]

    run_command(cmd)
    temp_a.unlink()
    temp_b.rename(file)


def convert_to_mp3(directory: Path) -> None:
    flac_files = list(directory.rglob("*.flac"))

    if not flac_files:
        raise FileNotFoundError("No FLAC files found")

    destination = create_output_directory(directory, "MP3")

    for f in tqdm(flac_files, desc="Converting to MP3"):
        output = destination / f.with_suffix(".mp3").name

        (
            ffmpeg.input(str(f))
            .output(
                str(output), acodec="libmp3lame", audio_bitrate="320k", format="mp3"
            )
            .run(quiet=True)
        )


def get_metadata(file: Path) -> dict[str, str | None]:
    probe_result = ffmpeg.probe(str(file))
    audio_stream = next(
        stream
        for stream in probe_result.get("streams", [])
        if stream.get("codec_type") == "audio"
    )

    return {
        "bits_per_raw_sample": audio_stream.get("bits_per_raw_sample"),
        "sample_rate": audio_stream.get("sample_rate"),
    }


def get_flac_tiers(
    sample_rate: int, bit_depth: int, fmt: str = "all"
) -> list[tuple[int, int]]:
    tiers = FLAC_44 if sample_rate in {44100, 88200, 176400} else FLAC_48

    for i, (tier_sr, tier_bd) in enumerate(tiers):
        if sample_rate > tier_sr and bit_depth >= tier_bd:
            result = [tier for tier in tiers[i:] if fmt != "24-bit" or tier[1] == 24]
            return result or []

    raise ValueError(f"No suitable tier for {bit_depth}-bit/{sample_rate}Hz")
