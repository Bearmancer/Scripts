import deepl, sys, os
auth_key = open("C:/Users/Lance/Documents/Powershell/Python Scripts/DeepL API Key.txt", 'r').read()

translator = deepl.Translator(auth_key)

if len(sys.argv) != 2:
    sys.exit(1)

doc = open(sys.argv[1], 'r').read()

result = translator.translate_text(doc, target_lang="EN-GB")

translated_doc = result

base, ext = os.path.splitext(sys.argv[1])
new_file_path = f"{base} - Translated{ext}"

with open(new_file_path, 'w') as file:
    file.write(translated_doc.text)