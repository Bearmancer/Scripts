using Scripts.Data.Entities;

namespace Scripts.Data.Repositories.Interfaces;

public interface IAlbumRepository
{
	Task<Album?> GetByArtistAndTitleAsync(
		int artistId,
		string title,
		CancellationToken ct = default
	);

	Task<Album> AddAsync(Album album, CancellationToken ct = default);

	Task<Album?> GetCachedByArtistAndTitleAsync(
		int artistId,
		string title,
		CancellationToken ct = default
	);

	Task<int> UpsertCacheAsync(Album album, CancellationToken ct = default);
}
