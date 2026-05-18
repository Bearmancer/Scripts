namespace CSharpScripts.Core;

internal static class Paths
{
	public const string ExportsDirectory =
		@"C:\Users\Lance\Google Drive\My Drive\Spreadsheets\Boxed Sets";

	public static readonly string ProjectRoot = FindAncestorContaining(marker: ".git");
	public static readonly string LogDirectory = Path.Combine(path1: ProjectRoot, path2: "logs");
	public static readonly string StateDirectory = Path.Combine(path1: ProjectRoot, path2: "state");
	public static readonly string DumpsDirectory = Path.Combine(
		path1: StateDirectory,
		path2: "dump"
	);
	public static readonly string CacheDirectory = Path.Combine(
		path1: StateDirectory,
		path2: "cache"
	);

	private static string FindAncestorContaining(string marker)
	{
		var dir = AppContext.BaseDirectory;
		while (dir is not null)
		{
			var candidate = Path.Combine(dir, marker);
			if (Directory.Exists(candidate) || File.Exists(candidate))
				return dir;
			dir = Path.GetDirectoryName(dir);
		}
		throw new DirectoryNotFoundException($"Could not find ancestor containing '{marker}'");
	}
}


