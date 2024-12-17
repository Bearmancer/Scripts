import ffmpeg
import io
import random
import sys
import textwrap
from PIL import Image, ImageDraw, ImageFont
from pathlib import Path

def get_video_info(video_path):
    probe = ffmpeg.probe(str(video_path), v='error', select_streams='v:0', show_entries='format=duration,stream=width,height')
    video_info = {
        'duration': float(probe['format']['duration']),
        'width': int(probe['streams'][0]['width']),
        'height': int(probe['streams'][0]['height'])
    }
    return video_info


def add_timestamp(image, timestamp, font_path="calibri.ttf", font_size=20):
    draw = ImageDraw.Draw(image)
    font = ImageFont.truetype(font_path, font_size)
    timestamp_text = f"{int(timestamp // 3600):02}:{int((timestamp % 3600) // 60):02}:{int(timestamp % 60):02}"

    text_width, text_height = font.getbbox(timestamp_text)[2:]
    text_position = (image.width - text_width - 20, image.height - text_height - 20)
    draw.text(text_position, timestamp_text, font=font, fill=(255, 255, 255, 255))
    return image


def extract_frame(video_path, timestamp, video_info, target_width=None):
    input_stream = ffmpeg.input(str(video_path), ss=timestamp)
    if target_width:
        aspect_ratio = video_info['height'] / video_info['width']
        target_height = int(target_width * aspect_ratio)
        input_stream = input_stream.filter('scale', target_width, target_height)

    out, _ = input_stream.output('pipe:', vframes=1, format='image2', vcodec='mjpeg').run(capture_stdout=True, capture_stderr=True)
    img = Image.open(io.BytesIO(out))

    img = add_timestamp(img, timestamp)
    return img


def add_filename_to_header(draw, filename, header_size, image_width):
    font = ImageFont.truetype("calibri.ttf", 60)
    text_lines = textwrap.wrap(filename, width=40)

    draw.rectangle([(0, 0), (image_width, header_size)], fill=(240, 240, 240))

    y_offset = (header_size - (len(text_lines) * (font.size + 5))) // 2
    for line in text_lines:
        text_width, text_height = font.getbbox(line)[2:]
        text_position = ((image_width - text_width) // 2, y_offset)
        draw.text(text_position, line, font=font, fill=(0, 0, 0, 255))
        y_offset += text_height + 5


def create_thumbnail_grid(video_path, video_info, width=800, rows=8, columns=4):
    duration = video_info['duration']
    timestamps = [duration * i / (rows * columns) for i in range(rows * columns)]
    
    aspect_ratio = video_info['height'] / video_info['width']
    target_height = int(width * aspect_ratio)
    
    grid_width = width * columns
    grid_height = target_height * rows + 100 
    grid_image = Image.new('RGB', (grid_width, grid_height), (255, 255, 255, 255))
    draw = ImageDraw.Draw(grid_image)

    draw.rectangle([0, 0, grid_width, 100], fill=(255, 255, 255, 255))
    add_filename_to_header(draw, video_path.stem, 100, grid_width)

    for idx, timestamp in enumerate(timestamps):
        img = extract_frame(video_path, timestamp, video_info, width)
        if img:
            col = idx % columns
            row = idx // columns
            x = col * width
            y = 100 + row * target_height  
            grid_image.paste(img, (x, y))
    
    output_path = Path.home() / "Desktop" / f"{video_path.stem} - Thumbnails.jpg"

    grid_image.save(output_path)

    return timestamps


def save_full_size_images(video_path, video_info, thumbnail_timestamps):
    duration = video_info['duration']
    timestamps = []
    min_gap = 15 if duration > 1200 else 1

    while len(timestamps) < 6:
        timestamp = random.uniform(30, duration - 30)

        if all(abs(timestamp - thumb) >= min_gap for thumb in timestamps + thumbnail_timestamps):
            timestamps.append(timestamp)

    timestamps.sort()

    for idx, timestamp in enumerate(timestamps):
        img = extract_frame(video_path, timestamp, video_info)
        if img:
            output_path = Path.home() / "Desktop" / f"{video_path.stem} - Image {idx + 1}.jpg"
            img.save(output_path)


def extract_images(video_path):
    print(f"Now processing: {video_path}")

    if not video_path.is_file():
        raise ValueError("Invalid file path")

    video_info = get_video_info(video_path)

    if not video_info:
        raise ValueError("Could not retrieve video info")

    thumbnail_timestamps = create_thumbnail_grid(video_path, video_info)
    print("Thumbnails successfully generated.")

    save_full_size_images(video_path, video_info, thumbnail_timestamps)
    print("Full size images successfully generated.")
        

if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Invalid number of arguments entered.")
    
    video_file = Path(sys.argv[1])
    extract_images(video_file)