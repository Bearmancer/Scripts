from pathlib import Path
from subprocess import run
from threading import Lock
from datetime import datetime
from concurrent.futures import ThreadPoolExecutor

THREADS = 6
PARALLEL_LIMIT = 2
EXTENSIONS = {
    ".flac",
    ".m4a",
    ".aac",
    ".mp3",
    ".mp4",
    ".mkv",
    ".avi",
    ".wav",
    ".webm",
    ".opus",
    ".ogg",
    ".wma",
}

progress_counter = 0
total_files = 0
state_lock = Lock()


def whisper_logic(file, model, language):
    global progress_counter
    
    srt_path = file.with_suffix(".srt")

    if srt_path.exists():
        with state_lock:
            progress_counter += 1
            print(
                f"[{datetime.now():%H:%M:%S}] {progress_counter}/{total_files} - Skipped: {file.name}"
            )
        return file

    args = [
        "faster-whisper-xxl",
        "--device", "cpu",
        "--compute_type", "int8",
        "--threads", str(THREADS),
        "--output_dir", str(file.parent),
        "--model", model,
        *(["--language", language] if language is not None else []),
        str(file),
    ]

    run(args)

    with state_lock:
        progress_counter += 1
        print(
            f"[{datetime.now():%H:%M:%S}] {progress_counter}/{total_files} - Completed: {file.name}"
        )

    return file


def whisper_path(directory, language="en", model="distil-large-v3.5"):
    global total_files
    directory = Path(directory)
    audio_files = [
        f
        for f in directory.rglob("*")
        if f.is_file() and f.suffix.casefold() in EXTENSIONS
    ]
    total_files = len(audio_files)

    with ThreadPoolExecutor(max_workers=PARALLEL_LIMIT) as executor:
        for file in audio_files:
            executor.submit(whisper_logic, file, model, language)


def whisper_japanese(file_path):
    whisper_logic(file_path, model="medium", language="ja")


def process_japanese_directory(directory):
    whisper_path(directory, model="medium", language="ja")