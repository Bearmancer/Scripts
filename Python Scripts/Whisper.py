import subprocess, sys, re
from pathlib import Path
from Misc import call_cmdlet_all_files

file_extensions = ['.mkv', '.mp4', '.mp3', '.flac', '.m4a', '.ogg', '.opus', '.wmv', '.ts', '.flv', '.avi']

def whisper_logic(file: Path, model, language):
    file = Path(file)

    if file.suffix not in file_extensions:
        return
    
    subtitle_file = file.with_suffix('.srt')

    if subtitle_file.exists():
        print(f"Subtitle for {file.stem} already exists. Skipping...")
        return
    
    print(f"Now transcribing: {file.name}")
    
    subprocess.run(['whisper', '--fp16', 'False', '--output_format', 'srt', '--model', model, '--language', language, str(file)])
    
    remove_subtitle_duplication(subtitle_file)

    if language == "Japanese":
        srt_to_word(subtitle_file)

def whisp(file):
    whisper_logic(file, "small.en", "English")

def whisper_path(directory):
    for file in directory.glob('*'):
        if file.is_file():
            whisp(file)

def whisper_path_recursive(directory):
    for subdir in directory.rglob('*'):
        if subdir.is_dir():
            whisper_path(subdir)
    whisper_path(directory)

def whisper_japanese(file):
    whisper_logic("small", "Japanese", file)

def whisper_path_japanese(directory):
    for file in directory.glob('*'):
        if file.is_file():
            whisper_japanese(file)

def whisper_japanese_file(file):
    call_cmdlet_all_files (file)

def remove_subtitle_duplication(file):
    old_text = r'(\d+\r?\n\d+.*?\r?\n(.*?))(?:\r?\n)+(?:\d+\r?\n\d+.*?\r?\n\2(?:\r?\n)+)'
    new_text = r'\1\n\n'
    
    if file.exists():
        with open(file, 'r', encoding='utf-8') as f:
            content = f.read()
        
        new_content = re.sub(old_text, new_text, content)
        
        with open(file, 'w', encoding='utf-8') as f:
            f.write(new_content)
    else:
        print(f"{file} not found.")

def srt_to_word(file):
    subprocess.run(['python', 'C:/Users/Lance/Documents/Powershell/Python Scripts/Word and SRT Conversions.py', str(file), 'srt'])

def word_to_srt(file):
    subprocess.run(['python', 'C:/Users/Lance/Documents/Powershell/Python Scripts/Word and SRT Conversions.py', str(file), 'docx'])

if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Invalid input entered.")
        exit

command = sys.argv[1]
path = Path(sys.argv[2])

commands = {
    "WhisperLogic": lambda: whisper_logic(path, sys.argv[3], sys.argv[4]),
    "Whisp": lambda: whisp(path),
    "WhisperPath": lambda: whisper_path(path),
    "WhisperPathRecursive": lambda: whisper_path_recursive(path),
    "WhisperJapanese": lambda: whisper_japanese(path),
    "WhisperPathJapanese": lambda: whisper_path_japanese(path),
    "WhisperJapaneseFile": lambda: whisper_japanese_file(path)
}

execute_command = commands.get(command)

if execute_command:
    execute_command()
else:
    print("Unknown command")
