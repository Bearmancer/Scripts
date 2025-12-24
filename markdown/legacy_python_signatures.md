# Python Legacy Toolkit Signatures

## Module: `toolkit.audio`

### Functions
#### `calculate_gain`
```python
def calculate_gain(dff_file: pathlib.Path, target_headroom_db: float = -0.5) -> float:
```
> Calculate gain adjustment needed for target headroom.

#### `calculate_image_size`
```python
def calculate_image_size(path: pathlib.Path) -> None:
```
> Report FLAC files with embedded artwork larger than 1MB.

#### `convert_audio`
```python
def convert_audio(current_step: int, directory: pathlib.Path, fmt: str = 'all') -> None:
```
> Convert FLAC files to various sample rates and bit depths.

#### `convert_dff_to_flac`
```python
def convert_dff_to_flac(dff_dir: pathlib.Path) -> None:
```
> Convert DFF files with CUE sheet to FLAC.

#### `convert_iso_to_dff_and_cue`
```python
def convert_iso_to_dff_and_cue(iso_path: pathlib.Path, base_dir: pathlib.Path, disc_number: int) -> list[pathlib.Path]:
```
> Extract stereo and/or multichannel audio from SACD ISO.

#### `convert_to_mp3`
```python
def convert_to_mp3(directory: pathlib.Path) -> None:
```
> Convert all FLAC files in directory to 320kbps MP3.

#### `create_output_directory`
```python
def create_output_directory(directory: pathlib.Path, suffix: str) -> pathlib.Path:
```
> Create output directory with suffix, copying files using robocopy.

#### `downsample_flac`
```python
def downsample_flac(file: pathlib.Path, tier: tuple[int, int]) -> None:
```
> Downsample a single FLAC file using SoX.

#### `flac_directory_conversion`
```python
def flac_directory_conversion(directory: pathlib.Path, tier: tuple[int, int]) -> None:
```
> Convert all FLAC files in directory to specified tier.

#### `get_flac_tiers`
```python
def get_flac_tiers(sample_rate: int, bit_depth: int, fmt: str = 'all') -> list[tuple[int, int]]:
```
> Determine which FLAC tiers to convert to based on source format.

#### `get_metadata`
```python
def get_metadata(file: pathlib.Path) -> dict[str, str | None]:
```
> Extract audio metadata from a file using ffprobe.

#### `prepare_directory`
```python
def prepare_directory(directory: pathlib.Path) -> pathlib.Path:
```
> Sanitize filenames and normalize disc folder names.

#### `process_sacd_directory`
```python
def process_sacd_directory(directory: pathlib.Path, fmt: str = 'all') -> None:
```
> Extract and convert all SACD ISO files in a directory.

#### `progress_indicator`
```python
def progress_indicator(step: int, message: str) -> None:
```
> Print a step indicator with terminal-width borders.

#### `rename_file_red`
```python
def rename_file_red(path: pathlib.Path) -> None:
```
> Rename files with paths exceeding 180 characters for RED compatibility.

---

## Module: `toolkit.cli`

### Functions
#### `audio_art_report`
```python
def audio_art_report(directory: Annotated[pathlib.Path, <typer.models.OptionInfo object at 0x000002766A9F85F0>] = WindowsPath('.')) -> None:
```
> Report embedded artwork sizes in FLAC files.

#### `audio_convert`
```python
def audio_convert(directory: Annotated[pathlib.Path, <typer.models.OptionInfo object at 0x000002766A9F8380>] = WindowsPath('.'), mode: Annotated[str, <typer.models.OptionInfo object at 0x000002766A9F83E0>] = 'convert', format: Annotated[str, <typer.models.OptionInfo object at 0x000002766A9F8470>] = 'all') -> None:
```
> Convert audio files to various formats or extract SACD ISOs.

#### `audio_rename`
```python
def audio_rename(directory: Annotated[pathlib.Path, <typer.models.OptionInfo object at 0x000002766A9F8530>] = WindowsPath('.')) -> None:
```
> Rename files with excessively long paths.

#### `filesystem_torrents`
```python
def filesystem_torrents(directory: Annotated[pathlib.Path, <typer.models.OptionInfo object at 0x000002766A9F8FE0>] = WindowsPath('.'), include_subdirectories: Annotated[bool, <typer.models.OptionInfo object at 0x000002766A9F9070>] = False) -> None:
```
> Create RED and OPS torrents for directory.

#### `filesystem_tree`
```python
def filesystem_tree(directory: Annotated[pathlib.Path, <typer.models.OptionInfo object at 0x000002766A9F8E00>] = WindowsPath('.'), sort: Annotated[str, <typer.models.OptionInfo object at 0x000002766A9F8E90>] = 'size', include_files: Annotated[bool, <typer.models.OptionInfo object at 0x000002766A9F8F20>] = False) -> None:
```
> List directory tree with sizes.

#### `lastfm_update`
```python
def lastfm_update() -> None:
```
> Update Last.fm scrobbles to Google Sheets.

#### `main`
```python
def main() -> None:
```
#### `video_chapters`
```python
def video_chapters(path: Annotated[pathlib.Path, <typer.models.OptionInfo object at 0x000002766A9F88C0>] = WindowsPath('.')) -> None:
```
> Extract chapters from video files.

#### `video_compress`
```python
def video_compress(directory: Annotated[pathlib.Path, <typer.models.OptionInfo object at 0x000002766A9F8800>] = WindowsPath('.')) -> None:
```
> Batch compress MKV files using HandBrake.

#### `video_gif`
```python
def video_gif(input: Annotated[pathlib.Path, <typer.models.OptionInfo object at 0x000002766A9F8A40>], start: Annotated[str, <typer.models.OptionInfo object at 0x000002766A9F8A70>] = '00:00', duration: Annotated[int, <typer.models.OptionInfo object at 0x000002766A9F8B60>] = 30, max_size: Annotated[int, <typer.models.OptionInfo object at 0x000002766A9F8BF0>] = 300, output: Annotated[pathlib.Path, <typer.models.OptionInfo object at 0x000002766A9F8C80>] = WindowsPath('C:/Users/Lance/Desktop')) -> None:
```
> Create optimized GIF from video file.

#### `video_remux`
```python
def video_remux(path: Annotated[pathlib.Path, <typer.models.OptionInfo object at 0x000002766A9F86B0>] = WindowsPath('.'), skip_mediainfo: Annotated[bool, <typer.models.OptionInfo object at 0x000002766A9F8740>] = False) -> None:
```
> Remux DVD/Blu-ray discs to MKV.

#### `video_resolutions`
```python
def video_resolutions(path: Annotated[pathlib.Path, <typer.models.OptionInfo object at 0x000002766A9F8980>] = WindowsPath('.')) -> None:
```
> Print resolution information for video files.

#### `video_thumbnails`
```python
def video_thumbnails(path: Annotated[pathlib.Path, <typer.models.OptionInfo object at 0x000002766A9F8D40>]) -> None:
```
> Extract thumbnail grid and full-size images from video.

---

## Module: `toolkit.cuesheet`

### Functions
#### `calculate_track_durations`
```python
def calculate_track_durations(tracks: list[toolkit.cuesheet.TrackInfo], cue_file: pathlib.Path) -> list[toolkit.cuesheet.TrackInfo]:
```
> Calculate duration for each track based on start times and total file duration.

#### `extract_track_data`
```python
def extract_track_data(cue_data: Any) -> list[toolkit.cuesheet.TrackInfo]:
```
> Extract track information from parsed CUE data.

#### `parse_cue_file`
```python
def parse_cue_file(cuefile_path: pathlib.Path) -> Any:
```
> Parse a CUE file with automatic encoding detection.

#### `process_cue_file`
```python
def process_cue_file(cue_file: pathlib.Path, volume_adjustment: float = 0.0) -> None:
```
> Process a CUE file: parse, extract tracks, and convert to FLAC.

#### `process_tracks`
```python
def process_tracks(tracks: list[toolkit.cuesheet.TrackInfo], cue_file: pathlib.Path, volume_adjustment: float = 0.0) -> None:
```
> Extract individual FLAC files from CUE sheet with volume adjustment.


### Classes
#### `class TrackInfo`
> Represents a single track parsed from a CUE sheet.

Methods:
- `__init__(self, file: str, parent: str, title: str, track_num: int, start_sec: float, duration: float | None = None, metadata: dict[str, str] = <factory>) -> None`

---

## Module: `toolkit.filesystem`

### Functions
#### `get_folder_size`
```python
def get_folder_size(path: pathlib.Path) -> int:
```
> Calculate total size of all files in a directory recursively.

#### `list_directories`
```python
def list_directories(path: pathlib.Path, sort_order: str = '0', indent: int = 0) -> None:
```
> List directories with sizes, sorted by size or name.

#### `list_files_and_directories`
```python
def list_files_and_directories(path: pathlib.Path, sort_order: bool = False, indent: int = 0) -> None:
```
> List files and directories with sizes.

#### `make_torrents`
```python
def make_torrents(folder: pathlib.Path) -> None:
```
> Create RED and OPS torrents for a folder.

#### `rename_file_red`
```python
def rename_file_red(path: pathlib.Path) -> None:
```
> Rename files with paths exceeding 180 characters for RED compatibility.

#### `run_command`
```python
def run_command(cmd: list[str], cwd: str | None = None) -> tuple[str, str]:
```
> Run a subprocess command and return stdout/stderr.

---

## Module: `toolkit.lastfm`

### Functions
#### `authenticate_google_sheets`
```python
def authenticate_google_sheets() -> gspread.client.Client:
```
> Authenticate with Google Sheets using stored credentials.

#### `get_last_scrobble_timestamp`
```python
def get_last_scrobble_timestamp(sheet: gspread.worksheet.Worksheet) -> datetime.datetime:
```
> Get the timestamp of the most recent scrobble in the sheet.

#### `prepare_track_data`
```python
def prepare_track_data(tracks: list[pylast.PlayedTrack]) -> list[list[str]]:
```
> Convert track objects to row data for the sheet.

#### `update_scrobbles`
```python
def update_scrobbles() -> None:
```
> Fetch new scrobbles from Last.fm and add them to Google Sheets.

---

## Module: `toolkit.video`

### Functions
#### `add_filename_to_header`
```python
def add_filename_to_header(draw: PIL.ImageDraw.ImageDraw, filename: str, header_size: int, image_width: int) -> None:
```
> Add filename header to thumbnail grid.

#### `add_timestamp`
```python
def add_timestamp(image: PIL.Image.Image, timestamp: float, font_path: str = 'calibri.ttf', font_size: int = 20) -> PIL.Image.Image:
```
> Add timestamp overlay to image.

#### `batch_compression`
```python
def batch_compression(path: pathlib.Path) -> None:
```
> Batch compress MKV files using HandBrake.

#### `convert_disc_to_mkv`
```python
def convert_disc_to_mkv(file: pathlib.Path, dvd_folder: pathlib.Path) -> None:
```
> Convert disc to MKV using MakeMKV CLI.

#### `create_gif`
```python
def create_gif(input_path: pathlib.Path, start: str, duration: int, output_path: pathlib.Path, fps: float, scale: int) -> float:
```
> Create a single GIF and return its size in MiB.

#### `create_gif_optimized`
```python
def create_gif_optimized(input_path: pathlib.Path, start: str, duration: int, max_size: int, output_dir: pathlib.Path) -> None:
```
> Create optimized GIF with automatic quality reduction to meet size target.

#### `create_thumbnail_grid`
```python
def create_thumbnail_grid(video_path: pathlib.Path, video_info: toolkit.video.VideoInfo, width: int = 800, rows: int = 8, columns: int = 4) -> list[int]:
```
> Create a grid of thumbnails from video.

#### `extract_chapters`
```python
def extract_chapters(video_files: list[pathlib.Path]) -> None:
```
> Extract individual chapters from video files.

#### `extract_frame`
```python
def extract_frame(video_path: pathlib.Path, timestamp: float, video_info: toolkit.video.VideoInfo, target_width: int | None = None) -> PIL.Image.Image:
```
> Extract a single frame from video at specified timestamp.

#### `extract_images`
```python
def extract_images(video_path: pathlib.Path) -> None:
```
> Extract thumbnail grid and full-size images from video.

#### `get_mediainfo`
```python
def get_mediainfo(video_path: pathlib.Path) -> None:
```
> Get MediaInfo and copy to clipboard.

#### `get_video_info`
```python
def get_video_info(video_path: pathlib.Path) -> toolkit.video.VideoInfo:
```
> Get comprehensive video info (duration, width, height).

#### `get_video_info_for_gif`
```python
def get_video_info_for_gif(input_path: pathlib.Path) -> tuple[float, int]:
```
> Get FPS and width for GIF creation.

#### `get_video_resolution`
```python
def get_video_resolution(filepath: pathlib.Path) -> dict[str, int] | None:
```
> Get video resolution using ffprobe.

#### `print_video_resolution`
```python
def print_video_resolution(video_files: list[pathlib.Path]) -> None:
```
> Print resolution information for video files, grouped by HD status.

#### `remux_disc`
```python
def remux_disc(path: pathlib.Path, fetch_mediainfo: bool = True) -> None:
```
> Remux DVD/Blu-ray discs to MKV using MakeMKV.

#### `save_full_size_images`
```python
def save_full_size_images(video_path: pathlib.Path, video_info: toolkit.video.VideoInfo, thumbnail_timestamps: list[int]) -> None:
```
> Save random full-size images from video, excluding thumbnail timestamps.


### Classes
#### `class VideoInfo`

---

