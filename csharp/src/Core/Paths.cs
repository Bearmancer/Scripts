namespace Scripts.Core;

internal static class Paths
{
	public const string ExportsDirectory =
		@"C:\Users\Lance\Google Drive\My Drive\Spreadsheets\Boxed Sets";

	public static readonly string ProjectRoot = FindAncestorContaining(marker: ".git");
	public static readonly string LogDirectory = Path.Combine(
		path1: Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
		path2: ".cache",
		path3: "logs",
		path4: "scripts"
	);
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
		while (dir is { })
		{
			var candidate = Path.Combine(path1: dir, path2: marker);
			if (Directory.Exists(path: candidate) || File.Exists(path: candidate))
				return dir;

			dir = Path.GetDirectoryName(path: dir);
		}
		throw new DirectoryNotFoundException($"Could not find ancestor containing '{marker}'");
	}
}
