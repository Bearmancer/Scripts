import subprocess
from pathlib import Path

def get_image_size(folder: Path):
    exif_tool = Path(r"C:\Users\Lance\Desktop\exiftool-12.96_64\exiftool.exe")
    problematic_files = []

    for flac_file in folder.glob('*.flac'):
        image_size = subprocess.run([exif_tool, '-PictureLength', '-s', '-s', '-s', str(flac_file)], 
                                     capture_output=True, text=True)

        image_size_kb = round(int(image_size.stdout.strip())/1024, 2)

        if image_size_kb > 1024:
            print(f"{flac_file} embedded image size is: {image_size_kb} KB")
            problematic_files.append(image_size_kb)
        
    output = f"Files larger than 1MB:\nfilelist:\"{'|'.join(str(file) for file in problematic_files)}\""

    print(output)
    
    with (Path.home()/"Desktop" / "problematic_files.txt").open('w') as file: 
        file.write(output.strip())

if __name__ == "__main__":
    folder_path = Path(input("Please provide a directory path: "))
    if folder_path.exists() and folder_path.is_dir():
        sizes, problems = get_image_size(folder_path)
        print(f"Image sizes greater than 1MB: {sizes}")
        if problems:
            print("Problematic files:")
            for file in problems:
                print(file)
    else:
        print("Invalid directory path.")