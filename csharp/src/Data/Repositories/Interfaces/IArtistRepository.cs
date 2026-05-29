using CSharpScripts.Data.Entities;

namespace CSharpScripts.Data.Repositories.Interfaces;

public interface IArtistRepository
{
	Task<Artist?> GetByNameAsync(string name, CancellationToken ct = default);

	Task<Artist> AddAsync(Artist artist, CancellationToken ct = default);
}
