import subprocess, sys
from pathlib import Path

def main(folder: Path):
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
    if sys.arg[len] > 1:
        main(Path(sys.arg[1]))
    else:
        print("Invalid number of arguments supplied.")