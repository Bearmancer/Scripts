using System.Xml.Linq;
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
		var doc = XDocument.Parse(xml);
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
		var doc = XDocument.Parse(xml);
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
		// Validates that floating `*` resolves to a real semver (not "*-*" prerelease
		// nor a missing package) at restore time.
		var packageId = "SearchPioneer.Lingua";
		var lockFile = TestPaths.Combine("csharp", "obj", "project.assets.json");
		File.Exists(lockFile).Should().BeTrue(
			"dotnet restore must have produced project.assets.json"
		);

		var assets = await File.ReadAllTextAsync(lockFile);
		assets.Should().Contain($"\"{packageId.ToLowerInvariant()}\"",
			$"because {packageId} must be in the resolved package graph");
	}
}
