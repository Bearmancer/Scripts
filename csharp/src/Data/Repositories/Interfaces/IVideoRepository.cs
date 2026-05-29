using CSharpScripts.Data.Entities;

namespace CSharpScripts.Data.Repositories.Interfaces;

public interface IVideoRepository
{
	Task<Video> AddAsync(Video video, CancellationToken ct = default);

	Task<Video?> GetByUrlAsync(string url, CancellationToken ct = default);

	Task<IReadOnlyList<Video>> GetByChannelAsync(
		string channelName,
		CancellationToken ct = default
	);
}
