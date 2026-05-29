using System.Xml.Linq;
using FluentAssertions;
using TUnit;

namespace Scripts.Tests.Language;

internal sealed class LinguaPackageReferenceTests
{
	private const string CsprojPath = @"C:\Users\Lance\Dev\Scripts\csharp\CSharpScripts.csproj";

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
	public async Task Csproj_Lingua_Version_Is_One_Dot_Zero_Dot_Five()
	{
		var xml = await File.ReadAllTextAsync(CsprojPath);
		var doc = XDocument.Parse(xml);
		var ns = doc.Root!.GetDefaultNamespace();

		var linguaRef = doc.Root!.Elements(ns + "ItemGroup")
			.SelectMany(g => g.Elements(ns + "PackageReference"))
			.FirstOrDefault(e => e.Attribute("Include")?.Value == "SearchPioneer.Lingua");

		linguaRef.Should().NotBeNull();

		var version = linguaRef!.Attribute("Version")?.Value;
		version.Should().Be("1.0.5", $"because the target version is 1.0.5. Actual: {version}");
	}
}
