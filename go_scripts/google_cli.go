package main

import (
	"bufio"
	"bytes"
	"fmt"
	"os"
	"os/exec"
	"path/filepath"
	"strings"
)

func main() {
	inputFilePath := "C:\\Users\\Lance\\Desktop\\File Names.txt"
	outputDir := "C:\\Users\\Lance\\Desktop\\Gemini-CLI"
	model := "gemini-1.5-pro-002"
	prompt := `
	Use composer last name at start of file. Keep input number of lines the same as output. Don't add new lines 
Convert all-caps to title case (preserve specific acronyms like BBC and prepositions like 'for')
Remove first name of composers
Remove invalid characters for file names on Windows en-US such as :, ;, /, ⁄, ¦,  – and - with space unless part of composer name like Rimsky-Korsakov
Translate foreign titles to English
Translate composer names to their English variants (Chiakowsky to Tchaikovsky)
Remove single quotes in titles
Add "No." to numbered works if missing such as: "Symphony 6" → "Symphony No. 6"

Expand abbreviated titles:

PC → Piano Concerto
VC → Violin Concerto

Trim and standardize spacing

Preserve orchestra names with hyphen as their original names such as hr-sinfonieorchester.

Reformat dates:

"2020/11" → "2020-11"
	`

	err := os.MkdirAll(outputDir, os.ModePerm)
	if err != nil {
		fmt.Printf("Error creating output directory: %v\n", err)
		return
	}

	file, err := os.Open(inputFilePath)
	if err != nil {
		fmt.Printf("Error reading input file: %v\n", err)
		return
	}
	defer file.Close()

	var chunks [][]string
	var currentChunk []string
	scanner := bufio.NewScanner(file)

	lineCount := 0
	for scanner.Scan() {
		currentChunk = append(currentChunk, scanner.Text())
		lineCount++
		if lineCount >= 75 { 
			chunks = append(chunks, currentChunk)
			currentChunk = nil
			lineCount = 0
		}
	}
	if len(currentChunk) > 0 {
		chunks = append(chunks, currentChunk)
	}

	if err := scanner.Err(); err != nil {
		fmt.Printf("Error reading file: %v\n", err)
		return
	}

	fmt.Printf("File read successfully. Processing %d chunks...\n", len(chunks))

	for i, chunk := range chunks {
		chunkText := strings.Join(chunk, "\n")

		fmt.Printf("Processing chunk %d with %d lines...\n", i+1, len(chunk))

		cmd := exec.Command("gemini-cli", "prompt", "-s", prompt, "--model", model, "-")
		cmd.Env = append(os.Environ(), "GEMINI_API_KEY=AIzaSyDaWVaXLH9d_LWFt1T6qEpXYHh1hGAa2UA")
		cmd.Stdin = strings.NewReader(chunkText)

		var out bytes.Buffer
		var stderr bytes.Buffer
		cmd.Stdout = &out
		cmd.Stderr = &stderr

		err := cmd.Run()
		if err != nil {
			fmt.Printf("Error processing chunk %d: %s\n", i+1, stderr.String())
			continue
		}

		outputFile := filepath.Join(outputDir, fmt.Sprintf("chunk_%d.txt", i+1))
		err = os.WriteFile(outputFile, out.Bytes(), 0644)
		if err != nil {
			fmt.Printf("Error writing to file %s: %v\n", outputFile, err)
			continue
		}

		fmt.Printf("Chunk %d processed and saved to %s\n", i+1, outputFile)
	}

	fmt.Println("Processing completed.")
}