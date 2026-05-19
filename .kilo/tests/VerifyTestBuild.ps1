$ErrorActionPreference = 'Stop'
Set-Location "C:\Users\Lance\Dev\Scripts"
dotnet restore "csharp/tests/CSharpScripts.Tests/CSharpScripts.Tests.csproj"
dotnet build "csharp/tests/CSharpScripts.Tests/CSharpScripts.Tests.csproj" --no-restore
if ($LASTEXITCODE -ne 0) {
    throw "BUILD_FAILED"
}
Write-Output "BUILD_PASS"
