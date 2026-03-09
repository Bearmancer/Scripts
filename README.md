# Scripts-Azure

Utilities for OCR/document extraction and music metadata workflows.

## Azure document transcription setup

For the Karajan OCR workflow in this repository, the shortest path is:

```powershell
tools read .\1000054936.jpg --azure-docintel-key "<api-key>"
```

That works because this repo already defaults the Azure Document Intelligence endpoint to:

```text
https://document-intelligence-lance.cognitiveservices.azure.com/
```

You only need to pass `--azure-docintel-endpoint` if you want to use a different Azure resource.

If you are not using that default resource, you only need **two Azure values** to use Azure OCR here:

- endpoint
- API key

You can pass them directly to the CLI, so environment variables are optional:

```powershell
tools read .\booklet.pdf --azure-docintel-endpoint "https://<resource>.cognitiveservices.azure.com/" --azure-docintel-key "<api-key>"
```

### Where to get the endpoint and API key in Azure

There is no separate “endpoint key” value. You need:

1. the **Endpoint**
2. **Key 1** or **Key 2**

To find them in Azure:

1. Open the Azure portal or Azure mobile app.
2. Open your **Document Intelligence** resource. If Azure shows it as a **Cognitive Services account**, that is fine too.
3. Open **Keys and Endpoint**.
4. Copy the **Endpoint** value.
5. Copy either **Key 1** or **Key 2**.

Typical endpoint format:

```text
https://<resource-name>.cognitiveservices.azure.com/
```

You can then run:

```powershell
tools read .\booklet.pdf --azure-docintel-endpoint "https://<resource-name>.cognitiveservices.azure.com/" --azure-docintel-key "<key-1-or-key-2>"
```

The `tools read` command prefers **Azure Document Intelligence** for local scanned PDFs, standalone page images, and EPUB page images whenever an API key is available through the command-line options or these environment variables:

- `AZURE_DOCUMENT_INTELLIGENCE_ENDPOINT`
- `AZURE_DOCUMENT_INTELLIGENCE_KEY`
- `AZURE_DOCUMENT_INTELLIGENCE_MODEL_ID` (optional, defaults to `prebuilt-layout`)

If Azure Document Intelligence is not configured, the existing Google OCR path remains in place.

### Why Document Intelligence instead of basic OCR

For multilingual, layout-heavy booklet scans, Azure Document Intelligence is the better fit than generic image OCR because it keeps paragraph/layout structure, handles PDFs and images, and makes it easier to strip headers/footers before building EPUB text output.

### What Azure AI/model you can configure here

This repository currently exposes **one Azure model knob**:

- `--azure-docintel-model`
- or `AZURE_DOCUMENT_INTELLIGENCE_MODEL_ID`

Default:

- `prebuilt-layout`

What that means in practice:

- `prebuilt-layout` is the recommended default for booklet OCR because this pipeline mainly needs **text, paragraphs, line order, and layout**.
- The code passes any model id you provide directly to Azure Document Intelligence.
- This pipeline currently uses the returned **OCR/layout text** and **paragraph structure**. It does **not** currently consume specialized field extraction such as invoice fields, receipt fields, or ID-document fields.

So, while Azure supports many model types, this repository is currently optimized for **layout-first document transcription**, not form extraction.

### Practical OCR optimizations that matter for this repo

If you want the best results for booklet transcription, these are the useful knobs:

1. **Use a clean PDF when possible**
   - A scanned PDF is usually easier to batch than loose images.
   - If the PDF already contains embedded text, the pipeline uses that instead of OCR.

2. **Use `prebuilt-layout` first**
   - That is the default and the path this repo is tuned for.
   - It preserves page structure better than basic OCR.

3. **Use direct CLI options for one-off runs**
   - Best when you just want to test a file quickly without setting env vars.

4. **Use env vars for repeated runs**
   - Better when you are processing many files and do not want to repeat the same endpoint/key every time.

5. **Improve the scan before OCR**
   - 300 DPI or better
   - straight pages
   - cropped borders
   - good contrast
   - avoid camera photos when a flat scan or exported PDF is available

6. **Prefer full page images over partial crops**
   - The header/footer stripping and paragraph recovery work best on complete pages.

7. **Let the fallback chain work**
   - If Azure is not configured, the repo falls back to the existing Google/Tesseract path.
   - If Azure is configured but fails, the fallback path still runs.

8. **Know what is *not* configurable yet**
   - Header/footer stripping thresholds are currently code defaults.
   - There is no separate language flag in this integration.
   - There is no repo-specific Azure tuning beyond endpoint, key, and model id.

## Secrets and local usage

For the OCR/translation environment variables used by the `read` workflow and related local runs, GitHub Actions secrets are encrypted for workflow use, but you **cannot view or recover the original plaintext value in the GitHub UI after saving it**. Workflows can still access the secret at runtime; keep your own copy in a password manager or local secret store.

Important distinction:

- **Azure** is where you discover the original endpoint and API key.
- **GitHub Actions secrets** are only where you store those values for workflows after you already have them.

Typical local setup:

```powershell
$env:AZURE_DOCUMENT_INTELLIGENCE_ENDPOINT = "https://<resource>.cognitiveservices.azure.com/"
$env:AZURE_DOCUMENT_INTELLIGENCE_KEY = "<api-key>"
$env:AZURE_DOCUMENT_INTELLIGENCE_MODEL_ID = "prebuilt-layout"
$env:AZURE_TRANSLATOR_KEY = "<translator-key>"
$env:AZURE_TRANSLATOR_REGION = "<region>"
```

The repository already ignores `.env`, so you can also mirror the same values locally in an uncommitted `.env` file.

## Classical booklet workflow

Recommended sequence for large classical disc/booklet sets:

1. Scan or assemble pages into PDF/EPUB page images, or keep the page scans as standalone `.jpg`, `.jpeg`, or `.png` files.
2. Run `tools read <file.pdf>`, `tools read <file.epub>`, or `tools read <file.jpg>` to extract searchable text and produce EPUB output.
3. Review OCR output for multilingual titles and performer credits.
4. Normalize work-level metadata into `TrackInfo`/`WorkSummary` records.
5. Export merged work rows with CSV fields including work, composer, orchestra, conductor, soloists, venue, year, and merged duration.

`WorkGrouper` already merges per-track durations into a single work-level duration, and the CSV export now includes soloists, venue, and total duration to support classical catalogue work.
