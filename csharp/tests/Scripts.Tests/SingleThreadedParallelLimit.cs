using TUnit.Core.Interfaces;

namespace Scripts.Tests;

internal sealed class SingleThreadedParallelLimit : IParallelLimit
{
	public int Limit => 1;
}
