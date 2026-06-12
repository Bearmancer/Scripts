namespace Scripts.Tests.SyncService;

internal sealed class LastFmServiceDeleteTests
{
	[Test]
	public async Task LegacyLastFmService_FileDoesNotExist()
	{
		var path = Path.Combine(
			TestPaths.SrcRoot,
			"Services",
			"Sync",
			"LastFm",
			"LastFmService.cs"
		);
		await Assert.That(System.IO.File.Exists(path)).IsFalse();
	}

	[Test]
	public async Task CanonicalLastFmService_FileExists()
	{
		var path = Path.Combine(TestPaths.SrcRoot, "Services", "Sync", "LastFmService.cs");
		await Assert.That(System.IO.File.Exists(path)).IsTrue();
	}

	[Test]
	public async Task LegacyNamespace_DoesNotContainInlineScrobbleDefinition()
	{
		var inlineType = Type.GetType("Scripts.Services.Sync.LastFm.Scrobble, Scripts");
		await Assert.That(inlineType).IsNull();
	}
}
