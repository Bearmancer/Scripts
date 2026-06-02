using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using TUnit;

namespace Scripts.Tests.Language;

internal sealed class LinguaPackageReferenceTests
{
	private static readonly string CsprojPath = TestPaths.Combine("csharp", "Scripts.csproj");

	[Test]
	public async Task Csproj_References_SearchPioneer_Lingua()
	{
		var xml = await File.ReadAllTextAsync(CsprojPath);
		var doc = System.Xml.Linq.XDocument.Parse(xml);
		var ns = doc.Root!.GetDefaultNamespace();

		var linguaRef = doc.Root!.Elements(ns + "ItemGroup")
			.SelectMany(g => g.Elements(ns + "PackageReference"))
			.FirstOrDefault(e => e.Attribute("Include")?.Value == "SearchPioneer.Lingua");

		linguaRef
			.Should()
			.NotBeNull("because LanguageIdentifier now uses SearchPioneer.Lingua v1.0.5");
	}

	[Test]
	public async Task Csproj_Lingua_Version_Is_Floating()
	{
		// Repo policy: always use latest NuGet (per "use * not hardcoded versions" rule).
		// Version="*" means NuGet picks the latest stable on every restore.
		var xml = await File.ReadAllTextAsync(CsprojPath);
		var doc = System.Xml.Linq.XDocument.Parse(xml);
		var ns = doc.Root!.GetDefaultNamespace();

		var linguaRef = doc.Root!.Elements(ns + "ItemGroup")
			.SelectMany(g => g.Elements(ns + "PackageReference"))
			.FirstOrDefault(e => e.Attribute("Include")?.Value == "SearchPioneer.Lingua");

		linguaRef.Should().NotBeNull();

		var version = linguaRef!.Attribute("Version")?.Value;
		version.Should().Be("*",
			$"because the repo always uses latest NuGet packages. Actual: {version}");
	}

	[Test]
	public async Task Resolved_Lingua_NuGet_Version_Is_Latest_Stable()
	{
		// Validates that floating `*` resolves to a real stable semver
		// (not "*-*" prerelease nor a missing package) at restore time.
		// Cross-checks against the live NuGet flat-container index so we know
		// the restore actually picked the latest stable, not just any stable.
		var packageId = "SearchPioneer.Lingua";
		var lockFile = TestPaths.Combine("csharp", "obj", "project.assets.json");
		File.Exists(lockFile).Should().BeTrue(
			"dotnet restore must have produced project.assets.json"
		);

		var assets = await File.ReadAllTextAsync(lockFile);
		using var doc = JsonDocument.Parse(assets);

		var resolvedVersion = ResolvePackageVersion(doc, packageId);
		resolvedVersion.Should().NotBeNull(
			$"because {packageId} must appear in the resolved package graph"
		);

		resolvedVersion.Should().MatchRegex(@"^\d+\.\d+\.\d+$",
			"because floating '*' must resolve to a stable semver (no prerelease tag like '-alpha', '-rc1')");

		var latestStable = await GetLatestStableVersionAsync(packageId);
		latestStable.Should().NotBeNull(
			$"because nuget.org must list at least one stable version for {packageId}"
		);

		resolvedVersion.Should().Be(latestStable,
			$"because '*' should always restore the latest stable from nuget.org");
	}

	private static string? ResolvePackageVersion(JsonDocument doc, string packageId)
	{
		if (!doc.RootElement.TryGetProperty("targets", out var targets)) return null;
		if (!targets.TryGetProperty("net10.0", out var tfm)) return null;

		// The version is encoded in the property name: "<PackageId>/<Version>".
		foreach (var prop in tfm.EnumerateObject())
			if (prop.Name.StartsWith($"{packageId}/", StringComparison.Ordinal))
				return prop.Name.Substring(packageId.Length + 1);

		return null;
	}

	private static async Task<string?> GetLatestStableVersionAsync(string packageId)
	{
		var lowerId = packageId.ToLowerInvariant();
		var url = new Uri($"https://api.nuget.org/v3-flatcontainer/{lowerId}/index.json");
		using var http = new HttpClient();
		var json = await http.GetStringAsync(url);
		using var doc = JsonDocument.Parse(json);

		if (!doc.RootElement.TryGetProperty("versions", out var versions)) return null;

		string? latest = null;
		foreach (var v in versions.EnumerateArray())
		{
			var s = v.GetString();
			if (s is null) continue;
			if (!Regex.IsMatch(s, @"^\d+\.\d+\.\d+$")) continue;
			if (latest is null || string.Compare(s, latest, StringComparison.Ordinal) > 0)
				latest = s;
		}
		return latest;
	}
}
