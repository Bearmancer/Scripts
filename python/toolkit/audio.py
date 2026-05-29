import re
import shutil
import subprocess
from pathlib import Path
from typing import Any

import ffmpeg
from pathvalidate import sanitize_filename
from tqdm import tqdm
from unidecode import unidecode

from toolkit.cuesheet import process_cue_file
from toolkit.exceptions import ConversionError
from toolkit.logging_config import get_logger
from toolkit.types import AudioFormat, AudioTier
from toolkit.utils import run_command

logger = get_logger("audio")

FLAC_44: list[AudioTier] = [
	{"sample_rate": 176400, "bit_depth": 24},
	{"sample_rate": 88200, "bit_depth": 24},
	{"sample_rate": 44100, "bit_depth": 16},
]
FLAC_48: list[AudioTier] = [
	{"sample_rate": 192000, "bit_depth": 24},
	{"sample_rate": 96000, "bit_depth": 24},
	{"sample_rate": 48000, "bit_depth": 16},
]

SACD_MASTER_SAMPLE_FMT = "s32"
SACD_MASTER_SAMPLE_RATE = 88200


def prepare_directory(directory: Path) -> Path:
	"""Sanitize filenames and normalize disc folder names."""

	def sanitize_name(p: Path) -> Path:
		new_name = sanitize_filename(unidecode(p.name))
		if new_name == p.name:
			return p
		try:
			return p.rename(p.with_name(new_name))
		except OSError:
			logger.error(f"Failed to rename: {p.name}")
			return p

	folder_pattern = re.compile(r"^(Disc|CD|Disk)\s?(\d+)$", re.IGNORECASE)

	for path in directory.rglob("*"):
		sanitize_name(path)

	for folder in (f for f in directory.glob("**/*") if f.is_dir()):
		if match := folder_pattern.match(folder.name):
			_, number = match.groups()
			new_name = f"Disc {int(number):02d}"
			new_path = folder.parent / new_name

			if folder != new_path:
				if new_path.exists():
					shutil.rmtree(new_path)
				folder.rename(new_path)

	return directory


def create_output_directory(directory: Path, suffix: str) -> Path:
	"""Create output directory with suffix, copying non-audio files."""
	destination = directory.parent / f"{directory.name} [{suffix}]"
	destination.mkdir(parents=True, exist_ok=True)

	audio_extensions = {".flac", ".mp3", ".dff", ".dsf", ".wav", ".aiff"}
	skip_extensions = {".log", ".m3u", ".cue", ".md5"}

	for item in directory.rglob("*"):
		if (
			item.is_file()
			and item.suffix.lower() not in audio_extensions
			and item.suffix.lower() not in skip_extensions
		):
			rel = item.relative_to(directory)
			dest_file = destination / rel
			dest_file.parent.mkdir(parents=True, exist_ok=True)
			shutil.copy2(item, dest_file)

	return destination


def calculate_image_size(path: Path) -> None:
	"""Report FLAC files with embedded artwork larger than 1MB."""

	exif_tool = os.getenv("EXIFTOOL_PATH", "")
	if not exif_tool:
		raise ValueError(
			"EXIFTOOL_PATH environment variable not set. "
			"Please configure the path to the exiftool executable."
		)
	problematic_files: list[Path] = []

	for flac_file in path.glob("*.flac"):
		try:
			result = subprocess.run(
				[exif_tool, "-PictureLength", "-s", "-s", "-s", str(flac_file)],
				capture_output=True,
				text=True,
			)
		except subprocess.CalledProcessError as e:
			logger.error(f"exiftool failed for {flac_file.name} (exit {e.returncode})")
			if e.stderr:
				logger.error(f"stderr: {e.stderr[:200]}")
			raise FileOperationError(
				f"Failed to check artwork size for {flac_file.name}", flac_file
			) from e
		except OSError as e:
			logger.error(f"Failed to check artwork size for {flac_file.name}: {e}")
			raise FileOperationError(
				f"Failed to check artwork size for {flac_file.name}", flac_file
			) from e

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


def process_sacd_directory(directory: Path, fmt: AudioFormat = "all") -> None:
	"""Extract SACD ISOs, build a high-resolution master, then final deliverables."""
	iso_files = sorted(directory.rglob("*.iso"))
	output_dirs: list[Path] = []

	if not iso_files:
		logger.warning("No SACD ISO files found")
		return

	for disc_number, iso in enumerate(
		tqdm(iso_files, desc="SACD ISOs", unit="disc", position=0), 1
	):
		try:
			dirs = convert_iso_to_dff_and_cue(iso, directory, disc_number)
			output_dirs.extend(dirs)
		except subprocess.CalledProcessError as e:
			logger.error(f"ISO extraction failed for {iso.name} (exit {e.returncode})")
			if e.stderr:
				logger.error(f"stderr: {e.stderr[:500]}")
			raise ConversionError(
				f"ISO extraction failed for {iso.name}", iso, e.cmd
			) from e

	if not output_dirs:
		logger.warning("No DFF/CUE extraction output was detected")
		return

	for folder in tqdm(
		output_dirs, desc="Converting DFF", unit="disc", position=0, leave=True
	):
		try:
			convert_dff_to_flac(
				folder,
				SACD_MASTER_SAMPLE_FMT,
				SACD_MASTER_SAMPLE_RATE,
				position=1,
			)
		except subprocess.CalledProcessError as e:
			logger.error(f"DFF conversion failed in {folder} (exit {e.returncode})")
			if e.stderr:
				logger.error(f"stderr: {e.stderr[:500]}")
			raise ConversionError(
				f"DFF conversion failed in {folder}", folder, e.cmd
			) from e
		except FileNotFoundError as e:
			logger.error(f"File not found in {folder}: {e}")
			raise

	channel_roots = sorted({folder.parent for folder in output_dirs}, key=str)
	if fmt != "24-bit":
		for channel_root in channel_roots:
			convert_audio(channel_root, fmt)


def convert_iso_to_dff_and_cue(
	iso_path: Path, base_dir: Path, disc_number: int
) -> list[Path]:
	"""Extract stereo and/or multichannel audio from SACD ISO."""
	probe_stdout, probe_stderr = run_command(
		["sacd_extract", "-P", "-i", str(iso_path)], cwd=str(base_dir)
	)
	probe_result = f"{probe_stdout}\n{probe_stderr}"

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
		if re.search(pattern, probe_result, flags=re.IGNORECASE):
			channel_dir = base_dir.parent / f"{base_dir.name} [{suffix}]"
			channel_dir.mkdir(exist_ok=True, parents=True)

			output_disc_dir = channel_dir / f"Disc {disc_number:02d}"
			old_dirs = {p for p in channel_dir.iterdir() if p.is_dir()}

			try:
				run_command(
					["sacd_extract", *cmd, "-i", str(iso_path)], cwd=str(channel_dir)
				)
			except subprocess.CalledProcessError as e:
				logger.error(
					f"sacd_extract {suffix} failed for {iso_path.name} (exit {e.returncode})"
				)
				if e.stderr:
					logger.error(f"stderr: {e.stderr[:500]}")
				raise
			logger.info(f"{suffix} audio extracted from {iso_path.name}")

			new_dirs = {p for p in channel_dir.iterdir() if p.is_dir()}
			created_dirs = sorted(
				new_dirs - old_dirs,
				key=lambda p: p.stat().st_mtime,
				reverse=True,
			)
			new_dir = created_dirs[0] if created_dirs else None

			if new_dir:
				if output_disc_dir.exists():
					shutil.rmtree(output_disc_dir)
				new_dir.rename(output_disc_dir)
				out_dirs.append(output_disc_dir)
			elif output_disc_dir.exists():
				logger.info(
					f"Reusing existing extraction directory for {iso_path.name} [{suffix}]"
				)
				out_dirs.append(output_disc_dir)
			else:
				logger.warning(
					f"sacd_extract produced no new directory for {iso_path.name} [{suffix}] - disc skipped"
				)

	return out_dirs


def convert_dff_to_flac(
	dff_dir: Path, sample_fmt: str = "s32", sample_rate: int = 88200, position: int = 0
) -> None:
	cue_file = next(dff_dir.rglob("*.cue"), None)
	dff_file = next(dff_dir.rglob("*.dff"), None)

	if cue_file is None:
		raise FileNotFoundError(f"No .cue file found in {dff_dir}")
	if dff_file is None:
		raise FileNotFoundError(f"No .dff file found in {dff_dir}")

	gain_db = calculate_gain(dff_file)
	process_cue_file(cue_file, gain_db, sample_fmt, sample_rate, position)

	if dff_file.exists():
		dff_file.unlink()


def calculate_gain(dff_file: Path, target_headroom_db: float = -0.5) -> float:
	"""Calculate gain adjustment needed for target headroom."""
	if not dff_file.exists():
		raise FileNotFoundError(f"DFF file not found: {dff_file}")

	peaks: list[float] = []

	_: Any
	error: Any
	try:
		_, error = (
			ffmpeg
			.input(str(dff_file))
			.audio.filter("volumedetect")
			.output("null", format="null")
			.run(capture_stderr=True)
		)
	except ffmpeg.Error as e:
		stderr = e.stderr.decode("utf-8", errors="replace") if e.stderr else "no stderr"
		logger.error(f"ffmpeg volumedetect failed on {dff_file.name}: {stderr}")
		raise ConversionError(
			f"ffmpeg volumedetect failed on {dff_file.name}", dff_file
		) from e

	if isinstance(error, (bytes, bytearray)):
		error_str: str = error.decode("utf-8", errors="ignore")
	else:
		error_str = str(error)

	if m := re.search(r"max_volume: (-\d+\.?\d*) dB", error_str):
		peaks.append(float(m.group(1)))

	if not peaks:
		raise ConversionError("Could not detect peak levels", dff_file)

	return target_headroom_db - max(peaks)


def convert_audio(directory: Path, fmt: AudioFormat = "all") -> None:
	"""Convert FLAC files to various sample rates and bit depths."""
	flac_files = list(directory.rglob("*.flac"))

	if not flac_files:
		logger.warning("No FLAC files found")
		return

	metadata_pair = next(
		(
			(int(m["bits_per_raw_sample"]), int(m["sample_rate"]))
			for f in flac_files
			if (m := get_metadata(f)) and m["bits_per_raw_sample"] and m["sample_rate"]
		),
		None,
	)
	if metadata_pair is None:
		logger.error(
			"No FLAC files with valid audio metadata found - skipping conversion"
		)
		return
	bd, sr = metadata_pair

	if sr == 44100 and bd == 16 and fmt not in ("mp3", "all"):
		logger.info("Source is already CD quality, skipping FLAC conversion")
		return

	flac_tiers = get_flac_tiers(sr, bd, fmt)

	with tqdm(flac_tiers, desc="Tiers", unit="tier", position=0) as tier_bar:
		for tier in tier_bar:
			tier_sr = tier["sample_rate"]
			tier_bd = tier["bit_depth"]
			tier_bar.set_postfix_str(f"{tier_bd}-bit/{tier_sr // 1000}kHz")
			flac_directory_conversion(directory, tier, position=1)

	if fmt in ["mp3", "all"]:
		convert_to_mp3(directory)


def flac_directory_conversion(
	directory: Path, tier: AudioTier, position: int = 0
) -> None:
	"""Convert all FLAC files in directory to specified tier."""
	sample_rate = tier["sample_rate"]
	bit_depth = tier["bit_depth"]
	suffix = f"{bit_depth} - {sample_rate / 1000:.1f}"

	destination = create_output_directory(directory, suffix)
	flac_files = list(directory.rglob("*.flac"))

	for f in tqdm(
		flac_files,
		desc=f"{bit_depth}-bit/{sample_rate // 1000}kHz",
		position=position,
		leave=False,
	):
		rel = f.relative_to(directory)
		dest_file = destination / rel
		dest_file.parent.mkdir(parents=True, exist_ok=True)
		try:
			downsample_flac(f, dest_file, tier)
		except subprocess.CalledProcessError as e:
			logger.error(f"SoX conversion failed: {f.name} (exit {e.returncode})")
			if e.stderr:
				logger.error(f"SoX stderr: {e.stderr[:500]}")
			raise ConversionError(
				f"SoX conversion failed for {f.name}", f, e.cmd
			) from e


def downsample_flac(source: Path, dest: Path, tier: AudioTier) -> None:
	"""Downsample a single FLAC file using SoX, writing directly to destination."""
	sample_rate = tier["sample_rate"]
	bit_depth = tier["bit_depth"]
	cmd = [
		"sox",
		"-S",
		str(source),
		"-b",
		str(bit_depth),
		"-R",
		"-G",
		str(dest),
		"rate",
		"-v",
		"-L",
		str(sample_rate),
	]
	if bit_depth == 16:
		cmd.extend(["dither", "-s"])

	try:
		run_command(cmd)
	except subprocess.CalledProcessError as e:
		logger.error(f"SoX conversion failed: {source.name} (exit {e.returncode})")
		if e.stderr:
			logger.error(f"SoX stderr: {e.stderr[:500]}")
		if dest.exists():
			dest.unlink()
		raise ConversionError(
			f"SoX conversion failed for {source.name}", source, cmd
		) from e


def convert_to_mp3(directory: Path) -> None:
	"""Convert all FLAC files in directory to 320kbps MP3."""
	flac_files = list(directory.rglob("*.flac"))

	if not flac_files:
		raise FileNotFoundError("No FLAC files found")

	destination = create_output_directory(directory, "MP3")

	for f in tqdm(flac_files, desc="Converting to MP3"):
		rel = f.relative_to(directory)
		output = destination / rel.with_suffix(".mp3")
		output.parent.mkdir(parents=True, exist_ok=True)

		try:
			(
				ffmpeg
				.input(str(f))
				.output(
					str(output), acodec="libmp3lame", audio_bitrate="320k", format="mp3"
				)
				.run(quiet=True)
			)
		except ffmpeg.Error as e:
			stderr = (
				e.stderr.decode("utf-8", errors="replace") if e.stderr else "no stderr"
			)
			logger.error(f"ffmpeg MP3 conversion failed for {f.name}: {stderr[-300:]}")
			raise ConversionError(
				f"ffmpeg MP3 conversion failed for {f.name}", f
			) from e


def get_metadata(file: Path) -> dict[str, str | None]:
	"""Extract audio metadata from a file using ffprobe."""
	probe_result = ffmpeg.probe(str(file))
	audio_stream = next(
		(
			stream
			for stream in probe_result.get("streams", [])
			if stream.get("codec_type") == "audio"
		),
		None,
	)
	if audio_stream is None:
		raise ConversionError(f"No audio stream found in {file.name}", file)

	return {
		"bits_per_raw_sample": audio_stream.get("bits_per_raw_sample"),
		"sample_rate": audio_stream.get("sample_rate"),
	}


def get_flac_tiers(
	sample_rate: int, bit_depth: int, fmt: AudioFormat = "all"
) -> list[AudioTier]:
	"""Determine which FLAC tiers to convert to based on source format."""
	tiers = FLAC_44 if sample_rate in {44100, 88200, 176400} else FLAC_48

	for i, tier in enumerate(tiers):
		tier_sr = tier["sample_rate"]
		tier_bd = tier["bit_depth"]
		if sample_rate > tier_sr and bit_depth >= tier_bd:
			match fmt:
				case "16bit":
					return [tier for tier in tiers[i:] if tier["bit_depth"] == 16][:1]
				case "cd":
					cd_tier: AudioTier = {"sample_rate": 44100, "bit_depth": 16}
					return [cd_tier] if cd_tier in tiers[i:] else []
				case "all":
					return tiers[i:]
				case "24-bit":
					return [tier for tier in tiers[i:] if tier["bit_depth"] == 24]
				case "mp3":
					return []

	return []


def get_dff_target_params(fmt: AudioFormat) -> tuple[str, int]:
	"""Determine target sample format and rate for DFF conversion based on format."""
	match fmt:
		case "16bit" | "cd" | "mp3":
			return "s16", 44100
		case "24-bit" | "all":
			return "s32", 88200


def detect_audio_mode(directory: Path) -> str:
	"""Auto-detect processing mode: 'extract' for SACD ISOs, 'convert' for FLAC."""
	if any(directory.rglob("*.iso")):
		return "extract"
	return "convert"
