using System.Text.Json;
using System.Text.RegularExpressions;

namespace Scripts.Tests.Language;

internal sealed class LinguaPackageReferenceTests
{
	private static readonly string CsprojPath = TestPaths.Combine("csharp", "Scripts.csproj");
	private static readonly string LockFilePath = TestPaths.Combine(
		"csharp",
		"obj",
		"project.assets.json"
	);

	private static readonly Regex StableSemverPattern = new(
		@"^(?<maj>\d+)\.(?<min>\d+)\.(?<patch>\d+)$",
		RegexOptions.Compiled
	);

	[Test]
	public async Task Csproj_References_SearchPioneer_Lingua()
	{
		var xml = await File.ReadAllTextAsync(CsprojPath);
		var doc = System.Xml.Linq.XDocument.Parse(xml);
		var ns = doc.Root!.GetDefaultNamespace();

		var linguaRef = doc.Root!.Elements(ns + "ItemGroup")
			.SelectMany(g => g.Elements(ns + "PackageReference"))
			.FirstOrDefault(e => e.Attribute("Include")?.Value == "SearchPioneer.Lingua");

		await Assert.That(linguaRef).IsNotNull();
	}

	[Test]
	public async Task Csproj_Lingua_Version_Is_Floating()
	{
		var xml = await File.ReadAllTextAsync(CsprojPath);
		var doc = System.Xml.Linq.XDocument.Parse(xml);
		var ns = doc.Root!.GetDefaultNamespace();

		var linguaRef = doc.Root!.Elements(ns + "ItemGroup")
			.SelectMany(g => g.Elements(ns + "PackageReference"))
			.FirstOrDefault(e => e.Attribute("Include")?.Value == "SearchPioneer.Lingua");

		await Assert.That(linguaRef).IsNotNull();

		var version = linguaRef!.Attribute("Version")?.Value;
		await Assert.That(version).IsEqualTo("*");
	}

	[Test]
	public async Task Csproj_Does_Not_Contain_EF_Related_Version_Pin_Regression()
	{
		var xml = await File.ReadAllTextAsync(CsprojPath);
		var doc = System.Xml.Linq.XDocument.Parse(xml);
		var ns = doc.Root!.GetDefaultNamespace();

		var efPackagePrefixes = new[]
		{
			"Microsoft.EntityFrameworkCore",
			"Microsoft.EntityFrameworkCore.Design",
			"Microsoft.EntityFrameworkCore.Tools",
			"Microsoft.EntityFrameworkCore.InMemory",
			"Microsoft.EntityFrameworkCore.Relational",
			"Npgsql.EntityFrameworkCore.PostgreSQL",
		};

		foreach (var prefix in efPackagePrefixes)
		{
			var refs = doc.Root!.Elements(ns + "ItemGroup")
				.SelectMany(g => g.Elements(ns + "PackageReference"))
				.Where(e =>
				{
					var inc = e.Attribute("Include")?.Value;
					return inc is not null && inc.StartsWith(prefix, StringComparison.Ordinal);
				})
				.ToList();

			foreach (var r in refs)
			{
				var v = r.Attribute("Version")?.Value;
				await Assert.That(v).IsEqualTo("*");
			}
		}
	}

	[Test]
	public async Task Resolved_Lingua_Version_Is_Stable_Semver_From_Offline_Lock_File()
	{
		await Assert.That(File.Exists(LockFilePath)).IsTrue();

		var assets = await File.ReadAllTextAsync(LockFilePath);
		using var doc = JsonDocument.Parse(assets);

		var resolvedVersion = ResolvePackageVersion(doc, "SearchPioneer.Lingua");
		await Assert.That(resolvedVersion).IsNotNull();

		var semver = TryParseStableSemver(resolvedVersion!);
		await Assert.That(semver).IsNotNull();
	}

	[Test]
	public async Task SemverAwareCompare_Orders_TwoDigitMinor_Above_SingleDigitMinor()
	{
		await Assert.That(CompareSemver("1.10.0", "1.9.0")).IsGreaterThan(0);
		await Assert.That(CompareSemver("1.9.0", "1.10.0")).IsLessThan(0);
		await Assert.That(CompareSemver("1.0.0", "1.0.0")).IsEqualTo(0);
		await Assert.That(CompareSemver("2.0.0", "1.99.99")).IsGreaterThan(0);
	}

	private static int CompareSemver(string left, string right)
	{
		if (TryParseStableSemver(left) is not { } l)
			throw new ArgumentException($"not a stable semver: {left}", nameof(left));
		if (TryParseStableSemver(right) is not { } r)
			throw new ArgumentException($"not a stable semver: {right}", nameof(right));

		var byMajor = l.Major.CompareTo(r.Major);
		if (byMajor != 0)
			return byMajor;
		var byMinor = l.Minor.CompareTo(r.Minor);
		if (byMinor != 0)
			return byMinor;
		return l.Patch.CompareTo(r.Patch);
	}

	private static (int Major, int Minor, int Patch)? TryParseStableSemver(string? value)
	{
		if (value is null)
			return null;
		var m = StableSemverPattern.Match(value);
		if (!m.Success)
			return null;
		return (
			int.Parse(m.Groups["maj"].Value, System.Globalization.CultureInfo.InvariantCulture),
			int.Parse(m.Groups["min"].Value, System.Globalization.CultureInfo.InvariantCulture),
			int.Parse(m.Groups["patch"].Value, System.Globalization.CultureInfo.InvariantCulture)
		);
	}

	private static string? ResolvePackageVersion(JsonDocument doc, string packageId)
	{
		if (!doc.RootElement.TryGetProperty("targets", out var targets))
			return null;

		foreach (var tfm in targets.EnumerateObject())
		{
			foreach (var prop in tfm.Value.EnumerateObject())
			{
				if (prop.Name.StartsWith($"{packageId}/", StringComparison.Ordinal))
					return prop.Name.Substring(packageId.Length + 1);
			}
		}

		return null;
	}
}
