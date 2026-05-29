using CSharpScripts.Data.Repositories.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Polly;

namespace CSharpScripts.Data.Repositories;

internal static class RepositoryRegistration
{
	public static IServiceCollection AddRepositories(this IServiceCollection services)
	{
		var resiliencePipeline = RepositoryResilienceFactory.CreateDatabasePipeline();

		services.AddSingleton(resiliencePipeline);
		services.AddScoped<IScrobbleRepository, ScrobbleRepository>();
		services.AddScoped<IVideoRepository, VideoRepository>();
		services.AddScoped<ITrackRepository, TrackRepository>();
		services.AddScoped<IArtistRepository, ArtistRepository>();
		services.AddScoped<IAlbumRepository, AlbumRepository>();

		return services;
	}
}
