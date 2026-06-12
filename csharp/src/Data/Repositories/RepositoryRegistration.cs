using Microsoft.Extensions.DependencyInjection;

namespace Scripts.Data.Repositories;

internal static class RepositoryRegistration
{
	public static IServiceCollection AddRepositories(this IServiceCollection services)
	{
		var resiliencePipeline = RepositoryResilienceFactory.CreateDatabasePipeline();

		services.AddSingleton(resiliencePipeline);
		services.AddScoped<ScrobbleRepository>();
		services.AddScoped<VideoRepository>();
		services.AddScoped<TrackRepository>();
		services.AddScoped<ArtistRepository>();
		services.AddScoped<AlbumRepository>();

		return services;
	}
}
