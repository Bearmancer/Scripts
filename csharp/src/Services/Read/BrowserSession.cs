namespace CSharpScripts.Services.Read;

using Microsoft.Playwright;

internal sealed class BrowserSession : IAsyncDisposable
{
	private const string UserDataDirName = "pw_user_data_dir";
	private const int ViewportWidth = 1280;
	private const int ViewportHeight = 800;

	private readonly IPlaywright PlaywrightInstance;

	public IBrowserContext Browser { get; }

	private BrowserSession(IPlaywright playwright, IBrowserContext browser)
	{
		PlaywrightInstance = playwright;
		Browser = browser;
	}

	public static async Task<BrowserSession> CreateAsync(
		string? extensionPath = null,
		CancellationToken cancellationToken = default
	)
	{
		cancellationToken.ThrowIfCancellationRequested();

		IPlaywright pw = await Playwright.CreateAsync();
		var userDataDir = Path.GetFullPath(UserDataDirName);

		BrowserTypeLaunchPersistentContextOptions options = new()
		{
			Headless = false,
			ViewportSize = new() { Width = ViewportWidth, Height = ViewportHeight },
			AcceptDownloads = true,
		};

		if (!IsNullOrEmpty(extensionPath))
		{
			options.Args =
			[
				$"--disable-extensions-except={extensionPath}",
				$"--load-extension={extensionPath}",
			];
		}

		IBrowserContext browser = await pw.Chromium.LaunchPersistentContextAsync(
			userDataDir,
			options
		);
		return new BrowserSession(pw, browser);
	}

	public async Task<IPage> GetOrCreatePageAsync() =>
		Browser.Pages.Count > 0 ? Browser.Pages[0] : await Browser.NewPageAsync();

	public async ValueTask DisposeAsync()
	{
		await Browser.CloseAsync();
		PlaywrightInstance.Dispose();
	}
}
