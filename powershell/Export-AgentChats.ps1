





$script:DefaultOutputDir = 'C:\Users\Lance\Desktop\export_chat_strategy'

function New-SessionId {
    
    $unixMs = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()
    $hexTime = $unixMs.ToString("x12")
    $random = [Guid]::NewGuid().ToString("N").Substring(12)
    return "$($hexTime.Substring(0,8))-$($hexTime.Substring(8,4))-$($random.Substring(0,4))-$($random.Substring(4,4))-$($random.Substring(8,12))"
}

function Export-AgentChats {
    [CmdletBinding()]
    param(
        [ValidateSet('AGY', 'Cline', 'KiloCode', 'Codex', 'OpenCode', 'All')]
        [string]$Agent = 'All',
        [string]$OutputDir = $script:DefaultOutputDir
    )

    if (-not (Test-Path $OutputDir)) {
        New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
    }

    if ($Agent -in 'AGY',      'All') { _Export-AgyChats      -OutputDir (Join-Path $OutputDir 'AGY') }
    if ($Agent -in 'Cline',    'All') { _Export-ClineChats    -OutputDir (Join-Path $OutputDir 'Cline') }
    if ($Agent -in 'KiloCode', 'All') { _Export-KiloCodeChats -OutputDir (Join-Path $OutputDir 'KiloCode') }
    if ($Agent -in 'Codex',    'All') { _Export-CodexChats    -OutputDir (Join-Path $OutputDir 'Codex') }
    if ($Agent -in 'OpenCode', 'All') { _Export-OpenCodeChats -OutputDir (Join-Path $OutputDir 'OpenCode') }

    Write-Host "High-Fidelity Export complete -> $OutputDir" -ForegroundColor Green
}

function _Write-ChatJson {
    param([string]$Path, [object]$SessionObj)
    if (-not $SessionObj.Messages -or $SessionObj.Messages.Count -eq 0) { return }
    $dir = Split-Path $Path
    if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
    $SessionObj | ConvertTo-Json -Depth 6 | Out-File -FilePath $Path -Encoding utf8 -Force
}




function _Export-AgyChats {
    param([string]$OutputDir)
    $brainRoot = 'C:\Users\Lance\.gemini\antigravity-cli\brain'
    if (-not (Test-Path $brainRoot)) { return }

    if (Test-Path $OutputDir) { Remove-Item "$OutputDir\*" -Recurse -Force -ErrorAction SilentlyContinue }

    $transcripts = Get-ChildItem -Path $brainRoot -Recurse -Filter 'transcript.jsonl' -ErrorAction SilentlyContinue
    foreach ($file in $transcripts) {
        $convId  = ($file.FullName -split [regex]::Escape('\brain\'))[1] -split '\\' | Select-Object -First 1
        $outPath = Join-Path $OutputDir "${convId}.json"
        
        $lines = Get-Content $file.FullName -Encoding utf8 -ErrorAction SilentlyContinue
        $chat  = [System.Collections.Generic.List[object]]::new()

        foreach ($line in $lines) {
            try { $obj = $line | ConvertFrom-Json -ErrorAction Stop } catch { continue }
            
            $msg = @{ role = 'system'; content = '' }
            
            if ($obj.type -eq 'USER_INPUT') {
                $msg.role = 'user'
                $msg.content = if ($obj.content -is [string]) { $obj.content } else { $obj.content | ConvertTo-Json -Compress }
            }
            elseif ($obj.type -eq 'PLANNER_RESPONSE') {
                $msg.role = 'assistant'
                $msg.content = if ($obj.content -is [string]) { $obj.content } else { $obj.content | ConvertTo-Json -Compress }
                if ($obj.tool_calls) {
                    $msg.tool_calls = $obj.tool_calls
                }
            }
            elseif ($obj.type -match 'TOOL_RESPONSE|COMMAND_OUTPUT') {
                $msg.role = 'tool'
                $msg.content = if ($obj.content -is [string]) { $obj.content } else { $obj.content | ConvertTo-Json -Compress }
            }
            else {
                $msg.role = 'system'
                $msg.content = if ($obj.content -is [string]) { $obj.content } else { $obj.content | ConvertTo-Json -Compress }
            }

            if (-not [string]::IsNullOrWhiteSpace($msg.content) -or $msg.tool_calls) {
                $chat.Add($msg)
            }
        }
        
        $sessionObj = @{
            Metadata = @{ SessionId = (New-SessionId); OriginalId = $convId; Agent = 'AGY'; Timestamp = (Get-Date).ToString("o") }
            Messages = $chat
        }
        _Write-ChatJson -Path $outPath -SessionObj $sessionObj
    }
}

# -----------------------------------------------------------------------------
# Cline & KiloCode (Shared Logic)
# -----------------------------------------------------------------------------
function _Process-ClaudeDev {
    param([string]$rootDir, [string]$OutputDir, [string]$AgentName)
    if (-not (Test-Path $rootDir)) { return }
    if (Test-Path $OutputDir) { Remove-Item "$OutputDir\*" -Recurse -Force -ErrorAction SilentlyContinue }

    foreach ($taskDir in (Get-ChildItem -Path $rootDir -Directory -ErrorAction SilentlyContinue)) {
        $uiFile = Join-Path $taskDir.FullName 'ui_messages.json'
        if (-not (Test-Path $uiFile)) { continue }
        try { $messages = Get-Content $uiFile -Raw -Encoding utf8 | ConvertFrom-Json -ErrorAction Stop } catch { continue }
        
        $outPath = Join-Path $OutputDir "$($taskDir.Name).json"
        $chat    = [System.Collections.Generic.List[object]]::new()

        foreach ($msg in $messages) {
            $role = 'system'
            $content = if ($msg.text -is [string]) { $msg.text } else { $msg.text | ConvertTo-Json -Compress }
            $tool_calls = $null

            switch ($msg.say) {
                { $_ -in 'user_feedback', 'task' } { $role = 'user' }
                'text' { $role = 'assistant' }
                { $_ -in 'command', 'browser_action', 'mcp_call' } {
                    $role = 'assistant'
                    $tool_calls = @(@{ type = 'function'; function = @{ name = $msg.say; arguments = $msg.text } })
                    $content = ''
                }
                { $_ -in 'command_output', 'mcp_response' } {
                    $role = 'tool'
                }
            }

            if (-not [string]::IsNullOrWhiteSpace($content) -or $tool_calls) {
                $msgObj = @{ role = $role; content = $content }
                if ($tool_calls) { $msgObj.tool_calls = $tool_calls }
                $chat.Add($msgObj)
            }
        }
        $sessionObj = @{
            Metadata = @{ SessionId = (New-SessionId); OriginalId = $taskDir.Name; Agent = $AgentName; Timestamp = (Get-Date).ToString("o") }
            Messages = $chat
        }
        _Write-ChatJson -Path $outPath -SessionObj $sessionObj
    }
}

function _Export-ClineChats {
    param([string]$OutputDir)
    $root = "$env:APPDATA\Code - Insiders\User\globalStorage\saoudrizwan.claude-dev\tasks"
    if (-not (Test-Path $root)) { $root = "$env:APPDATA\Code\User\globalStorage\saoudrizwan.claude-dev\tasks" }
    _Process-ClaudeDev -rootDir $root -OutputDir $OutputDir -AgentName 'Cline'
}

function _Export-KiloCodeChats {
    param([string]$OutputDir)
    $root = "$env:APPDATA\Code - Insiders\User\globalStorage\kilocode.kilo-code\tasks"
    if (-not (Test-Path $root)) { $root = "$env:APPDATA\Code\User\globalStorage\kilocode.kilo-code\tasks" }
    _Process-ClaudeDev -rootDir $root -OutputDir $OutputDir -AgentName 'KiloCode'
}

# -----------------------------------------------------------------------------
# Codex
# -----------------------------------------------------------------------------
function _Export-CodexChats {
    param([string]$OutputDir)
    $codexRoot = "$env:USERPROFILE\.codex\sessions"
    if (-not (Test-Path $codexRoot)) { return }

    if (Test-Path $OutputDir) { Remove-Item "$OutputDir\*" -Recurse -Force -ErrorAction SilentlyContinue }

    $files = Get-ChildItem -Path $codexRoot -Recurse -Filter '*.jsonl' -ErrorAction SilentlyContinue
    foreach ($file in $files) {
        $outPath = Join-Path $OutputDir "$($file.BaseName).json"
        $chat    = [System.Collections.Generic.List[object]]::new()

        $lines = Get-Content $file.FullName -Encoding utf8 -ErrorAction SilentlyContinue
        foreach ($line in $lines) {
            try { $obj = $line | ConvertFrom-Json -ErrorAction Stop } catch { continue }
            if ($obj.type -eq 'response_item') {
                $payload = $obj.payload
                $role = switch ($payload.role) {
                    'developer' { 'system' }
                    'assistant' { 'assistant' }
                    'user'      { 'user' }
                    'tool'      { 'tool' }
                    default     { 'system' }
                }
                
                $textNode = $payload.content | Where-Object { $_.type -in 'input_text','output_text','text' } | Select-Object -First 1
                $text = if ($textNode) { $textNode.text } else { $null }

                $msgObj = @{ role = $role; content = $text }
                if ($payload.tool_calls) { $msgObj.tool_calls = $payload.tool_calls }
                
                if (-not [string]::IsNullOrWhiteSpace($text) -or $msgObj.tool_calls) {
                    $chat.Add($msgObj)
                }
            }
        }
        $sessionObj = @{
            Metadata = @{ SessionId = (New-SessionId); OriginalId = $file.BaseName; Agent = 'Codex'; Timestamp = (Get-Date).ToString("o") }
            Messages = $chat
        }
        _Write-ChatJson -Path $outPath -SessionObj $sessionObj
    }
}

# -----------------------------------------------------------------------------
# OpenCode
# -----------------------------------------------------------------------------
function _Export-OpenCodeChats {
    param([string]$OutputDir)
    $dbPath = 'C:\Users\Lance\.local\share\opencode\opencode.db'
    if (-not (Test-Path $dbPath)) { return }
    if (-not (Get-Command rsql -ErrorAction SilentlyContinue)) { return }

    if (Test-Path $OutputDir) { Remove-Item "$OutputDir\*" -Recurse -Force -ErrorAction SilentlyContinue }

    $dbUrl = "sqlite:///$($dbPath -replace '\\', '/')"
    # Extract all relevant events to reconstruct tools and system
    $query = @"
SELECT
    m.session_id,
    json_extract(m.data, '`$.role') AS role,
    json_extract(p.data, '`$.type') AS msg_type,
    json_extract(p.data, '`$.text') AS content,
    json_extract(p.data, '`$.tool_calls') AS tool_calls
FROM message m
JOIN part p ON m.id = p.message_id
WHERE json_extract(p.data, '`$.text') IS NOT NULL
   OR json_extract(p.data, '`$.tool_calls') IS NOT NULL
ORDER BY m.session_id, p.time_created ASC;
"@

    try { $records = rsql -u $dbUrl --format json --footer false --limit 1000000 -- $query | ConvertFrom-Json -ErrorAction Stop }
    catch { return }

    if (-not $records) { return }

    foreach ($group in ($records | Group-Object -Property session_id)) {
        if ([string]::IsNullOrWhiteSpace($group.Name)) { continue }
        $outPath = Join-Path $OutputDir "$($group.Name).json"
        $chat    = [System.Collections.Generic.List[object]]::new()
        foreach ($r in $group.Group) {
            $roleVal = if ([string]::IsNullOrWhiteSpace($r.role)) { 'system' } else { $r.role }
            $msgObj = @{ role = $roleVal; content = $r.content }
            if ($r.tool_calls) { $msgObj.tool_calls = ConvertFrom-Json $r.tool_calls }
            $chat.Add($msgObj)
        }
        $sessionObj = @{
            Metadata = @{ SessionId = (New-SessionId); OriginalId = $group.Name; Agent = 'OpenCode'; Timestamp = (Get-Date).ToString("o") }
            Messages = $chat
        }
        _Write-ChatJson -Path $outPath -SessionObj $sessionObj
    }
}

