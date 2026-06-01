using Scripts.Data.Entities;

namespace Scripts.Data.Repositories.Interfaces;

public interface IScrobbleRepository
{
	Task<int> UpsertAsync(IEnumerable<Entities.Scrobble> scrobbles, CancellationToken ct = default);

	Task<int> DeleteByTrackIdAsync(int trackId, CancellationToken ct = default);

	Task<IReadOnlyList<Entities.Scrobble>> GetByTrackIdAsync(
		int trackId,
		CancellationToken ct = default
	);

	Task<IReadOnlyList<Entities.Scrobble>> GetByPlatformAsync(
		string platform,
		CancellationToken ct = default
	);
}
