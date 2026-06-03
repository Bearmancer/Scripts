import json
import os
from pathlib import Path

from py3createtorrent import create_torrent

from toolkit.exceptions import FileOperationError
from toolkit.logging_config import get_logger

logger = get_logger("filesystem")


def get_folder_size(path: Path) -> int:
	"""Calculate total size of all files in a directory recursively."""
	total = 0
	for entry in path.rglob("*"):
		try:
			if entry.is_file():
				total += entry.stat().st_size
		except OSError:
			continue
	return total


def list_directories(path: Path, sort_order: str = "0", indent: int = 0) -> None:
	"""List directories with sizes, sorted by size or name."""
	indentation = "  " * indent
	folder_size = get_folder_size(path)
	output = f"{indentation}{path.name} ({folder_size / (1024**2):.2f} MB)"
	print(output)

	entries = [
		(entry, get_folder_size(entry)) for entry in path.iterdir() if entry.is_dir()
	]

	entries.sort(
		key=lambda e: e[0].name if sort_order == "1" else e[1],
		reverse=sort_order != "1",
	)

	for entry, _ in entries:
		list_directories(entry, sort_order, indent + 2)


def list_files_and_directories(
	path: Path, sort_order: bool = False, indent: int = 0
) -> None:
	"""List files and directories with sizes."""
	indentation = "  " * indent
	folder_size = get_folder_size(path)
	output = f"{indentation}{path.name} ({folder_size / (1024**2):.2f} MB)"
	print(output)

	entries = list(path.iterdir())
	directories = [entry for entry in entries if entry.is_dir()]
	files = [entry for entry in entries if entry.is_file()]

	if sort_order:
		directories.sort(key=lambda e: e.name)
		files.sort(key=lambda e: e.name)
	else:
		directories.sort(key=lambda e: get_folder_size(e), reverse=True)
		files.sort(key=lambda e: e.stat().st_size, reverse=True)

	for entry in directories:
		list_files_and_directories(entry, sort_order, indent + 2)

	for entry in files:
		file_size_mb = entry.stat().st_size / (1024**2)
		file_output = f"{indentation}  {entry.name} ({file_size_mb:.2f} MB)"
		print(file_output)


def rename_file_red(path: Path) -> None:
	"""Rename files with paths exceeding 180 characters for RED compatibility."""
	if not path.exists() or not path.is_dir():
		logger.error(f"Path does not exist: {path}")
		return

	root_path = path.parent
	renamed_count = 0

	for file in path.rglob("*"):
		relative_path_length = len(str(file.relative_to(root_path)))

		if relative_path_length > 180:
			new_length = (
				180 - (relative_path_length - len(file.name)) - len(file.suffix)
			)
			new_name = file.stem[:new_length] + file.suffix
			new_file_path = file.with_name(new_name)
			try:
				file.rename(new_file_path)
				renamed_count += 1
				logger.info(f"Renamed: {file.name}")
			except OSError as e:
				logger.error(
					f"Failed to rename {file.name}: {e.strerror} (errno {e.errno})"
				)

	logger.info(
		f"Renamed {renamed_count} files"
		if renamed_count
		else "No files needed renaming"
	)


def make_torrents(folder: Path) -> None:
	"""Create RED and OPS torrents for a folder."""
	logger.info(f"Creating torrents for {folder.name}")

	dropbox_info_path = Path.home() / "AppData" / "Local" / "Dropbox" / "info.json"

	if not dropbox_info_path.exists():
		raise FileNotFoundError("Dropbox info.json not found")

	try:
		data = json.loads(dropbox_info_path.read_text(encoding="utf-8"))
	except json.JSONDecodeError as e:
		raise ValueError(f"Malformed Dropbox info.json: {e}") from e
	dropbox_path = data.get("personal", {}).get("path")
	if not dropbox_path:
		raise ValueError("Dropbox path not found in info.json")
	dropbox = Path(dropbox_path)

	rename_file_red(folder)

	try:
		create_torrent(
			path=str(folder),
			trackers=[
				"https://home.opsfet.ch/7a0917ca5bbdc282de7f2eed00a69e2b/announce"
			],
			private=True,
			source="OPS",
			output=str(dropbox / "Lance" / f"{folder.name} - OPS.torrent"),
		)
	except (RuntimeError, ValueError, OSError) as e:
		logger.error(f"Failed to create OPS torrent for {folder.name}: {e}")
		raise FileOperationError(
			f"Failed to create OPS torrent for {folder.name}", folder
		) from e

	try:
		create_torrent(
			path=str(folder),
			trackers=[
				os.getenv(
					"TRACKER_OPS_ANNOUNCE",
					"https://home.opsfet.ch/7a0917ca5bbdc282de7f2eed00a69e2b/announce",
				),
				os.getenv(
					"TRACKER_FLACS_ANNOUNCE",
					"https://flacsfor.me/250f870ba861cefb73003d29826af739/announce",
				),
			],
			private=True,
			source="RED",
			output=str(dropbox / "Lance" / f"{folder.name} - RED.torrent"),
		)
	except (RuntimeError, ValueError, OSError) as e:
		logger.error(f"Failed to create RED torrent for {folder.name}: {e}")
		raise FileOperationError(
			f"Failed to create RED torrent for {folder.name}", folder
		) from e

	logger.info(f"Torrents created for {folder.name}")
