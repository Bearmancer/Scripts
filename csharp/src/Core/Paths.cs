namespace CSharpScripts.Core;

internal static class Paths
{
	public static readonly string ProjectRoot = FindAncestorContaining(".git");
	public static readonly string LogDirectory = Path.Combine(ProjectRoot, "logs");
	public static readonly string StateDirectory = Path.Combine(ProjectRoot, "state");
	public static readonly string DumpsDirectory = Path.Combine(StateDirectory, "dump");
	public static readonly string CacheDirectory = Path.Combine(StateDirectory, "cache");
	public const string ExportsDirectory =
		@"C:\Users\Lance\Google Drive\My Drive\Spreadsheets\Boxed Sets";

	private static string FindAncestorContaining(string marker)
	{
		DirectoryInfo? dir = new(AppContext.BaseDirectory);
		while (dir is not null)
		{
			if (
				Directory.Exists(Path.Combine(dir.FullName, marker))
				|| File.Exists(Path.Combine(dir.FullName, marker))
			)
				return dir.FullName;

			dir = dir.Parent;
		}
		throw new DirectoryNotFoundException($"Could not find ancestor containing '{marker}'");
	}
}
