using System.Reflection;
using CSharpScripts.CLI.Read;
using CSharpScripts.Core;
using CSharpScripts.Core.Auth;
using CSharpScripts.Services.Language;
using CSharpScripts.Services.Read.Ocr;
using AssertionExtensions = FluentAssertions.AssertionExtensions;

namespace CSharpScripts.Tests;

internal sealed class SmokeTests
{
	[Test]
	public void Smoke_TestInfrastructure_Passes() => AssertionExtensions.Should(true).BeTrue();

	[Test]
	public void WhenReadSourceIsSupportedImageThenItIsRecognizedAsLocalImage()
	{
		AssertionExtensions.Should(ReadCommand.IsImage(path: "disc-page.jpg")).BeTrue();
		AssertionExtensions.Should(ReadCommand.IsImage(path: "disc-page.jpeg")).BeTrue();
		AssertionExtensions.Should(ReadCommand.IsImage(path: "disc-page.png")).BeTrue();
	}

	[Test]
	public void WhenReadSourceIsNotSupportedImageThenItIsRejected() =>
		AssertionExtensions.Should(ReadCommand.IsImage(path: "disc-page.webp")).BeFalse();
}

[NotInParallel]
internal sealed class StateManagerTests
{
	private string TestDirectory = null!;

	[Before(hookType: Test)]
	public void Setup()
	{
		TestDirectory = Path.Combine(
			Paths.ProjectRoot,
			".test-tmp",
			Guid.NewGuid().ToString(format: "N")
		);
		StateManager.RootDirectory = TestDirectory;
	}

	[After(hookType: Test)]
	public void Cleanup()
	{
		if (TestDirectory is not null && Directory.Exists(path: TestDirectory))
		{
			try
			{
				Directory.Delete(path: TestDirectory, recursive: true);
			}
			catch (IOException) { }
		}
	}

	[Test]
	public async Task WhenConcurrentAccessOccursThenNoCorruptionHappens()
	{
		var testFile = "concurrent-test.json";
		List<Task> tasks = [];
		var successCount = 0;
		var unauthorizedCount = 0;

		for (var i = 0; i < 10; i++)
		{
			var index = i;
			tasks.Add(
				Task.Run(async () =>
				{
					try
					{
						await StateManager.SaveStateAsync(
							fileName: testFile,
							new TestState { Value = index }
						);
						TestState loaded = await StateManager.LoadStateAsync<TestState>(
							fileName: testFile
						);
						AssertionExtensions.Should(loaded).NotBeNull();
						Interlocked.Increment(location: ref successCount);
					}
					catch (UnauthorizedAccessException)
					{
						Interlocked.Increment(location: ref unauthorizedCount);
					}
				})
			);
		}

		await Task.WhenAll(tasks: tasks);

		AssertionExtensions.Should(successCount).BeGreaterThan(expected: 0);

		TestState finalState = await StateManager.LoadStateAsync<TestState>(fileName: testFile);
		AssertionExtensions.Should(finalState).NotBeNull();
		AssertionExtensions.Should(finalState.Value).BeInRange(minimumValue: 0, maximumValue: 9);
	}

	[Test]
	public async Task WhenJsonIsCorruptedThenReturnsDefaultState()
	{
		var testFile = "corrupted.json";
		Directory.CreateDirectory(path: TestDirectory);
		var path = Path.Combine(path1: TestDirectory, path2: testFile);

		await File.WriteAllTextAsync(path: path, contents: "{ invalid json }}}");

		TestState state = await StateManager.LoadStateAsync<TestState>(fileName: testFile);

		AssertionExtensions.Should(state).NotBeNull();
		AssertionExtensions.Should(state.Value).Be(expected: 0);
		AssertionExtensions.Should(File.Exists(path + ".corrupted")).BeTrue();
	}

	[Test]
	public void WhenCheckingMethodNamesThenAllArePascalCase()
	{
		IEnumerable<MethodInfo> methods = Enumerable.Where(
			Enumerable.Where(typeof(StateManager).GetMethods(), m => m.IsPublic || m.IsAssembly),
			m => !m.IsSpecialName
		);

		foreach (MethodInfo method in methods)
		{
			AssertionExtensions
				.Should(char.IsUpper(method.Name[index: 0]))
				.BeTrue($"Method {method.Name} should start with uppercase letter (PascalCase)");
		}
	}

	[Test]
	public async Task WhenDeleteAllStatesIsCalledThenAllStateFilesAreRemoved()
	{
		await StateManager.SaveStateAsync(fileName: "test1.json", new TestState { Value = 1 });
		await StateManager.SaveStateAsync(fileName: "test2.json", new TestState { Value = 2 });
		await StateManager.SaveStateAsync(
			fileName: "nested/test3.json",
			new TestState { Value = 3 }
		);

		AssertionExtensions.Should(Directory.Exists(path: TestDirectory)).BeTrue();
		AssertionExtensions
			.Should(
				Directory.GetFiles(
					path: TestDirectory,
					searchPattern: "*.json",
					searchOption: SearchOption.AllDirectories
				)
			)
			.HaveCountGreaterThanOrEqualTo(expected: 3);

		StateManager.DeleteAllStates();

		AssertionExtensions.Should(Directory.Exists(path: TestDirectory)).BeFalse();
	}

	[Test]
	public async Task WhenSavingStateThenWriteIsAtomic()
	{
		var testFile = "atomic-test.json";
		var state = new TestState { Value = 42 };

		await StateManager.SaveStateAsync(fileName: testFile, state: state);

		var path = Path.Combine(path1: TestDirectory, path2: testFile);
		AssertionExtensions.Should(File.Exists(path: path)).BeTrue();

		var tmpFiles = Directory.GetFiles(
			path: TestDirectory,
			searchPattern: "*.tmp",
			searchOption: SearchOption.AllDirectories
		);
		AssertionExtensions.Should(tmpFiles).BeEmpty();

		TestState loaded = await StateManager.LoadStateAsync<TestState>(fileName: testFile);
		AssertionExtensions.Should(loaded.Value).Be(expected: 42);
	}

	[Test]
	public async Task WhenCancellationTokenIsCancelledThenLoadOperationThrows()
	{
		using var cts = new CancellationTokenSource();
		await cts.CancelAsync();

		async Task act() =>
			await StateManager.LoadStateAsync<TestState>(fileName: "test.json", ct: cts.Token);
		await AssertionExtensions.Should(act).ThrowAsync<OperationCanceledException>();
	}

	[Test]
	public async Task WhenSavingWithCancellationTokenThenTokenIsRespected()
	{
		using var cts = new CancellationTokenSource();
		await cts.CancelAsync();

		async Task act() =>
			await StateManager.SaveStateAsync(
				fileName: "test.json",
				new TestState(),
				ct: cts.Token
			);
		await AssertionExtensions.Should(act).ThrowAsync<OperationCanceledException>();
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
		Environment.SetEnvironmentVariable(variable: "GOOGLE_CLIENT_ID", value: null);

		static void act() => _ = Secrets.GoogleClientId;

		AssertionExtensions
			.Should(act)
			.Throw<TypeInitializationException>()
			.WithInnerException<InvalidOperationException>()
			.WithMessage(expectedWildcardPattern: "*GOOGLE_CLIENT_ID*");
	}

	[Test]
	public void WhenAzureDocumentIntelligenceModelUnsetThenDefaultModelIsLayout()
	{
		Environment.SetEnvironmentVariable(
			variable: "AZURE_DOCUMENT_INTELLIGENCE_MODEL_ID",
			value: null
		);

		AssertionExtensions
			.Should(Secrets.AzureDocumentIntelligenceModelId)
			.Be(expected: "prebuilt-layout");
	}

	[Test]
	public void WhenAzureDocumentIntelligenceEndpointUnsetThenHardcodedDefaultIsUsed()
	{
		Environment.SetEnvironmentVariable(
			variable: "AZURE_DOCUMENT_INTELLIGENCE_ENDPOINT",
			value: null
		);

		AssertionExtensions
			.Should(Secrets.AzureDocumentIntelligenceEndpoint)
			.Be(expected: "https://document-intelligence-lance.cognitiveservices.azure.com/");
	}

	[Test]
	public void WhenAzureDocumentIntelligenceOptionsAreProvidedThenEnvironmentVariablesAreNotRequired()
	{
		Environment.SetEnvironmentVariable(
			variable: "AZURE_DOCUMENT_INTELLIGENCE_ENDPOINT",
			value: null
		);
		Environment.SetEnvironmentVariable(
			variable: "AZURE_DOCUMENT_INTELLIGENCE_KEY",
			value: null
		);

		AssertionExtensions
			.Should(
				AzureDocumentIntelligenceOcrProvider.IsConfigured(
					new AzureDocumentIntelligenceOptions(
						Endpoint: "https://example.cognitiveservices.azure.com/",
						ApiKey: "test-key",
						ModelId: null
					)
				)
			)
			.BeTrue();
	}

	[Test]
	public void WhenOnlyAzureDocumentIntelligenceApiKeyIsProvidedThenHardcodedEndpointStillWorks()
	{
		Environment.SetEnvironmentVariable(
			variable: "AZURE_DOCUMENT_INTELLIGENCE_ENDPOINT",
			value: null
		);

		static void act() =>
			_ = AzureDocumentIntelligenceOcrProvider.CreateConfigured(
				new AzureDocumentIntelligenceOptions(
					Endpoint: null,
					ApiKey: "test-key",
					ModelId: null
				)
			);

		AssertionExtensions.Should(act).NotThrow();
	}
}

internal sealed class LanguageIdentifierTests
{
	[Test]
	public void WhenProfileFileMissingThenGracefullyHandles()
	{
		static void act() => LanguageIdentifier.Detect(text: "test");

		AssertionExtensions.Should(act).NotThrow<NullReferenceException>();
	}
}
