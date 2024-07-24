$word = New-Object -ComObject Word.Application
$word.Visible = $false

$doc = $word.Documents.Add()

$files = Get-ChildItem -Path $PWD | Select-Object -ExpandProperty Name

$cleanedFiles = $files -replace '[":\\/]', ''

$paragraph = $doc.Paragraphs.Add()
$paragraph.Range.Text = ($cleanedFiles -join "`r`n")

$desktopPath = [Environment]::GetFolderPath('Desktop')
$doc.SaveAs([ref] "$desktopPath\List of Files.docx")
$doc.Close()

$word.Quit()

[System.Runtime.Interopservices.Marshal]::ReleaseComObject($doc) | Out-Null
[System.Runtime.Interopservices.Marshal]::ReleaseComObject($word) | Out-Null
[System.GC]::Collect()
[System.GC]::WaitForPendingFinalizers()