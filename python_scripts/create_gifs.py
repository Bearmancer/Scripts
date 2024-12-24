import argparse
import ffmpeg
from pathlib import Path

def get_video_info(input_path: Path):
    try:
        probe = ffmpeg.probe(str(input_path))
        video_stream = next(
            (stream for stream in probe['streams'] if stream['codec_type'] == 'video'), None
        )

        if not video_stream:
            raise ValueError("No video stream found.")

        fps = eval(video_stream['r_frame_rate'])
        width = int(video_stream['width'])

        return fps, width
    except Exception as e:
        print(f"Error retrieving video info: {e}")
        return 10, 320

def create_gif(input_path: Path, start: str, duration: int, output_path: Path, fps: float, scale: int):
    print('Creating GIF with parameters:')
    print(f'  FPS: {fps}')
    print(f'  Scale width: {scale}')
    print(f'  Output path: {output_path}')

    try:
        (
            ffmpeg
            .input(str(input_path), ss=start, t=duration)
            .filter('fps', fps=fps)
            .filter('scale', scale, -1, flags='lanczos')
            .output(str(output_path), format='gif', gifflags='+transdiff', y=True)
            .run(quiet=True)
        )

        size = output_path.stat().st_size / (1024 * 1024)
        print(f"GIF created at {output_path} with size {size:.2f} MiB")
        return size
    except ffmpeg.Error as e:
        print(f"Error creating GIF: {e}")
        return float('inf')

def main():
    parser = argparse.ArgumentParser(description='Create a GIF from a video file.')
    parser.add_argument('-i', '--input_path', type=Path, required=True, help='Path to the input video file')
    parser.add_argument('-s', '--start', default='00:00', help='Start time (mm:ss)')
    parser.add_argument('-d', '--duration', type=int, default=30, help='Duration in seconds')
    parser.add_argument('-m', '--max_size', type=float, default=300, help='Maximum GIF size in MiB')
    parser.add_argument('-o', '--output_dir', type=Path, default=Path.home() / 'Desktop', help='Directory to save the GIF')

    args = parser.parse_args()

    input_path = args.input_path
    start = args.start
    duration = args.duration
    max_size = args.max_size
    output_dir = args.output_dir

    if not input_path.is_file():
        print(f"Error: The input file {input_path} does not exist.")
        return

    output_dir.mkdir(parents=True, exist_ok=True)
    base_name = input_path.stem

    original_fps, original_width = get_video_info(input_path)
    print(f"Original FPS: {original_fps}")
    print(f"Original Resolution Width: {original_width}")

    fps = original_fps
    scale = original_width
    min_fps = 10
    min_scale = 160

    while True:
        timestamp = start.replace(":", "")
        output_name = f'{base_name} - {timestamp} - {duration}.gif'
        output_path = output_dir / output_name

        size = create_gif(input_path, start, duration, output_path, fps, scale)

        if size <= max_size:
            print('Desired GIF size achieved.')
            break
        else:
            print('GIF size too large, compressing further...')
            if fps > min_fps:
                fps -= 1
                print(f'Reducing FPS to {fps}')
            elif scale > min_scale:
                scale = max(scale - 40, min_scale)
                print(f'Reducing scale to {scale}')
            else:
                print('Cannot compress further without significant quality loss.')
                break

    print('GIF creation process completed.')

if __name__ == '__main__':
    main()