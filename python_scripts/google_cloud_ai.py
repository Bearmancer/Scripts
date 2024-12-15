import os
import google.generativeai as genai
from pathlib import Path
from argparse import ArgumentParser

def process_chunks(input_file: Path, output_dir: Path, model_name: str, chunk_size: int, instructions: str):
    api_key = os.getenv("GEMINI_API_KEY")
    genai.configure(api_key=api_key)
    model = genai.GenerativeModel(model_name)

    with input_file.open(encoding="utf-8") as file:
        lines = file.readlines()

    processed_content = []

    for i in range(0, len(lines), chunk_size):
        chunk = ''.join(lines[i:i + chunk_size])
        response = model.generate_content(f"{instructions}\n\n{chunk}")
        response_text = response.text.strip().splitlines()[1:-1]
        processed_content.append("\n".join(response_text))

    output_file = output_dir / f"Gemini CLI - {input_file.name}"
    output_file.write_text("\n".join(processed_content), encoding="utf-8")

    print(f"Processed {input_file.name} and saved to {output_file}")

def process_file(
        input_path: Path, 
        output_dir: Path = Path(r'C:\Users\Lance\Desktop\Gemini-CLI Output'), 
        model_name: str = "gemini-2.0-flash-exp", 
        chunk_size: int = 500, 
        instructions: str = "Translate subtitles to English whilst retaining the SRT formatting."
        ):
    
    if input_path.is_dir():
        for file in input_path.iterdir():
            if file.is_file():
                process_chunks(file, output_dir, model_name, chunk_size, instructions)
    else:
        process_chunks(input_path, output_dir, model_name, chunk_size, instructions)

if __name__ == "__main__":
    parser = ArgumentParser(description="Process files with Google Generative AI Gemini")

    parser.add_argument("-i", "--input", default=r"C:\Users\Lance\Desktop\Input", help="Path to the input file or directory")
    parser.add_argument("-o", "--output", default=r"C:\Users\Lance\Desktop\Gemini-CLI Output", help="Path to the output directory")
    parser.add_argument("-m", "--model", default="gemini-2.0-flash-exp", help="Model to use")
    parser.add_argument("-c", "--chunk-size", type=int, default=500, help="Size of chunks to split the input text (in lines)")
    parser.add_argument("-t", "--instructions", default="Translate subtitles to English whilst retaining the SRT formatting.", help="Instructions for the AI model")

    args = parser.parse_args()

    input_path = Path(args.input)
    output_dir = Path(args.output)
    output_dir.mkdir(parents=True, exist_ok=True)

    process_file(input_path, output_dir, args.model, args.chunk_size, args.instructions)