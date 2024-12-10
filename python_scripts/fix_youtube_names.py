from pathlib import Path
import re

def log_error(log_message):
    log_file_path = Path.home() / 'Desktop' / 'Files That Could Not Be Renamed.txt'
    with open(log_file_path, 'a', encoding='utf-8') as log_file:
        log_file.write(log_message)


def rename_files(directory, txt_file):
    try:
        directory = Path(directory)
        if not directory.exists():
            raise FileNotFoundError(f"Directory {directory} not found.")
        
        txt_file = Path(txt_file)
        with txt_file.open('r', encoding='utf-8') as file:
            lines = file.readlines()
        
        files = list(directory.iterdir())
        
        if len(files) != len(lines):
            raise ValueError(f"Mismatch: {len(files)} files but {len(lines)} lines in {txt_file}.")
        
        for file, new_name in zip(files, lines):
            new_name = new_name.strip()
            sanitized_new_name = re.sub(r'[<>:"/\\|?*]', ' ', new_name)
            new_path = directory / sanitized_new_name
            
            if new_path == file:
                continue

            if new_path.exists():
                log_message = f"Cannot rename {file.name} to {sanitized_new_name} as it already exists.\n"
                print(log_message)
                log_error(log_message)
                
            try:
                file.rename(new_path)
                print(f"Renamed: {file.name} to {sanitized_new_name}")
            except OSError as e:
                try:
                    sanitized_file_name = re.sub(r'[<>:"/\\|?*]', ' ', file.name)
                    sanitized_path = file.parent / sanitized_file_name
                    file.rename(sanitized_path)
                    print(f"Renamed with sanitized name: {file.name} to {sanitized_file_name}")
                except OSError as e:
                    log_message = f"Exception: {e}\n"
                    print(f"Exception: {e}")
                    log_error(log_message)
                    continue
    except Exception as e:
        print(f"Exception: {e}")
        log_error(str(e))


def process_subfolders(base_directory, txt_directory):
    for subfolder in Path(base_directory).iterdir():
        if subfolder.is_dir():
            name = subfolder.name
            txt_file = Path(txt_directory) / f"{name}.txt"
            if txt_file.exists():
                print(f"Processing {name}...")
                rename_files(subfolder, txt_file)
            else:
                print(f"Warning: Text file for {name} does not exist. Now creating list of files...")


def main():
    base_directory = Path.home() / 'Desktop' / 'Done'
    txt_directory = Path.home() / 'Desktop' / 'Gemini-CLI'
    process_subfolders(base_directory, txt_directory)


if __name__ == "__main__":
    main()