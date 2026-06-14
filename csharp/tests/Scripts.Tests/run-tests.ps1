$env:AZURE_TRANSLATOR_ENDPOINT = "https://api.cognitive.microsofttranslator.com"
$env:AZURE_TRANSLATOR_REGION = "centralindia"
$env:AZURE_VISION_ENDPOINT = "https://lance-resource.cognitiveservices.azure.com/"
$env:AZURE_OPENAI_ENDPOINT = "https://openai-lance-b8469.openai.azure.com/"
$env:AZURE_OPENAI_DEPLOYMENT = "gpt-4o"
$env:AZURE_OPENAI_DEPLOYMENT_NAME = "gpt-4o-mini"
$env:AZURE_OPENAI_WHISPER_DEPLOYMENT_NAME = "whisper"
$env:AZURE_DOCUMENT_INTELLIGENCE_ENDPOINT = "https://document-intelligence-lance.cognitiveservices.azure.com/"

Set-Location "C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests"
dotnet run --project Scripts.Tests.csproj --no-build
