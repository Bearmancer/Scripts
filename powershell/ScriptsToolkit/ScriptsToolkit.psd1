@{
	RootModule = 'ScriptsToolkit.psm1'
	ModuleVersion = '5.5.0'
	GUID = '3e9ddc4f-071f-4b0a-a5d0-1bc364f204c8'
	Author = 'Lance'
	CompanyName = 'Personal'
	Description = 'CLI wrappers for C# scripts, whisper transcription, and utilities'
	PowerShellVersion = '7.5'

	FunctionsToExport = @(
		'Invoke-Tools'
		'Sync-YouTube', 'Sync-LastFm', 'Sync-All'
		'Invoke-Toolkit'
		'Convert-Audio', 'Rename-AudioRed', 'Get-AudioArtReport'
		'Invoke-Remux', 'Compress-Video'
		'Get-VideoChapters', 'Get-VideoResolutions', 'New-Gif', 'Get-VideoThumbnails'
		'New-Torrent'
		'Invoke-Whisper', 'Invoke-WhisperEnglish', 'Invoke-WhisperJapanese'
		'Save-YouTubeDownload'
		'Register-LastFmSyncTask', 'Register-YouTubeSyncTask'
		'Register-AllSyncTasks', 'Unregister-AllSyncTasks'
		'Get-SyncLog', 'Get-YouTubePlaylistLog', 'Get-SyncStatus'
		'Invoke-Propolis', 'Get-ToolkitCommand'
	)

	AliasesToExport = @(
		'tools'
		'syncyt', 'music', 'sync'
		'sacd', 'renred', 'artreport'
		'remux', 'hb'
		'gif', 'mktor'
		'whisper', 'whisp', 'wpj'
		'ytdl'
		'reglfm', 'regyt', 'regall', 'unreg'
		'viewlog', 'ytlog', 'syncstatus', 'propolis', 'stk'
	)
}
