package main

import (
	"fmt"
	"os"
	"os/exec"
	"path/filepath"
	"strings"
)

func main() {
	inputPath := `C:\Users\Lance\Desktop\Input`
	outputDir := `C:\Users\Lance\Desktop\Gemini-CLI`
	model := "gemini-2.0-flash-exp"
	prompt := `You are tasked with rewriting each line in a text file containing file names. Follow these rules:
Very Important: Do not delete any lines.
Very Important: Translate all foreign languages to English.
Very Important: Replace ∙, :, ;, /, ⁄, ¦, –, - with spaces (except in names like Rimsky-Korsakov or hr-sinfonieorchester).
Very Important: Always keep years and reformat dates (e.g., "2020/11" becomes "2020-11").
Start with the composer's last name (and remove first name when applicable.)
Convert all-caps to title case (keep acronyms like BBC and small words like "for").
Replace double quotes with single quotes.
Replace "n°" and "Nº" with "No."
Use composer names' English transliterations only (e.g., "Tchaikovsky" not "Chiakowsky").
Add "No." to numbered works (e.g., "Symphony 6" becomes "Symphony No. 6").
Expand abbreviations (e.g., "PC" to "Piano Concerto").
Trim extra spaces and standardize formatting.
`

	apiKey := os.Getenv("GEMINI_API_KEY")

	os.MkdirAll(outputDir, os.ModePerm)

	info, _ := os.Stat(inputPath)

	if info.IsDir() {
		files, _ := os.ReadDir(inputPath)
		for _, file := range files {
			processFile(filepath.Join(inputPath, file.Name()), outputDir, prompt, model, apiKey)
		}
	} else {
		processFile(inputPath, outputDir, prompt, model, apiKey)
	}
}

func processFile(inputFilePath, outputDir, prompt, model, apiKey string) {
	content, _ := os.ReadFile(inputFilePath)
	lines := strings.Split(string(content), "\n")
	baseName := filepath.Base(inputFilePath[:len(inputFilePath)-4])

	fmt.Printf("Processing file: %s\nTotal lines: %d\n", inputFilePath, len(lines))

	const chunkSize = 200
	var processedLines []string

	for i := 0; i < len(lines); i += chunkSize {
		end := min(i+chunkSize, len(lines))
		chunk := lines[i:end]

		fmt.Printf("Processing chunk %d of %d\n", i/chunkSize+1, (len(lines)+chunkSize-1)/chunkSize)

		output := processChunk(chunk, prompt, model, apiKey)
		processedLines = append(processedLines, strings.Split(output, "\n")...)

		chunkName := fmt.Sprintf("%s - Chunk %d%s", baseName, i/chunkSize+1, filepath.Ext(inputFilePath))
		chunkFile := filepath.Join(outputDir, chunkName)

		os.WriteFile(chunkFile, []byte(output), 0644)
	}

	outputPath := filepath.Join(outputDir, filepath.Base(inputFilePath))
	os.WriteFile(outputPath, []byte(strings.Join(processedLines, "\n")), 0644)

	fmt.Printf("%s successfully processed.\n", baseName)
}

func processChunk(lines []string, prompt, model, apiKey string) string {
	cmd := exec.Command("gemini-cli", "--key", apiKey, "prompt", prompt, "--model", model, "-")

	cmd.Stdin = strings.NewReader(strings.Join(lines, "\n"))

	out, _ := cmd.Output()

	output := string(out)
	output = strings.Trim(output, "`")
	output = strings.TrimSpace(output)

	return output
}