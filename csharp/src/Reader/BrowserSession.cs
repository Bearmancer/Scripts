using Microsoft.Playwright;

namespace CSharpScripts.Services.Read;

internal sealed class BrowserSession : IAsyncDisposable
{
	private const string UserDataDirName = "pw_user_data_dir";
	private const int ViewportWidth = 1280;
	private const int ViewportHeight = 800;

	private readonly IPlaywright PlaywrightInstance;

	private BrowserSession(IPlaywright playwright, IBrowserContext browser)
	{
		PlaywrightInstance = playwright;
		Browser = browser;
	}

	public IBrowserContext Browser { get; }

	public async ValueTask DisposeAsync()
	{
		await Browser.CloseAsync();
		PlaywrightInstance.Dispose();
	}

	public static async Task<BrowserSession> CreateAsync(
		string? extensionPath = null,
		CancellationToken cancellationToken = default
	)
	{
		cancellationToken.ThrowIfCancellationRequested();

		IPlaywright pw = await Playwright.CreateAsync();
		var userDataDir = Path.GetFullPath(path: UserDataDirName);

		BrowserTypeLaunchPersistentContextOptions options = new()
		{
			Headless = false,
			ViewportSize = new ViewportSize { Width = ViewportWidth, Height = ViewportHeight },
			AcceptDownloads = true,
		};

		if (!IsNullOrEmpty(value: extensionPath))
		{
			options.Args =
			[
				$"--disable-extensions-except={extensionPath}",
				$"--load-extension={extensionPath}",
			];
		}

		IBrowserContext browser = await pw.Chromium.LaunchPersistentContextAsync(
			userDataDir: userDataDir,
			options: options
		);
		return new BrowserSession(playwright: pw, browser: browser);
	}

	public async Task<IPage> GetOrCreatePageAsync() =>
		Browser.Pages.Count > 0 ? Browser.Pages[index: 0] : await Browser.NewPageAsync();
}
