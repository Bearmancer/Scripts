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
	model, prompt := "gemini-1.5-pro-002", `Whilst preserving SRT formatting and without removing any lines, rewrite each line of dialogue in simple modern English.`
	apiKey := readAPIKey(`C:\Users\Lance\Documents\Powershell\go_scripts\Google AI Studio API Key.txt`)

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
	lines, originalLines, originalEmptyLines := readLines(inputFilePath)
	baseName := filepath.Base(inputFilePath[:len(inputFilePath)-len(filepath.Ext(inputFilePath))]) 

	fmt.Printf("Processing file: %s\nTotal lines: %d\n", baseName, len(lines))

	chunkSize := 100
	totalChunks := (len(lines) + chunkSize - 1) / chunkSize 

	var combinedOutput bytes.Buffer
	var chunkFiles []string 

	for i := 0; i < len(lines); i += chunkSize {
		end := min(i+chunkSize, len(lines))		
		chunk := lines[i:end]

		fmt.Printf("Processing chunk %d of %d\n", i/chunkSize+1, totalChunks)

		output := processChunk(chunk, prompt, model, apiKey)
		chunkFile := filepath.Join(outputDir, fmt.Sprintf("%s_chunk_%d%s", baseName, i/chunkSize+1, filepath.Ext(inputFilePath))) 
		os.WriteFile(chunkFile, []byte(output), 0644)
		combinedOutput.WriteString(output)

		chunkFiles = append(chunkFiles, chunkFile)
	}

	outputFile := filepath.Join(outputDir, fmt.Sprintf("%s%s", baseName, filepath.Ext(inputFilePath)))
	restoreOriginalFormat(&combinedOutput, outputFile, originalLines, originalEmptyLines)

	verifyLineCount(inputFilePath, outputFile)

	for _, chunkFile := range chunkFiles {
		os.Remove(chunkFile)
	}

	fmt.Printf("%s successfully processed.\n", baseName)
}

func processChunk(lines []string, prompt, model, apiKey string) string {
	cmd := exec.Command("gemini-cli", "prompt", prompt, "--model", model, "-")
	cmd.Env = append(os.Environ(), "GEMINI_API_KEY="+apiKey)
	cmd.Stdin = strings.NewReader(strings.Join(lines, "\n"))
	var out bytes.Buffer
	cmd.Stdout = &out
	cmd.Run()

	var trimmedOutput []string
	for _, line := range strings.Split(out.String(), "\n") {
		if trimmed := strings.TrimSpace(line); trimmed != "" {
			trimmedOutput = append(trimmedOutput, trimmed)
		}
	}
	return strings.Join(trimmedOutput, "\n") + "\n"
}

func readLines(filePath string) ([]string, []string, []bool) {
	file, _ := os.Open(filePath)
	defer file.Close()

	var lines, originalLines []string
	var originalEmptyLines []bool
	scanner := bufio.NewScanner(file)
	for scanner.Scan() {
		line := scanner.Text()
		originalLines = append(originalLines, line)
		if strings.TrimSpace(line) == "" {
			originalEmptyLines = append(originalEmptyLines, true)
		} else {
			originalEmptyLines = append(originalEmptyLines, false)
			lines = append(lines, line)
		}
	}
	return lines, originalLines, originalEmptyLines
}

func restoreOriginalFormat(combinedOutput *bytes.Buffer, outputFile string, originalLines []string, originalEmptyLines []bool) {
	var restoredOutput bytes.Buffer
	outputLines := strings.Split(combinedOutput.String(), "\n")
	outputIndex := 0
	for i := range originalLines {
		if originalEmptyLines[i] {
			restoredOutput.WriteString("\n")
		} else if outputIndex < len(outputLines) {
			restoredOutput.WriteString(outputLines[outputIndex] + "\n")
			outputIndex++
		}
	}
	os.WriteFile(outputFile, restoredOutput.Bytes(), 0644)
}

func verifyLineCount(inputFilePath, outputFile string) {
	inputLineCount := countLines(inputFilePath)
	outputLineCount := countLines(outputFile)
	if inputLineCount != outputLineCount {
		fmt.Printf("Line count mismatch. Retrying...\n")
		time.Sleep(3 * time.Second)
		processFile(inputFilePath, filepath.Dir(outputFile), "", "", "")
	}
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

func readAPIKey(apiKeyFile string) string {
	file, _ := os.Open(apiKeyFile)
	defer file.Close()
	scanner := bufio.NewScanner(file)
	scanner.Scan()
	return scanner.Text()
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