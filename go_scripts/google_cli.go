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

	if err := scanner.Err(); err != nil {
		return 0, err
	}

	return lineCount, nil
}

func trimFile(filePath string) error {
	file, err := os.Open(filePath)
	if err != nil {
		return fmt.Errorf("error reading file for trimming: %v", err)
	}
	defer file.Close()

	var trimmedLines []string
	scanner := bufio.NewScanner(file)
	for scanner.Scan() {
		line := scanner.Text()
		trimmedLine := strings.TrimSpace(line)
		if trimmedLine != "" {
			trimmedLines = append(trimmedLines, trimmedLine)
		}
	}

	if err := scanner.Err(); err != nil {
		return fmt.Errorf("error scanning file: %v", err)
	}

	err = os.WriteFile(filePath, []byte(strings.Join(trimmedLines, "\n")+"\n"), 0644)
	if err != nil {
		return fmt.Errorf("error writing trimmed file: %v", err)
	}

	return nil
}

func processChunks(inputFilePath string, outputDir string, prompt string, model string) error {
    file, err := os.Open(inputFilePath)
    if err != nil {
        return fmt.Errorf("error reading input file: %v", err)
    }
    defer file.Close()

    baseName := strings.TrimSuffix(filepath.Base(inputFilePath), ".txt")

    var combinedOutput bytes.Buffer
    scanner := bufio.NewScanner(file)

    chunkNumber := 1
    lines := []string{}
    lineCount := 0

    for scanner.Scan() {
        line := scanner.Text()
        lines = append(lines, line)
        lineCount++

        // Split the file into chunks of exactly 100 lines
        if lineCount == 100 {
            err := processChunk(lines, chunkNumber, baseName, outputDir, prompt, model)
            if err != nil {
                return err
            }
            combinedOutput.WriteString(strings.Join(lines, "\n") + "\n")
            lines = []string{}
            lineCount = 0 // Reset line count after processing a chunk
            chunkNumber++
        }
    }

    // Handle remaining lines if any
    if len(lines) > 0 {
        err := processChunk(lines, chunkNumber, baseName, outputDir, prompt, model)
        if err != nil {
            return err
        }
        combinedOutput.WriteString(strings.Join(lines, "\n") + "\n")
    }

    // Handling combined file for line count comparison
    originalLineCount, err := countLines(inputFilePath)
    if err != nil {
        return fmt.Errorf("error counting lines in input file: %v", err)
    }

    if originalLineCount > 100 {
        combinedFile := filepath.Join(outputDir, fmt.Sprintf("%s_Combined.txt", baseName))
        err = os.WriteFile(combinedFile, combinedOutput.Bytes(), 0644)
        if err != nil {
            return fmt.Errorf("error writing combined file: %v", err)
        }

        err = trimFile(combinedFile)
        if err != nil {
            return fmt.Errorf("error trimming combined file: %v", err)
        }

        trimmedLineCount, err := countLines(combinedFile)
        if err != nil {
            return fmt.Errorf("error counting lines in trimmed combined file: %v", err)
        }

        if originalLineCount == trimmedLineCount {
            fmt.Println("Line count matches between original file and combined file.")
        } else {
            fmt.Printf("Line count does not match! Original: %d, Trimmed Combined: %d\n", originalLineCount, trimmedLineCount)
        }
    } else {
        err := trimFile(inputFilePath)
        if err != nil {
            return fmt.Errorf("error trimming original file: %v", err)
        }

        trimmedLineCount, err := countLines(inputFilePath)
        if err != nil {
            return fmt.Errorf("error counting lines in trimmed original file: %v", err)
        }

        if originalLineCount == trimmedLineCount {
            fmt.Println("Line count matches between original file and processed file.")
        } else {
            fmt.Printf("Line count does not match! Original: %d, Processed: %d\n", originalLineCount, trimmedLineCount)
        }
    }

    return nil
}

func processChunk(lines []string, chunkNumber int, baseName, outputDir, prompt, model string) error {
	chunkText := strings.Join(lines, "\n")

	cmd := exec.Command("gemini-cli", "prompt", "-s", prompt, "--model", model, "-")
	cmd.Env = append(os.Environ(), "GEMINI_API_KEY=AIzaSyDaWVaXLH9d_LWFt1T6qEpXYHh1hGAa2UA")
	cmd.Stdin = strings.NewReader(chunkText)

	var out bytes.Buffer
	var stderr bytes.Buffer
	cmd.Stdout = &out
	cmd.Stderr = &stderr

	err := cmd.Run()
	if err != nil {
		return fmt.Errorf("error processing chunk %d: %s", chunkNumber, stderr.String())
	}

	outputFile := filepath.Join(outputDir, fmt.Sprintf("%s_chunk_%02d.txt", baseName, chunkNumber))
	err = os.WriteFile(outputFile, out.Bytes(), 0644)
	if err != nil {
		return fmt.Errorf("error writing to file %s: %v", outputFile, err)
	}

	err = trimFile(outputFile)
	if err != nil {
		return fmt.Errorf("error trimming chunk file: %v", err)
	}

	fmt.Printf("Chunk %02d processed and saved to %s\n", chunkNumber, outputFile)
	return nil
}

func main() {
	inputDir := `C:\Users\Lance\Desktop\Files`
	outputDir := `C:\Users\Lance\Desktop\Gemini-CLI`
	model := "gemini-1.5-pro-002"
	prompt := `
	You have been tasked with handling with this large amount of string. You must rewrite each line based on the prompt given to you.
Start file with composer's last name. Harding is not a composer.
Keep all lines; don’t remove or empty any.
Convert all-caps to title case (keep acronyms like BBC and prepositions like "for").
Remove composers' first names (e.g., "Ludwig van Beethoven" becomes "Beethoven").
Replace invalid characters (e.g., ∙, ", :, ;, /, ⁄, ¦, –, -) with spaces, except in names like Rimsky-Korsakov or hr-sinfonieorchester.
Translate foreign titles to English.
Replace n° with No. 
Use English translation of composer names always (e.g., "Tchaikovsky" not "Chiakowsky").
Remove single quotes in titles.
Add "No." to numbered works (e.g., "Symphony 6" becomes "Symphony No. 6").
Expand abbreviations (e.g., "PC" to "Piano Concerto", "VC" to "Violin Concerto").
Trim and standardize spacing.
Don’t translate hyphenated names like "hr-sinfonieorchester".
Reformat dates (e.g., "2020/11" becomes "2020-11").
`

	err := os.MkdirAll(outputDir, os.ModePerm)
	if err != nil {
		fmt.Printf("Error creating output directory: %v\n", err)
		return
	}

	files, err := os.ReadDir(inputDir)
	if err != nil {
		fmt.Printf("Error reading input directory: %v\n", err)
		return
	}

	for _, fileInfo := range files {
		if !fileInfo.IsDir() && strings.HasSuffix(fileInfo.Name(), ".txt") {
			inputFilePath := filepath.Join(inputDir, fileInfo.Name())
			fmt.Printf("Processing file: %s\n", inputFilePath)

			originalLineCount, err := countLines(inputFilePath)
			if err != nil {
				fmt.Printf("Error counting lines in input file: %v\n", err)
				continue
			}
			fmt.Printf("Original file %s has %d lines.\n", inputFilePath, originalLineCount)

			err2 := processChunks(inputFilePath, outputDir, prompt, model)
			if err2 != nil {
				fmt.Printf("Error processing chunks: %v\n", err)
				continue
			}
		}
	}

	fmt.Println("Processing completed.")
}