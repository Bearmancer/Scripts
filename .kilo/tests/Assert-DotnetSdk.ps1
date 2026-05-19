$ErrorActionPreference = 'Stop'
$out = dotnet --list-sdks 2>&1
if (-not ($out -match '10\.\d+\.\d+')) {
    throw "FAIL: No .NET 10 SDK found. Install from https://dotnet.microsoft.com/download/dotnet/10.0`n$out"
}
Write-Host "PASS: .NET 10 SDK detected."
