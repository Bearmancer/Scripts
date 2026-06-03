using TUnit.Core.Interfaces;

namespace Scripts.Tests;

/// <summary>
/// Caps the test assembly at one concurrent test. Required because the shared
/// <c>ScriptsDbContextModel.Instance</c> has lazy initialization that races under
/// concurrent first-access, surfacing as NullReferenceException deep inside EF Core
/// (<c>RuntimeProperty.GetValueComparer</c>). Forcing serial execution avoids the race.
/// <para>
/// As of the t1-17 root comparer fix, the null-unsafe <c>ValueComparer&lt;string&gt;</c>
/// in <c>OnModelCreating</c> is no longer the trigger. The race that still requires
/// <c>Limit == 1</c> is the upstream EF Core 10.0.8 TOCTOU bug in
/// <c>RuntimeProperty.GetValueComparer</c> / <c>GetKeyValueComparer</c> documented
/// in <c>research/20260602-efcore-1008-race-condition-research.md</c>. That bug is
/// not project-fixable; it requires a runtime upgrade of the EF Core packages.
/// <c>Scripts.csproj</c> pins <c>Version="*"</c> for those packages, so when the
/// upstream fix lands, removing the limiter becomes the default outcome of the
/// next <c>dotnet restore</c> with no further code change.
/// </para>
/// <para>
/// Do not raise <c>Limit</c> above 1 without first reproducing a green run with
/// the upstream-pinned EF Core that contains the fix. A <c>Limit &gt; 1</c>
/// reproducer must show 1000 consecutive clean assemblies with zero
/// <c>NullReferenceException</c> in <c>RuntimeProperty.&lt;&gt;c.&lt;GetValueComparer&gt;b__49_0</c>.
/// The original failure rate is 56/213 (see
/// <c>research/20260602-efcore-1008-race-condition-research.md:7</c>); the
/// current test count is higher because of the regression tests added by the
/// t1-17 work, but the failure ratio is unchanged.
/// </para>
/// </summary>
internal sealed class SingleThreadedParallelLimit : IParallelLimit
{
	public int Limit => 1;
}
