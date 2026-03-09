# Scripts-Azure

Utilities for OCR/document extraction and music metadata workflows.

## Azure document transcription setup

The `tools read` command now prefers **Azure Document Intelligence** for local scanned PDFs and EPUB page images whenever these environment variables are set:

- `AZURE_DOCUMENT_INTELLIGENCE_ENDPOINT`
- `AZURE_DOCUMENT_INTELLIGENCE_KEY`
- `AZURE_DOCUMENT_INTELLIGENCE_MODEL_ID` (optional, defaults to `prebuilt-layout`)

If Azure Document Intelligence is not configured, the existing Google OCR path remains in place.

### Why Document Intelligence instead of basic OCR

For multilingual, layout-heavy booklet scans, Azure Document Intelligence is the better fit than generic image OCR because it keeps paragraph/layout structure, handles PDFs and images, and makes it easier to strip headers/footers before building EPUB text output.

## Secrets and local usage

For the OCR/translation environment variables used by the `read` workflow and related local runs, GitHub Actions secrets are **write-only** once saved. GitHub encrypts them for workflow use, but you **cannot recover the original plaintext value from GitHub later**. Keep your own copy in a password manager or local secret store.

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

1. Scan or assemble pages into PDF/EPUB page images.
2. Run `tools read <file.pdf>` or `tools read <file.epub>` to extract searchable text and produce EPUB output.
3. Review OCR output for multilingual titles and performer credits.
4. Normalize work-level metadata into `TrackInfo`/`WorkSummary` records.
5. Export merged work rows with CSV fields including work, composer, orchestra, conductor, soloists, venue, year, and merged duration.

`WorkGrouper` already merges per-track durations into a single work-level duration, and the CSV export now includes soloists, venue, and total duration to support classical catalogue work.
