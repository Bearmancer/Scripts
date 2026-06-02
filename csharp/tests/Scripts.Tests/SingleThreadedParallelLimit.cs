using TUnit.Core.Interfaces;

namespace Scripts.Tests;

/// <summary>
/// Caps the test assembly at one concurrent test. Required because the shared
/// <c>ScriptsDbContextModel.Instance</c> has lazy initialization that races under
/// concurrent first-access, surfacing as NullReferenceException deep inside EF Core
/// (<c>RuntimeProperty.GetValueComparer</c>). Forcing serial execution avoids the race.
/// </summary>
internal sealed class SingleThreadedParallelLimit : IParallelLimit
{
	public int Limit => 1;
}
