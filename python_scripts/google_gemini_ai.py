import chardet
import langid
import os
import time
from pathlib import Path
from argparse import ArgumentParser
import google.generativeai as genai

os.environ['GRPC_VERBOSITY'] = 'NONE'


def process_file(input_file: Path, model_name: str = "gemini-2.0-flash-exp", chunk_size: int = 500,
                 match_lines: bool = "False", instructions: str = ""):
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

    output = None

    for _ in range(5):
        output = process_chunks(lines, chunk_size, instructions, model)
        if output: break

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
        if 'finish_reason' in str(e) and '4' in str(e):
            print("Error 4 occurred: Finish reason 4")
            log_to_file("Error 4: " + str(e))
       
        else:
            print(f'Error occurred: {e}')
            log_to_file(str(e))
            time.sleep(1000)
        
        return None


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
    parser.add_argument("-c", "--chunk-size", type=int, default=200, help="Lines per chunk")
    parser.add_argument("--match_lines", type=bool, default=False, help="Match the number of input and output lines.")
    parser.add_argument("-t", "--instructions", default=""" 
    You are tasked with rewriting each line in a text file containing file names. Follow these rules:
    VERY IMPORTANT: DO NOT GET RID OF ANY TEXT. DO NOT REMOVE INFORMATION.
    Very Important: Translate all foreign languages to English.
    Very Important: Replace ∙, :, ;, /, ⁄, ¦, –, -, _ with spaces (except in names like Rimsky-Korsakov or hr-sinfonieorchester). Don't remove ( or ).
    Very Important: Always keep years and reformat dates (e.g., "2020/11" becomes "2020-11"). Put all dates after the name of the piece.
    Start with the composer's last name (and remove first name when applicable.)
    Convert all-caps to title case (keep acronyms like BBC and small words like "for").
    Replace double quotes with single quotes.
    Replace "n°", "N. " and "Nº" with "No."
    Use composer names' English transliterations only (e.g., "Tchaikovsky" not "Chiakowsky").
    Add "No." to numbered works (e.g., "Symphony 6" becomes "Symphony No. 6").
    Expand abbreviations (e.g., "PC" to "Piano Concerto").
    Trim extra spaces and standardize formatting.
    """)

    args = parser.parse_args()
    input_path = Path(args.input)

    if input_path.is_file():
        files = [input_path]
    else:
        files = input_path.rglob('*.[txt][srt]')

    for file in files:
        print(f"Processing file: {file}")
        process_file(file, args.model, args.chunk_size, args.match_lines, args.instructions)


if __name__ == "__main__":
    main()