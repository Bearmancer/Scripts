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
	inputDir := `C:\Users\Lance\Desktop\Files`
	outputDir := `C:\Users\Lance\Desktop\Gemini-CLI`
	model := "gemini-1.5-pro-002"
	prompt := `
You have been tasked with handling a large text file with file names on each line. You must rewrite each line based on the prompt given to you. VERY IMPORTANT: NEVER DELETE LINES. ALWAYS TRANSLATE FOREIGN LANGUAGES TO ENGLISH.
VERY IMPORTANT - NEVER REMOVE YEARS EVER!
Start file with composer's last name. Harding is not a composer.
Keep all lines; don’t remove or empty any.
Convert all-caps to title case (keep acronyms like BBC and prepositions like "for").
Remove composers' first names (e.g., "Ludwig van Beethoven" becomes "Beethoven").
Very important: Replace " with '
VERY IMPORTANT THAT YOU Replace the following characters [∙, :, ;, /, ⁄, ¦, –, -] with spaces, except in names like Rimsky-Korsakov or hr-sinfonieorchester.
Translate all foreign languages to English. VERY IMPORTANT.
Replace n° and Nº with No.
Use English translation of composer names always (e.g., "Tchaikovsky" not "Chiakowsky").
Remove single quotes in titles.
Add "No." to numbered works (e.g., "Symphony 6" becomes "Symphony No. 6").
Expand abbreviations (e.g., "PC" to "Piano Concerto", "VC" to "Violin Concerto").
Trim and standardize spacing.
Don’t translate hyphenated names like "hr-sinfonieorchester".
Reformat dates (e.g., "2020/11" becomes "2020-11").`

	apiKeyFile := `C:\Users\Lance\Documents\Powershell\go_scripts\Google AI Studio API Key.txt`

	apiKey, err := readAPIKey(apiKeyFile)
	if err != nil {
		fmt.Println("Error reading API key:", err)
		return
	}

	if err := os.MkdirAll(outputDir, os.ModePerm); err != nil {
		fmt.Println("Error creating output directory:", err)
		return
	}

	files, err := os.ReadDir(inputDir)
	if err != nil {
		fmt.Println("Error reading input directory:", err)
		return
	}

	for _, file := range files {
		if file.IsDir() || !strings.HasSuffix(file.Name(), ".txt") {
			continue
		}

		inputFilePath := filepath.Join(inputDir, file.Name())
		fmt.Println("Processing file:", file.Name())

		for {
			if err := processFile(inputFilePath, outputDir, prompt, model, apiKey); err != nil {
				fmt.Println("Error processing file:", err)
				return
			}

			inputFileLineCount, err := countLines(inputFilePath)
			if err != nil {
				fmt.Println("Error counting lines in input file:", err)
				return
			}

			combinedOutputFilePath := filepath.Join(outputDir, fmt.Sprintf("%s.txt", filepath.Base(inputFilePath[:len(inputFilePath)-4])))
			combinedOutputLineCount, err := countLines(combinedOutputFilePath)
			if err != nil {
				fmt.Println("Error counting lines in output file:", err)
				return
			}

			if inputFileLineCount != combinedOutputLineCount {
				fmt.Printf("Line counts do not match. Original file: %d lines, New file: %d lines. Reprocessing the file.\n", inputFileLineCount, combinedOutputLineCount)
				continue
			}

			fmt.Printf("%s successfully processed.\n", file.Name())
			break
		}
	}

	fmt.Println("Processing completed.")
}

func processFile(inputFilePath, outputDir, prompt, model, apiKey string) error {
	file, err := os.Open(inputFilePath)
	if err != nil {
		return err
	}
	defer file.Close()

	lines := []string{}
	scanner := bufio.NewScanner(file)
	for scanner.Scan() {
		line := strings.TrimSpace(scanner.Text())
		lines = append(lines, line)
	}

	if err := scanner.Err(); err != nil {
		return err
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

		output, err := processChunk(chunk, prompt, model, apiKey)
		if err != nil {
			return err
		}

		combinedOutput.WriteString(output)
	}

	outputFile := filepath.Join(outputDir, fmt.Sprintf("%s.txt", baseName))
	return os.WriteFile(outputFile, combinedOutput.Bytes(), 0644)
}

func processChunk(lines []string, prompt, model, apiKey string) (string, error) {
	cmd := exec.Command("gemini-cli", "prompt", prompt, "--model", model, "-")
	cmd.Env = append(os.Environ(), "GEMINI_API_KEY="+apiKey)
	cmd.Stdin = strings.NewReader(strings.Join(lines, "\n"))
	var out, stderr bytes.Buffer
	cmd.Stdout = &out
	cmd.Stderr = &stderr

	if err := cmd.Run(); err != nil {
		return "", fmt.Errorf("error processing chunk: %s", stderr.String())
	}

	outputLines := strings.Split(out.String(), "\n")
	trimmedOutput := []string{}
	for _, line := range outputLines {
		line = strings.TrimSpace(line)
		if line != "" {
			trimmedOutput = append(trimmedOutput, line)
		}
	}

	return strings.Join(trimmedOutput, "\n") + "\n", nil
}

func readAPIKey(apiKeyFile string) (string, error) {
	file, err := os.Open(apiKeyFile)
	if err != nil {
		return "", err
	}
	defer file.Close()

	scanner := bufio.NewScanner(file)
	if scanner.Scan() {
		return scanner.Text(), nil
	}
	return "", fmt.Errorf("API key not found in file")
}

func countLines(filePath string) (int, error) {
	file, err := os.Open(filePath)
	if err != nil {
		return 0, err
	}
	defer file.Close()

	scanner := bufio.NewScanner(file)
	lineCount := 0
	for scanner.Scan() {
		lineCount++
	}

	if err := scanner.Err(); err != nil {
		return 0, err
	}

	return lineCount, nil
}
