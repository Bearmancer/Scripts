param([string]$TaskName)
try {
    $TaskInfo = Get-ScheduledTaskInfo -TaskName $TaskName -ErrorAction Stop
    if ($TaskInfo.LastTaskResult -ne 0) {
        $ErrorCode = "0x{0:X8}" -f $TaskInfo.LastTaskResult
        $Message = "Task '$TaskName' Failed!`nExit: $($TaskInfo.LastTaskResult) ($ErrorCode)`nRun: $($TaskInfo.LastRunTime)"
        Add-Type -AssemblyName System.Windows.Forms
        [System.Windows.Forms.MessageBox]::Show($Message,'Failed','OK','Error')
        try { Write-EventLog -LogName Application -Source 'LastFM Monitor' -EventID 1001 -EntryType Error -Message $Message } catch { Write-Warning "Event log failed: $_" }
    }
} catch {
    Add-Type -AssemblyName System.Windows.Forms
    [System.Windows.Forms.MessageBox]::Show("Error: $($_.Exception.Message)",'Error','OK','Warning')
}
