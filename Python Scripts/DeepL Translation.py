import deepl, sys, os, chardet

auth_key = open("C:/Users/Lance/Documents/Powershell/Python Scripts/DeepL API Key.txt", 'r').read()

translator = deepl.Translator(auth_key)

if len(sys.argv) != 2:
    sys.exit(1)

try:
    with open(sys.argv[1], 'r') as file:
        doc = file.read()
except UnicodeDecodeError:
    with open(sys.argv[1], 'rb') as file:
        raw_data = file.read()
        encoding = chardet.detect(raw_data)['encoding']
        doc = raw_data.decode(encoding)

result = translator.translate_text(doc, target_lang="EN-GB")

translated_doc = result

base, ext = os.path.splitext(sys.argv[1])
new_file_path = f"{base} - Translated{ext}"

with open(new_file_path, 'w', encoding='utf-8') as file:
    file.write(translated_doc.text)