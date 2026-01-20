@{
    RootModule        = 'ScriptsToolkit.psm1'
    ModuleVersion     = '5.1.0'
    GUID              = '3e9ddc4f-071f-4b0a-a5d0-1bc364f204c8'
    Author            = 'Lance'
    CompanyName       = 'Personal'
    Description       = 'CLI wrappers for C# scripts, whisper transcription, and utilities'
    PowerShellVersion = '7.5'

    FunctionsToExport = @(
        # CLI Wrappers
        'Invoke-Scripts'
        'Sync-YouTube', 'Sync-LastFm', 'Sync-All'

        # Whisper Transcription
        'Invoke-Whisper', 'Invoke-WhisperEnglish', 'Invoke-WhisperJapanese'

        # YouTube
        'Save-YouTubeDownload'

        # Scheduled Tasks
        'Register-SyncTask', 'Register-AllSyncTasks'

        # Utilities
        'View-SyncLog', 'Invoke-Propolis', 'Get-ScriptsToolkitCommand'
    )

    AliasesToExport   = @(
        'scripts'
        'syncyt', 'synclf', 'syncall'
        'whisper', 'whisp', 'wpj'
        'ytdl'
        'regtask', 'regall'
        'synclog', 'propolis', 'stk'
    )
}
