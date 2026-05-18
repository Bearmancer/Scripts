using CSharpScripts.Data;

namespace CSharpScripts.Tests;

public class TextNormalizerTests
{
	[Fact]
	public void ToStorageKey_RemovesDiacritics() =>
		Assert.Equal("bjork", TextNormalizer.ToStorageKey("björk"));

	[Fact]
	public void ToStorageKey_LowercasesAndTrims() =>
		Assert.Equal("sigur ros", TextNormalizer.ToStorageKey("  SIGUR rÓs  "));
}
