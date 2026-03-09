using System.Text.Json;
using CSharpScripts.Core;
using FluentAssertions;
using TUnit;

namespace CSharpScripts.Tests;

internal sealed class SmokeTests
{
	[Test]
	public void Smoke_TestInfrastructure_Passes() => true.Should().BeTrue();

	[Test]
	public void WhenReadSourceIsSupportedImageThenItIsRecognizedAsLocalImage()
	{
		CLI.Read.ReadCommand.IsImage("disc-page.jpg").Should().BeTrue();
		CLI.Read.ReadCommand.IsImage("disc-page.jpeg").Should().BeTrue();
		CLI.Read.ReadCommand.IsImage("disc-page.png").Should().BeTrue();
	}

	[Test]
	public void WhenReadSourceIsNotSupportedImageThenItIsRejected()
	{
		CLI.Read.ReadCommand.IsImage("disc-page.webp").Should().BeFalse();
	}
}

[NotInParallel]
internal sealed class StateManagerTests
{
	private string TestDirectory = null!;

	[Before(Test)]
	public void Setup()
	{
		TestDirectory = Path.Combine(Path.GetTempPath(), $"statemanager-tests-{Guid.NewGuid()}");
		StateManager.RootDirectory = TestDirectory;
		Directory.CreateDirectory(TestDirectory);
	}

	[After(Test)]
	public async Task Cleanup()
	{
		if (Directory.Exists(TestDirectory))
		{
			await Task.Run(() => Directory.Delete(TestDirectory, recursive: true));
		}
	}

	[Test]
	public async Task WhenConcurrentAccessOccursThenNoCorruptionHappens()
	{
		var testFile = "concurrent-test.json";
		var tasks = new List<Task>();
		var successCount = 0;

		for (int i = 0; i < 10; i++)
		{
			int index = i;
			tasks.Add(
				Task.Run(async () =>
				{
					try
					{
						await StateManager.SaveStateAsync(
							testFile,
							new TestState { Value = index }
						);
						var loaded = await StateManager.LoadStateAsync<TestState>(testFile);
						loaded.Should().NotBeNull();
						Interlocked.Increment(ref successCount);
					}
					catch (UnauthorizedAccessException) { }
				})
			);
		}

		await Task.WhenAll(tasks);

		successCount.Should().BeGreaterThan(0);

		var finalState = await StateManager.LoadStateAsync<TestState>(testFile);
		finalState.Should().NotBeNull();
		finalState.Value.Should().BeInRange(0, 9);
	}

	[Test]
	public async Task WhenJsonIsCorruptedThenReturnsDefaultState()
	{
		var testFile = "corrupted.json";
		var path = Path.Combine(TestDirectory, testFile);

		await File.WriteAllTextAsync(path, "{ invalid json }}}");

		var state = await StateManager.LoadStateAsync<TestState>(testFile);

		state.Should().NotBeNull();
		state.Value.Should().Be(0);
		File.Exists(path + ".corrupted").Should().BeTrue();
	}

	[Test]
	public void WhenCheckingMethodNamesThenAllArePascalCase()
	{
		var methods = typeof(StateManager)
			.GetMethods()
			.Where(m => m.IsPublic || m.IsAssembly)
			.Where(m => !m.IsSpecialName);

		foreach (var method in methods)
		{
			char.IsUpper(method.Name[0])
				.Should()
				.BeTrue($"Method {method.Name} should start with uppercase letter (PascalCase)");
		}
	}

	[Test]
	public async Task WhenDeleteAllStatesIsCalledThenAllStateFilesAreRemoved()
	{
		await StateManager.SaveStateAsync("test1.json", new TestState { Value = 1 });
		await StateManager.SaveStateAsync("test2.json", new TestState { Value = 2 });
		await StateManager.SaveStateAsync("nested/test3.json", new TestState { Value = 3 });

		Directory.Exists(TestDirectory).Should().BeTrue();
		Directory
			.GetFiles(TestDirectory, "*.json", SearchOption.AllDirectories)
			.Should()
			.HaveCountGreaterThanOrEqualTo(3);

		StateManager.DeleteAllStates();

		Directory.Exists(TestDirectory).Should().BeFalse();
	}

	[Test]
	public async Task WhenSavingStateThenWriteIsAtomic()
	{
		var testFile = "atomic-test.json";
		var state = new TestState { Value = 42 };

		await StateManager.SaveStateAsync(testFile, state);

		var path = Path.Combine(TestDirectory, testFile);
		File.Exists(path).Should().BeTrue();

		var tmpFiles = Directory.GetFiles(TestDirectory, "*.tmp", SearchOption.AllDirectories);
		tmpFiles.Should().BeEmpty();

		var loaded = await StateManager.LoadStateAsync<TestState>(testFile);
		loaded.Value.Should().Be(42);
	}

	[Test]
	public async Task WhenCancellationTokenIsCancelledThenLoadOperationThrows()
	{
		using var cts = new CancellationTokenSource();
		await cts.CancelAsync();

		Func<Task> act = async () =>
			await StateManager.LoadStateAsync<TestState>("test.json", cts.Token);
		await act.Should().ThrowAsync<OperationCanceledException>();
	}

	[Test]
	public async Task WhenSavingWithCancellationTokenThenTokenIsRespected()
	{
		using var cts = new CancellationTokenSource();
		await cts.CancelAsync();

		Func<Task> act = async () =>
			await StateManager.SaveStateAsync("test.json", new TestState(), cts.Token);
		await act.Should().ThrowAsync<OperationCanceledException>();
	}

	private sealed class TestState
	{
		public int Value { get; set; }
	}
}

internal sealed class SecretsTests
{
	[Test]
	public void WhenRequiredSecretIsMissingThenThrowsInvalidOperationException()
	{
		Environment.SetEnvironmentVariable("GOOGLE_CLIENT_ID", null);

		Action act = () => _ = Core.Auth.Secrets.GoogleClientId;

		act.Should().Throw<InvalidOperationException>().WithMessage("*GOOGLE_CLIENT_ID*");
	}

	[Test]
	public void WhenAzureDocumentIntelligenceModelUnsetThenDefaultModelIsLayout()
	{
		Environment.SetEnvironmentVariable("AZURE_DOCUMENT_INTELLIGENCE_MODEL_ID", null);

		Core.Auth.Secrets.AzureDocumentIntelligenceModelId.Should().Be("prebuilt-layout");
	}

	[Test]
	public void WhenAzureDocumentIntelligenceEndpointUnsetThenHardcodedDefaultIsUsed()
	{
		Environment.SetEnvironmentVariable("AZURE_DOCUMENT_INTELLIGENCE_ENDPOINT", null);

		Core.Auth.Secrets.AzureDocumentIntelligenceEndpoint.Should().Be(
			"https://document-intelligence-lance.cognitiveservices.azure.com/"
		);
	}

	[Test]
	public void WhenAzureDocumentIntelligenceOptionsAreProvidedThenEnvironmentVariablesAreNotRequired()
	{
		Environment.SetEnvironmentVariable("AZURE_DOCUMENT_INTELLIGENCE_ENDPOINT", null);
		Environment.SetEnvironmentVariable("AZURE_DOCUMENT_INTELLIGENCE_KEY", null);

		Services.Read.Ocr.AzureDocumentIntelligenceOcrProvider.IsConfigured(
			new Services.Read.Ocr.AzureDocumentIntelligenceOptions(
				"https://example.cognitiveservices.azure.com/",
				"test-key",
				null
			)
		)
			.Should()
			.BeTrue();
	}

	[Test]
	public void WhenOnlyAzureDocumentIntelligenceApiKeyIsProvidedThenHardcodedEndpointStillWorks()
	{
		Environment.SetEnvironmentVariable("AZURE_DOCUMENT_INTELLIGENCE_ENDPOINT", null);

		Action act = () =>
			_ = Services.Read.Ocr.AzureDocumentIntelligenceOcrProvider.CreateConfigured(
				new Services.Read.Ocr.AzureDocumentIntelligenceOptions(null, "test-key", null)
			);

		act.Should().NotThrow();
	}
}

internal sealed class LanguageIdentifierTests
{
	[Test]
	public void WhenProfileFileMissingThenGracefullyHandles()
	{
		Action act = () => Services.Language.LanguageIdentifier.Detect("test");

		act.Should().NotThrow<NullReferenceException>();
	}
}
