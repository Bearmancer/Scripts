#pragma warning disable CS8604
namespace Scripts.Core;

internal sealed class SpectreTypeRegistrar(IServiceProvider serviceProvider) : ITypeRegistrar
{
	public ITypeResolver Build() => new SpectreTypeResolver(serviceProvider: serviceProvider);

	public void Register(Type service, Type implementation) { }

	public void RegisterInstance(Type service, object implementation) { }

	public void RegisterLazy(Type service, Func<object> factory) { }

	private sealed class SpectreTypeResolver(IServiceProvider serviceProvider) : ITypeResolver
	{
		public object? Resolve(Type? type) => serviceProvider.GetService(serviceType: type);
	}
}
