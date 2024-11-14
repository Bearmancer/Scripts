import sys
import zipfile
from pathlib import Path


def get_size(item: Path) -> int:
    if item.is_dir():
        return sum(f.stat().st_size for f in item.rglob('*') if f.is_file())
    return item.stat().st_size 


def create_zip(folders, zip_name):
    with zipfile.ZipFile(zip_name, 'w', zipfile.ZIP_DEFLATED) as zipf:
        for folder in folders:
            for item in folder.rglob('*'):
                if item.is_file():
                    arcname = item.relative_to(folder.parent)
                    zipf.write(item, arcname)


def main(directory: Path):
    max_size = 2 * 1024**3 
    zip_total = 0
    items_zipped = []
    starting_folder = None
    last_folder = None

    items = list(directory.iterdir()) 

    for idx, item in enumerate(items):
        item_size = get_size(item) 

        if starting_folder is None:
            starting_folder = item.name
        
        print(f'Adding "{item.name}" with a size of {item_size / (1024 * 1024):.2f} MiB.')

        if item_size > max_size:
            main(item)
        
        if zip_total + item_size >= max_size or idx == len(items) - 1:
            print(f"Current size of folders totalled: {zip_total / (1024 * 1024):.2f} MiB")

            zip_name = directory / f'{directory.name} - {starting_folder} to {last_folder}.zip'
            create_zip(items_zipped, zip_name)

            print(f"\n{zip_name} successfully created.\n")

            items_zipped = [item]
            zip_total = item_size
            starting_folder = item.name

        else:
            items_zipped.append(item)
            if "Disc" in item.name:
                last_folder = item.name
            zip_total += item_size


if __name__ == "__main__":
    if(len(sys.argv) < 2):
        print("Invalid number of arguments entered.")

    directory = Path(sys.argv[1])

    process_all_subfolders = sys.argv[2].lower() == 'true' if len(sys.argv) > 2 else True
    directories_to_process = directory.iterdir() if process_all_subfolders else [directory]

    for subdir in directories_to_process:
        if subdir.is_dir():
            main(subdir)