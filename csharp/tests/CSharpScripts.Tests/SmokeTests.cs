using System.Threading.Tasks;

namespace CSharpScripts.Tests;

public class SmokeTests
{
	[Test]
	public async Task TUnit_IsConfiguredCorrectly()
	{
		await Assert.That(true).IsTrue();
	}
}
