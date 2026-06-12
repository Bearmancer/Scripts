namespace Scripts.Services.Music;

internal sealed class PurgeService(IDbContextFactory<ScriptsDbContext> contextFactory)
{
	private readonly IDbContextFactory<ScriptsDbContext> _contextFactory = contextFactory;

	public async Task<PurgeResult> PurgeOrphansAsync(CancellationToken ct = default)
	{
		await using var db = await _contextFactory.CreateDbContextAsync(ct);
		await using var transaction = await db.Database.BeginTransactionAsync(ct);

		try
		{
			int tracksPurged = 0;
			int albumsPurged = 0;
			int artistsPurged = 0;

			// L1: Purge Tracks with no scrobbles
			var orphanTracks = await db
				.Tracks.Where(t => !db.Scrobbles.Any(s => s.TrackId == t.Id))
				.ToListAsync(ct);

			if (orphanTracks.Count > 0)
			{
				db.Tracks.RemoveRange(orphanTracks);
				await db.SaveChangesAsync(ct);
				tracksPurged = orphanTracks.Count;
			}

			// L2: Purge Albums with no tracks
			var orphanAlbums = await db
				.Albums.Where(a => !db.Tracks.Any(t => t.AlbumId == a.Id))
				.ToListAsync(ct);

			if (orphanAlbums.Count > 0)
			{
				db.Albums.RemoveRange(orphanAlbums);
				await db.SaveChangesAsync(ct);
				albumsPurged = orphanAlbums.Count;
			}

			// L3: Purge Artists with no albums and no tracks
			var orphanArtists = await db
				.Artists.Where(a =>
					!db.Albums.Any(al => al.ArtistId == a.Id)
					&& !db.Tracks.Any(t => t.ArtistId == a.Id)
				)
				.ToListAsync(ct);

			if (orphanArtists.Count > 0)
			{
				db.Artists.RemoveRange(orphanArtists);
				await db.SaveChangesAsync(ct);
				artistsPurged = orphanArtists.Count;
			}

			await transaction.CommitAsync(ct);

			return new PurgeResult(tracksPurged, albumsPurged, artistsPurged);
		}
		catch
		{
			await transaction.RollbackAsync(ct);
			throw;
		}
	}

	public record PurgeResult(int TracksPurged, int AlbumsPurged, int ArtistsPurged);
}
