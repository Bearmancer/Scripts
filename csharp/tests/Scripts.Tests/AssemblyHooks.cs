using System.Diagnostics.Tracing;
using System.Text;
using Azure.Core.Diagnostics;

namespace Scripts.Tests;

internal static class AssemblyHooks
{
	[Before(Assembly)]
	public static void Setup(AssemblyHookContext context) => Initialize();

	[After(Assembly)]
	public static void Teardown(AssemblyHookContext context) { }

	private static void Initialize()
	{
		ConfigureAzConfigDir();
		EnableAzureVerboseLogging();
	}

	private static void ConfigureAzConfigDir()
	{
		var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		var dir = Path.Combine(localAppData, $"scripts-tests-az-{Guid.NewGuid():N}");
		Directory.CreateDirectory(dir);
		Environment.SetEnvironmentVariable("AZURE_CONFIG_DIR", dir);
		Environment.SetEnvironmentVariable("TMP", dir);
		Environment.SetEnvironmentVariable("TEMP", dir);
	}

	private static void EnableAzureVerboseLogging()
	{
		_ = new AzureEventSourceListener(
			(eventData, message) =>
			{
				if (eventData.EventSource.Name != "Azure-Identity")
					return;
				try
				{
					var line =
						$"[{DateTime.Now:HH:mm:ss.fff}][{eventData.Level}][{eventData.EventSource.Name}] {message}\r\n";
					Console.OpenStandardError().Write(Encoding.UTF8.GetBytes(line));
				}
				catch
				{
				}
			},
			level: EventLevel.Verbose);
	}
}
