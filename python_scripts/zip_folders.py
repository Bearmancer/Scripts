import argparse
from pathlib import Path
import zipfile

def zip_up_to_size(file_iter, output_dir, base_name, base_dir):
    max_size = 2048
    part_num = 1
    current_size = 0
    zf = None

    for file in file_iter:
        if zf is None:
            zip_filename = output_dir / f"{base_name} - Part {str(part_num).zfill(2)}.zip"
            zf = zipfile.ZipFile(zip_filename, 'w', compression=zipfile.ZIP_STORED, allowZip64=True)
        file_size = file.stat().st_size / (1024 * 1024)
        if current_size + file_size > max_size:
            zf.close()
            part_num += 1
            zip_filename = output_dir / f"{base_name} - Part {str(part_num).zfill(2)}.zip"
            zf = zipfile.ZipFile(zip_filename, 'w', compression=zipfile.ZIP_STORED, allowZip64=True)
            current_size = 0
        zf.write(str(file), str(file.relative_to(base_dir)))
        current_size += file_size

    if zf is not None:
        zf.close()

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--directory', nargs='?', default='.')
    parser.add_argument('--process_subdirectories', dest='process_subdirectories', action='store_true', default=True)
    args = parser.parse_args()

    input_dir = Path(args.directory).resolve()
    output_dir = Path.home() / 'Desktop' / 'ZIP Files'
    output_dir.mkdir(parents=True, exist_ok=True)

    if args.process_subdirectories:
        for item in input_dir.iterdir():
            if item.is_dir():
                all_files = (f for f in item.rglob('*') if f.is_file())
                zip_up_to_size(all_files, output_dir, item.name, base_dir=item)

        files_in_root = (f for f in input_dir.iterdir() if f.is_file())

        try:
            first_file = next(files_in_root)
            files_in_root = (f for f in [first_file] + list(files_in_root))
            zip_up_to_size(files_in_root, output_dir, input_dir.name, base_dir=input_dir)
        except StopIteration:
            pass
    else:
        all_files = (f for f in input_dir.rglob('*') if f.is_file())
        zip_up_to_size(all_files, output_dir, input_dir.name, base_dir=input_dir)

if __name__ == '__main__':
    main()