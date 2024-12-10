package main

import (
	"bufio"
	"bytes"
	"fmt"
	"os"
	"os/exec"
	"path/filepath"
	"strings"
	"time"
)

func main() {
	inputPath := `C:\Users\Lance\Desktop\Macbeth 2015 Bluray 1080p DTS-HD x264-Grym.srt`
	outputDir := `C:\Users\Lance\Desktop\Gemini-CLI`
	model := "gemini-1.5-pro-002"
	prompt := `Whilst preserving SRT formatting and without removing any lines, rewrite each line of dialogue in simple modern English.`

	apiKeyFile := `C:\Users\Lance\Documents\Powershell\go_scripts\Google AI Studio API Key.txt`

	apiKey := readAPIKey(apiKeyFile)

	os.MkdirAll(outputDir, os.ModePerm)

	inputInfo, _ := os.Stat(inputPath)

	if inputInfo.IsDir() {
		files, _ := os.ReadDir(inputPath)

		for _, file := range files {
			inputFilePath := filepath.Join(inputPath, file.Name())
			fmt.Println("Processing file:", file.Name())

			processFile(inputFilePath, outputDir, prompt, model, apiKey)

			fmt.Printf("%s successfully processed.\n", file.Name())
		}
	} else {
		fmt.Println("Processing file:", inputPath)
		processFile(inputPath, outputDir, prompt, model, apiKey)

		fmt.Printf("%s successfully processed.\n", inputPath)
	}

	fmt.Println("Processing completed.")
}

func processFile(inputFilePath, outputDir, prompt, model, apiKey string) {
	file, _ := os.Open(inputFilePath)
	defer file.Close()

	lines := []string{}
	scanner := bufio.NewScanner(file)
	for scanner.Scan() {
		line := scanner.Text()
		lines = append(lines, line)
	}

	totalLines := len(lines)
	fmt.Println("Total lines in the original file:", totalLines)

	baseName := filepath.Base(inputFilePath[:len(inputFilePath)-4])
	chunkSize := 100
	var combinedOutput bytes.Buffer

	for i := 0; i < totalLines; i += chunkSize {
		end := i + chunkSize
		if end > totalLines {
			end = totalLines
		}
		chunk := lines[i:end]

		chunkNumber := (i / chunkSize) + 1
		fmt.Printf("Processing chunk %d of %d\n", chunkNumber, (totalLines/chunkSize)+1)

		output := processChunk(chunk, prompt, model, apiKey)

		combinedOutput.WriteString(output)
	}

	outputFile := filepath.Join(outputDir, fmt.Sprintf("%s.txt", baseName))
	_ = os.WriteFile(outputFile, combinedOutput.Bytes(), 0644)

	inputLineCount := countLines(inputFilePath)
	outputLineCount := countLines(outputFile)

	fmt.Printf("Input lines: %d, Output lines: %d\n", inputLineCount, outputLineCount)

	if inputLineCount != outputLineCount {
		fmt.Println("Line count mismatch detected, retrying...")
		time.Sleep(3 * time.Second)
		processFile(inputFilePath, outputDir, prompt, model, apiKey)
	}
}

func processChunk(lines []string, prompt, model, apiKey string) string {
	cmd := exec.Command("gemini-cli", "prompt", prompt, "--model", model, "-")
	cmd.Env = append(os.Environ(), "GEMINI_API_KEY="+apiKey)
	cmd.Stdin = strings.NewReader(strings.Join(lines, "\n"))
	var out, stderr bytes.Buffer
	cmd.Stdout = &out
	cmd.Stderr = &stderr

	cmd.Run()

	outputLines := strings.Split(out.String(), "\n")
	trimmedOutput := []string{}
	for _, line := range outputLines {
		line = strings.TrimSpace(line)
		if line != "" {
			trimmedOutput = append(trimmedOutput, line)
		}
	}

	return strings.Join(trimmedOutput, "\n") + "\n"
}

func readAPIKey(apiKeyFile string) string {
	file, _ := os.Open(apiKeyFile)
	defer file.Close()

	scanner := bufio.NewScanner(file)
	scanner.Scan()
	return scanner.Text()
}

func countLines(filePath string) int {
	file, _ := os.Open(filePath)
	defer file.Close()

	scanner := bufio.NewScanner(file)
	lineCount := 0
	for scanner.Scan() {
		lineCount++
	}

	return lineCount
}

// `
// You have been tasked with handling a large text file with file names on each line. You must rewrite each line based on the prompt given to you. VERY IMPORTANT: NEVER DELETE LINES. ALWAYS TRANSLATE FOREIGN LANGUAGES TO ENGLISH.
// VERY IMPORTANT - NEVER REMOVE YEARS EVER!
// Start file with composer's last name. Harding is not a composer.
// Keep all lines; don’t remove or empty any.
// Convert all-caps to title case (keep acronyms like BBC and prepositions like "for").
// Remove composers' first names (e.g., "Ludwig van Beethoven" becomes "Beethoven").
// Very important: Replace " with '
// VERY IMPORTANT THAT YOU Replace the following characters [∙, :, ;, /, ⁄, ¦, –, -] with spaces, except in names like Rimsky-Korsakov or hr-sinfonieorchester.
// Translate all foreign languages to English. VERY IMPORTANT.
// Replace n° and Nº with No.
// Use English translation of composer names always (e.g., "Tchaikovsky" not "Chiakowsky").
// Remove single quotes in titles.
// Add "No." to numbered works (e.g., "Symphony 6" becomes "Symphony No. 6").
// Expand abbreviations (e.g., "PC" to "Piano Concerto", "VC" to "Violin Concerto").
// Trim and standardize spacing.
// Don’t translate hyphenated names like "hr-sinfonieorchester".
// Reformat dates (e.g., "2020/11" becomes "2020-11").`