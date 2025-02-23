import chardet
import langid
import os
import time
import deepl
from pathlib import Path
from argparse import ArgumentParser
import google.generativeai as genai

os.environ['GRPC_VERBOSITY'] = 'NONE'


def process_file(input_file: Path, model_name: str = "gemini-2.0-flash", chunk_size: int = 200,
                 instructions: str = "", match_lines: bool = False):
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

    output = []

    chunks = [lines[i:i + chunk_size] for i in range(0, len(lines), chunk_size)]  

    for i, chunk_lines in enumerate(chunks, 1):
        print(f"Processing chunk {i} of {len(chunks)}...")
        
        while not (response := process_chunk(chunk_lines, instructions, model, match_lines)):
            print(f"Response was None. Retrying chunk {i}...")
            time.sleep(60)

        time.sleep(4)
        
        output.append('\n'.join(response))

    output_file.write_text('\n'.join(output), encoding="utf-8")
    print(f"Successfully translated: {input_file.name}\n--------------------")

    time.sleep(10)

    return output_file

def process_chunk(chunk_lines, instructions, model, match_lines: bool = False):
    try:
        response = model.generate_content(f"{instructions}\n\n{chunk_lines}").text.strip().splitlines()
        response = [line for line in response if "```" not in line and line and line != "[" and line != "]"]

        if match_lines:
            print(f"Input lines: {len(chunk_lines)}, Output lines: {len(response)}")

            while len(chunk_lines) != len(response):
                print(f"Lines count does not match. Length of input: {len(chunk_lines)}, length of output: {len(response)}")
                for idx, line in enumerate(response, start=1):
                    print(f"{idx}. {line}")
                response = process_chunk(chunk_lines, instructions, model, match_lines)

        return response

    except Exception as e:
        if 'finish_reason' in str(e) and '4' in str(e):
            print("Error 4 occurred: Finish reason 4")
            translated_text = deepl.Translator(os.getenv("DEEPL_API_KEY")).translate_text("\n".join(chunk_lines), target_lang='EN-US').text
            return translated_text.splitlines()
       
        else:
            print(f'Error occurred: {e}')
            log_to_file(str(e))
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
    parser.add_argument("-m", "--model", default="gemini-2.0-flash", help="Gemini model name")
    parser.add_argument("-c", "--chunk-size", type=int, default=200, help="Lines per chunk")
    parser.add_argument("--match_lines", type=bool, default=False, help="Match the number of input and output lines.")
    parser.add_argument("-t", "--instructions", default="""
    TASK: Rewrite each line in a text file that contains file names. DO NOT DELETE LINES AND DO NOT OUTPUT CODE. Follow these guidelines:

    1. VERY IMPORTANT: PRESERVE ALL TEXT AND INFORMATION INCLUDING INFORMATION INSIDE PARENTHESIS LIKE BIT RATE AND INFORMATION. DO NOT DELETE ANY INFORMATION. ONLY PRINT FILE NAMES IN OUTPUT. Don't add any commas.
    2. ALWAYS TRANSLATE FOREIGN TITLES TO ENGLISH.
    3. Replace the following symbols with spaces: ∙, :, ;, /, ⁄, ¦, –, -. However, retain these symbols in names like "Rimsky-Korsakov" or "hr-sinfonieorchester". Do not remove parentheses ().
    4. Retain all years and reformat dates to "YYYY-MM" format. Ensure dates appear after the piece's name if they are initially at the start.
    5. Start file names with the composer's last name when possible.
    6. Convert all-uppercase text to title case, except for acronyms (e.g., BBC) and smaller words such as "for" and "the".
    7. Replace double quotes with single quotes.
    8. Convert "N°", "N. ", and "Nº" to "No.".
    9. Use English transliterations of composer names (e.g., "Tchaikovsky" instead of "Chaikowsky").
    10. Add "No." to numbered works (e.g., "Symphony 6" becomes "Symphony No. 6").
    11. Expand abbreviations (e.g., "PC" to "Piano Concerto").
    12. Remove extra spaces and standardize formatting.
    """)

    args = parser.parse_args()
    input_path = Path(args.input)

    if input_path.is_file():
        files = [input_path]
    else:
        files = list(input_path.rglob('*.txt')) + list(input_path.rglob('*.srt'))

    for file in files:
        print(f"Processing file: {file}")
        process_file(file, args.model, args.chunk_size, args.instructions, args.match_lines)

if __name__ == "__main__":
    main()