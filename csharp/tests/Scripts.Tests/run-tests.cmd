@echo off
set AZURE_TRANSLATOR_ENDPOINT=https://api.cognitive.microsofttranslator.com
set AZURE_TRANSLATOR_REGION=centralindia
set AZURE_VISION_ENDPOINT=https://lance-resource.cognitiveservices.azure.com/
set AZURE_OPENAI_ENDPOINT=https://openai-lance-b8469.openai.azure.com/
set AZURE_OPENAI_DEPLOYMENT=gpt-4o
set AZURE_OPENAI_DEPLOYMENT_NAME=gpt-4o-mini
set AZURE_OPENAI_WHISPER_DEPLOYMENT_NAME=whisper
set AZURE_DOCUMENT_INTELLIGENCE_ENDPOINT=https://document-intelligence-lance.cognitiveservices.azure.com/

cd /d "C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests"
dotnet run --project Scripts.Tests.csproj --no-build
