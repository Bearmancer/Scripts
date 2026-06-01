namespace Scripts.Infrastructure;

public static class Paths
{
	public static readonly string ProjectRoot = FindAncestorContaining(".git");
	public static readonly string LogDirectory = Combine(ProjectRoot, "logs");
	public static readonly string StateDirectory = Combine(ProjectRoot, "state");
	public static readonly string DumpsDirectory = Combine(StateDirectory, "dump");
	public static readonly string CacheDirectory = Combine(StateDirectory, "cache");
	public static readonly string ExportsDirectory =
		@"C:\Users\Lance\Google Drive\My Drive\Spreadsheets\Boxed Sets";

	private static string FindAncestorContaining(string marker)
	{
		DirectoryInfo? dir = new(AppContext.BaseDirectory);
		while (dir is { })
		{
			if (
				Directory.Exists(Combine(dir.FullName, marker))
				|| File.Exists(Combine(dir.FullName, marker))
			)
				return dir.FullName;

			dir = dir.Parent;
		}
		throw new DirectoryNotFoundException($"Could not find ancestor containing '{marker}'");
	}
}
