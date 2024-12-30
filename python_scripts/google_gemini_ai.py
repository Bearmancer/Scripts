import chardet
import langid
import os
import time
from pathlib import Path
from argparse import ArgumentParser
import google.generativeai as genai

os.environ['GRPC_VERBOSITY'] = 'NONE'


def process_file(input_file: Path, model_name: str = "gemini-2.0-flash-exp", chunk_size: int = 500,
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
    parser.add_argument("--match_lines", type=bool, default=True, help="Match the number of input and output lines.")
    parser.add_argument("-t", "--instructions", default="""
    YOU ARE TASKED WITH REWRITING EACH LINE IN A TEXT FILE CONTAINING FILE NAMES. FOLLOW THESE RULES:
    VERY IMPORTANT: DO NOT GET RID OF ANY TEXT. DO NOT REMOVE INFORMATION.
    VERY IMPORTANT: TRANSLATE ALL FOREIGN LANGUAGES TO ENGLISH.
    VERY IMPORTANT: REPLACE ∙, :, ;, /, ⁄, ¦, –, -, _ WITH SPACES (EXCEPT IN NAMES LIKE Rimsky-Korsakov OR hr-sinfonieorchester). DON'T REMOVE ( OR ).
    VERY IMPORTANT: ALWAYS KEEP YEARS AND REFORMAT DATES (E.G., "2020/11" BECOMES "2020-11"). 
    VERY IMPORTANT: PUT ALL DATES AFTER THE NAME OF THE PIECE. SO IF THEY ARE AT THE START MOVE IT AFTER THE NAME OF THE PIECE.
    START FILE NAME WITH THE COMPOSER'S LAST NAME WHEREVER POSSIBLE.
    CONVERT ALL-CAPS TO TITLE CASE (EXCEPT FOR ACRONYMS LIKE BBC AND SMALLER WORDS LIKE "for", "the").
    REPLACE DOUBLE QUOTES WITH SINGLE QUOTES.
    REPLACE "N°", "N. " AND "Nº" WITH "No."
    USE COMPOSER NAMES' ENGLISH TRANSLITERATIONS ONLY (E.G., "Tchaikovsky" NOT "Chaikowsky").
    ADD "NO." TO NUMBERED WORKS (E.G., "Symphony 6" BECOMES "Symphony No. 6").
    EXPAND ABBREVIATIONS (E.G., "PC" TO "Piano Concerto").
    TRIM EXTRA SPACES AND STANDARDIZE FORMATTING.
    """)

    args = parser.parse_args()
    input_path = Path(args.input)

    if input_path.is_file():
        files = [input_path]
    else:
        files = input_path.rglob('*.[txt][srt]')

    for file in files:
        print(f"Processing file: {file}")
        process_file(file, args.model, args.chunk_size, args.instructions, args.match_lines)

if __name__ == "__main__":
    main()