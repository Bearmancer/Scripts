$ErrorActionPreference = 'Stop'
$repoRoot = Resolve-Path "$PSScriptRoot\..\.."
Set-Location $repoRoot
dotnet run --project "csharp/tests/CSharpScripts.Tests/CSharpScripts.Tests.csproj"
if ($LASTEXITCODE -ne 0) {
    throw "TESTS_FAILED"
}
Write-Output "ALL_TESTS_PASS"
