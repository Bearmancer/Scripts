using CSharpScripts.Data.Entities;

namespace CSharpScripts.Data.Repositories.Interfaces;

public interface ITrackRepository
{
	Task<int> BulkInsertAsync(IEnumerable<Track> tracks, CancellationToken ct = default);

	Task<Track?> GetByArtistAndTitleAsync(
		int artistId,
		string title,
		CancellationToken ct = default
	);
}
