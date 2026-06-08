using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Scripts.Data;
using Scripts.Tests.DbContext;
using TUnit.Core;

namespace Scripts.Tests;













internal abstract class DatabaseTestBase
{
	private static readonly ConcurrentDictionary<Type, PostgresFixture> Fixtures = new();
	private static readonly ConcurrentDictionary<Type, SemaphoreSlim> ClassLocks = new();

	
	
	
	
	private static readonly SemaphoreSlim GlobalLock = new(1, 1);

	protected PostgresFixture Fixture =>
		Fixtures.GetOrAdd(GetType(), static type => new PostgresFixture());

	[Before(HookType.Class)]
	public static async Task SetupFixture(ClassHookContext context)
	{
		ClassLocks.GetOrAdd(context.ClassType, static _ => new SemaphoreSlim(1, 1));
		var fixture = Fixtures.GetOrAdd(context.ClassType, static _ => new PostgresFixture());
		await GlobalLock.WaitAsync();
		try
		{
			await fixture.InitializeAsync();
		}
		finally
		{
			GlobalLock.Release();
		}
	}

	[After(HookType.Class)]
	public static async Task TeardownFixture(ClassHookContext context)
	{
		if (Fixtures.TryRemove(context.ClassType, out var fixture))
			await ((IAsyncDisposable)fixture).DisposeAsync();
		if (ClassLocks.TryRemove(context.ClassType, out var semaphore))
			semaphore.Dispose();
	}

	[Before(HookType.Test)]
	public async Task AcquireLocks()
	{
		await GlobalLock.WaitAsync();
		var semaphore = ClassLocks.GetOrAdd(GetType(), static _ => new SemaphoreSlim(1, 1));
		await semaphore.WaitAsync();
	}

	[After(HookType.Test)]
	public async Task ResetDatabase()
	{
		try
		{
			await using var context = Fixture.GetContext();
			await context.Database.ExecuteSqlRawAsync(
				"TRUNCATE TABLE " +
				"music.artists, music.albums, music.tracks, music.scrobbles, " +
				"youtube.videos, " +
				"public.execution_logs, public.failed_tasks, " +
				"fibery.fibery_entities, " +
				"public.source_records, music.release_progress " +
				"RESTART IDENTITY CASCADE");
		}
		finally
		{
			if (ClassLocks.TryGetValue(GetType(), out var semaphore))
				semaphore.Release();
			GlobalLock.Release();
		}
	}
}

