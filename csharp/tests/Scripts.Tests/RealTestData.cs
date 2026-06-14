namespace Scripts.Tests;

internal static class RealTestData
{
	internal const string Beethoven9 = "Beethoven: Symphony No. 9 in D minor, Op. 125 Choral";
	internal const string KarajanGerman =
		"Herbert von Karajan dirigierte die Berliner Philharmoniker von 1955 bis 1989.";
	internal const string KarajanFrench =
		"Herbert von Karajan a dirige l'Orchestre philharmonique de Berlin de 1955 a 1989.";
	internal const string KarajanItalian =
		"Herbert von Karajan diresse i Berliner Philharmoniker dal 1955 al 1989.";
	internal const string BachBMinor =
		"Johann Sebastian Bach: Messe in h-Moll, BWV 232, Kyrie eleison";
	internal const string MozartRequiem =
		"Wolfgang Amadeus Mozart: Requiem in d-Moll, KV 626, Lacrimosa";

	internal const string AzureDocRoot = @"C:\Users\Lance\Azure Document Intelligence";

	internal static string KarajanVol01Artwork =>
		Path.Combine(
			AzureDocRoot,
			"Herbert von Karajan - Complete Recordings on Deutsche Grammophon (240 CDs) (2008)",
			"Vol.01 1938-1943 (CD 1 - 6)",
			"artwork");

	internal static string TrumpVideo5Min =>
		Path.Combine(AzureDocRoot, "Trump - We Survived 250 Years (first 5 min).mp4");

	internal static string SvetlanovAudio3Min =>
		Path.Combine(AzureDocRoot, "Svetlanov - Дирижер (min 7-10).wav");

	internal static async Task<byte[]> ReadBooklet01JpgAsync()
	{
		var path = Path.Combine(KarajanVol01Artwork, "Box 01 Booklet 01.jpg");
		return await File.ReadAllBytesAsync(path);
	}

	internal static async Task<byte[]> ReadBooklet02JpgAsync()
	{
		var path = Path.Combine(KarajanVol01Artwork, "Box 01 Booklet 02.jpg");
		return await File.ReadAllBytesAsync(path);
	}

	internal static async Task<byte[]> ReadFrontJpgAsync()
	{
		var path = Path.Combine(KarajanVol01Artwork, "Box 01 Front.jpg");
		return await File.ReadAllBytesAsync(path);
	}

	internal static async Task<byte[]> ReadSvetlanovAudio3MinAsync() =>
		await File.ReadAllBytesAsync(SvetlanovAudio3Min);
}
