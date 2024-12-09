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
You have been tasked with handling with a large text file with file names on each line. You must rewrite each line based on the prompt given to you without removing a single line or emptying any. ALWAYS TRANSLATE FOREIGN LANGUAGES TO ENGLISH.
Start file with composer's last name. Harding is not a composer.
Keep all lines; don’t remove or empty any.
Convert all-caps to title case (keep acronyms like BBC and prepositions like "for").
Remove composers' first names (e.g., "Ludwig van Beethoven" becomes "Beethoven").
VERY IMPORTANT THAT YOU Replace the following characters [∙, ", :, ;, /, ⁄, ¦, –, -] with spaces, except in names like Rimsky-Korsakov or hr-sinfonieorchester.
Translate all foreign languages to English. VERY IMPORTANT.
Replace n° and Nº with No. 
Use English translation of composer names always (e.g., "Tchaikovsky" not "Chiakowsky").
Remove single quotes in titles.
Add "No." to numbered works (e.g., "Symphony 6" becomes "Symphony No. 6").
Expand abbreviations (e.g., "PC" to "Piano Concerto", "VC" to "Violin Concerto").
Trim and standardize spacing.
Don’t translate hyphenated names like "hr-sinfonieorchester".
Reformat dates (e.g., "2020/11" becomes "2020-11").
`
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

	for _, fileInfo := range files {
		if !fileInfo.IsDir() && strings.HasSuffix(fileInfo.Name(), ".txt") {
			inputFilePath := filepath.Join(inputDir, fileInfo.Name())
			if err := processFile(inputFilePath, outputDir, prompt, model, apiKey); err != nil {
				fmt.Println("Error processing file:", err)
			}
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

	baseName := strings.TrimSuffix(filepath.Base(inputFilePath), ".txt")
	var combinedOutput bytes.Buffer
	scanner := bufio.NewScanner(file)
	lines := []string{}
	chunkNumber, lineCount := 1, 0
	var chunkFiles []string

	// Print which file is being processed
	fmt.Printf("Processing file: %s\n", inputFilePath)

	for scanner.Scan() {
		lines = append(lines, scanner.Text())
		lineCount++
	}

	// Print total line count of the file
	fmt.Printf("File %s has %d lines.\n", inputFilePath, lineCount)

	// Process the file in chunks of 100 lines
	for chunkNumber*100 <= lineCount {
		if err := processChunk(lines[(chunkNumber-1)*100:chunkNumber*100], chunkNumber, baseName, outputDir, prompt, model, apiKey); err != nil {
			return err
		}
		chunkFiles = append(chunkFiles, fmt.Sprintf("%s_chunk_%02d.txt", baseName, chunkNumber))
		combinedOutput.WriteString(strings.Join(lines[(chunkNumber-1)*100:chunkNumber*100], "\n") + "\n")
		chunkNumber++
	}

	// Process any remaining lines
	if len(lines[(chunkNumber-1)*100:]) > 0 {
		if err := processChunk(lines[(chunkNumber-1)*100:], chunkNumber, baseName, outputDir, prompt, model, apiKey); err != nil {
			return err
		}
		chunkFiles = append(chunkFiles, fmt.Sprintf("%s_chunk_%02d.txt", baseName, chunkNumber))
		combinedOutput.WriteString(strings.Join(lines[(chunkNumber-1)*100:], "\n") + "\n")
	}

	originalLineCount, err := countLines(inputFilePath)
	if err != nil {
		return err
	}

	if originalLineCount > 100 {
		combinedFile := filepath.Join(outputDir, fmt.Sprintf("%s_Combined.txt", baseName))
		if err := os.WriteFile(combinedFile, combinedOutput.Bytes(), 0644); err != nil {
			return err
		}
		if err := trimFile(combinedFile); err != nil {
			return err
		}
		trimmedLineCount, err := countLines(combinedFile)
		if err != nil {
			return err
		}

		// Print result of comparing line counts
		fmt.Printf("Original file line count: %d, Combined file line count: %d\n", originalLineCount, trimmedLineCount)
		if originalLineCount != trimmedLineCount {
			fmt.Printf("Line count mismatch! Original: %d, Trimmed: %d\n", originalLineCount, trimmedLineCount)
		}

		// Remove chunk files with more detailed checks and debugging
		for _, chunkFile := range chunkFiles {
			fmt.Printf("Attempting to delete chunk file: %s\n", chunkFile)

			if _, err := os.Stat(chunkFile); err != nil {
				if os.IsNotExist(err) {
					fmt.Printf("Chunk file %s does not exist, skipping deletion.\n", chunkFile)
				} else {
					fmt.Printf("Error checking file status for %s: %s\n", chunkFile, err)
				}
			} else {
				// File exists, proceed with deletion
				if err := os.Remove(chunkFile); err != nil {
					fmt.Printf("Error deleting chunk file %s: %s\n", chunkFile, err)
				} else {
					fmt.Printf("Successfully deleted chunk file: %s\n", chunkFile)
				}
			}
		}
	} else {
		if err := trimFile(inputFilePath); err != nil {
			return err
		}
		trimmedLineCount, err := countLines(inputFilePath)
		if err != nil {
			return err
		}

		// Print result of comparing line counts
		fmt.Printf("Original file line count: %d, Processed file line count: %d\n", originalLineCount, trimmedLineCount)
		if originalLineCount != trimmedLineCount {
			fmt.Printf("Line count mismatch! Original: %d, Processed: %d\n", originalLineCount, trimmedLineCount)
		}
	}

	return nil
}

func processChunk(lines []string, chunkNumber int, baseName, outputDir, prompt, model, apiKey string) error {
	cmd := exec.Command("gemini-cli", "prompt", "-s", prompt, "--model", model, "-")
	cmd.Env = append(os.Environ(), "GEMINI_API_KEY="+apiKey)
	cmd.Stdin = strings.NewReader(strings.Join(lines, "\n"))
	var out, stderr bytes.Buffer
	cmd.Stdout = &out
	cmd.Stderr = &stderr
	if err := cmd.Run(); err != nil {
		return fmt.Errorf("chunk %d: %s", chunkNumber, stderr.String())
	}
	outputFile := filepath.Join(outputDir, fmt.Sprintf("%s_chunk_%02d.txt", baseName, chunkNumber))
	if err := os.WriteFile(outputFile, out.Bytes(), 0644); err != nil {
		return err
	}
	return trimFile(outputFile)
}

func countLines(filePath string) (int, error) {
	file, err := os.Open(filePath)
	if err != nil {
		return 0, err
	}
	defer file.Close()

	lineCount := 0
	scanner := bufio.NewScanner(file)
	for scanner.Scan() {
		lineCount++
	}
	return lineCount, scanner.Err()
}

func trimFile(filePath string) error {
	file, err := os.Open(filePath)
	if err != nil {
		return err
	}
	defer file.Close()

	var lines []string
	scanner := bufio.NewScanner(file)
	for scanner.Scan() {
		line := strings.TrimSpace(scanner.Text())
		if line != "" {
			lines = append(lines, line)
		}
	}

	return os.WriteFile(filePath, []byte(strings.Join(lines, "\n")+"\n"), 0644)
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
	return "", fmt.Errorf("API key not found")
}
