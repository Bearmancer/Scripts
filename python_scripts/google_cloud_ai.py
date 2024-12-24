import chardet
import langid
import os
import time
from pathlib import Path
from argparse import ArgumentParser
import google.generativeai as genai

os.environ['GRPC_VERBOSITY'] = 'NONE'


def process_file(input_file: Path, model_name: str = "gemini-2.0-flash-exp", chunk_size: int = 500,
                 match_lines: bool = "False", instructions: str = "", ):
    genai.configure(api_key=os.getenv("GEMINI_API_KEY"))
    model = genai.GenerativeModel(model_name)
    lines = read_file_content(input_file)

    output_file = input_file.with_name(f"{input_file.stem} (Gemini CLI){input_file.suffix}")

    if input_file.stem.endswith("(Gemini CLI)") or output_file.exists():
        print(f'Translated file already exists for {input_file.name}.\n--------------------')
        return

    if langid.classify(''.join(lines))[0] == "en":
        output_file.write_text("\n".join(lines), encoding="utf-8")
        print(f"File already in English: {input_file}.\n--------------------")
        return output_file

    output = process_chunks(lines, chunk_size, instructions, model)

    attempts = 0

    while match_lines and len(lines) != len(output) and attempts < 5:
        print(f"Line count mismatch. {len(lines)} in | {len(output)} out")
        output = process_chunks(lines, chunk_size, instructions, model)
        attempts += 1

    output_file.write_text('\n'.join(output), encoding="utf-8")
    print(f"Successfully translated: {input_file.name}\n--------------------")

    time.sleep(10)

    return output_file


def process_chunks(lines, chunk_size, instructions, model):
    try:
        chunks = [lines[i:i + chunk_size] for i in range(0, len(lines), chunk_size)]
        output = []
        for i, chunk_lines in enumerate(chunks, 1):
            print(f"Now processing chunk {i} of {len(chunks)}")
            chunk_text = '\n'.join(chunk_lines)
            output = model.generate_content(f"{instructions}\n\n{chunk_text}").text.splitlines()
        return output

    except Exception as e:
        print(f'Error occurred: {e}')
        log_to_file(e)


def read_file_content(input_file):
    try:
        return [line for line in input_file.read_text(encoding="utf-8").splitlines()]
    except UnicodeDecodeError:
        detected_encoding = chardet.detect(input_file.read_bytes())["encoding"]
        print(f"Detected encoding: {detected_encoding}")
        return [line for line in input_file.read_text(encoding=detected_encoding).splitlines()]


def log_to_file(message):
    log = Path.home() / 'Desktop' / 'failed_files_log.txt'
    with log.open(mode='a', encoding='utf-8') as f:
        f.write(message)


def main():
    parser = ArgumentParser(description="Translate text files using Google's Gemini AI")
    parser.add_argument("-i", "--input", required=True, help="Input file or directory path")
    parser.add_argument("-m", "--model", default="gemini-2.0-flash-exp", help="Gemini model name")
    parser.add_argument("-c", "--chunk-size", type=int, default=500, help="Lines per chunk")
    parser.add_argument("--match_lines", type=bool, default=True, help="Match the number of input and output lines.")
    parser.add_argument("-t", "--instructions", default="""
       Translate to English and replace the foreign text. Do not lose any lines! Do not insert any comments. 
       Just translate the text. Retain all info ESPECIALLY DATES THIS IS VERY IMPORTANT! 
       If the translation exists along with original language in the original text then retain both.
   """)

    args = parser.parse_args()
    input_path = Path(args.input)
    files = input_path.rglob("*.txt")

    for file in files:
        print(f"Processing file: {file}")
        process_file(file, args.model, args.chunk_size, args.match_lines, args.instructions)


if __name__ == "__main__":
    main()