import os
import warnings
import google.generativeai as genai
import absl.logging
from pathlib import Path
from argparse import ArgumentParser

warnings.filterwarnings("ignore")
absl.logging.set_verbosity(absl.logging.ERROR)
os.environ["GRPC_SHUTDOWN_GRACE_MS"] = "500"


def process_chunks(input_file: Path, model: genai.GenerativeModel, chunk_size: int, instructions: str):
    print(f"Now processing: {input_file}")

    with input_file.open(encoding="utf-8") as file:
        lines = file.readlines()

    processed_content = []

    total_chunks = (len(lines) + chunk_size - 1) // chunk_size
    
    for i in range(0, len(lines), chunk_size):
        chunk = lines[i:i + chunk_size]
        print(f"Processing chunk {i // chunk_size + 1} of {total_chunks}, starting from line {i+1}")
        response = model.generate_content(f"{instructions}\n\n{chunk}")
        processed_content.append("\n".join(response.text.strip().splitlines()[1:-1]))

    output_file_name = f"{input_file.stem} (Gemini CLI){input_file.suffix}"
    output_file = input_file.parent / output_file_name
    output_file.write_text("\n".join(processed_content), encoding="utf-8")

    print(f"Processed {input_file.name} and saved as {output_file.name}")
    
    return output_file


def process_file(input_path: Path, model_name: str = "gemini-2.0-flash-exp", chunk_size: int = 500, instructions: str = "Translate subtitles to English whilst retaining the SRT formatting."):
    api_key = os.getenv("GEMINI_API_KEY")
    genai.configure(api_key=api_key)
    model = genai.GenerativeModel(model_name)
    
    if input_path.is_dir():
        return [process_chunks(file, model, chunk_size, instructions) for file in input_path.iterdir() if file.is_file()]
    else:
        return [process_chunks(input_path, model, chunk_size, instructions)]


if __name__ == "__main__":
    parser = ArgumentParser(description="Process files with Google Generative AI Gemini")

    parser.add_argument("-i", "--input", required=True, help="Path to the input file or directory. Output will be saved in the same directory.")
    parser.add_argument("-m", "--model", default="gemini-2.0-flash-exp", help="Model to use")
    parser.add_argument("-c", "--chunk-size", type=int, default=500, help="Size of chunks to split the input text (in lines)")
    parser.add_argument("-t", "--instructions", default="Translate subtitles to English whilst retaining the SRT formatting.", help="Instructions for the AI model")

    args = parser.parse_args()

    input_path = Path(args.input)
    
    process_file(input_path, args.model, args.chunk_size, args.instructions)