using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Scripts.Data;
using Scripts.Tests.DbContext;
using TUnit.Core;

namespace Scripts.Tests;

/// <summary>
/// Base class for integration tests that need a real PostgreSQL database.
/// One <see cref="PostgresFixture"/> instance is created per derived test class
/// (keyed by <see cref="Type"/>) to avoid race conditions on the compiled model
/// and to share the migration cost across tests in the same class.
/// 
/// Tests within a class run serially via a per-class semaphore so the
/// shared database can be safely truncated between tests.
/// 
/// Requires <c>PGCONNSTR</c> in the environment (or a reachable Testcontainers Docker daemon).
/// Add <c>[RequiresPgConnStr]</c> on the class to skip cleanly when neither is available.
/// </summary>
internal abstract class DatabaseTestBase
{
	private static readonly ConcurrentDictionary<Type, PostgresFixture> Fixtures = new();
	private static readonly ConcurrentDictionary<Type, SemaphoreSlim> ClassLocks = new();

	// Global lock that serializes every test that derives from this base. The shared
	// compiled model used by ScriptsDbContext has lazy state that races under
	// concurrent first-access, so we serialize at the test level. The per-class
	// locks remain so a single class can still be run in isolation safely.
	private static readonly SemaphoreSlim GlobalLock = new(1, 1);

#pragma warning disable TUnit0023
	protected PostgresFixture Fixture =>
		Fixtures.GetOrAdd(GetType(), static type => new PostgresFixture());
#pragma warning restore TUnit0023

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
		Console.Error.WriteLine($"[DBG] AcquireLocks for {GetType().Name}");
		await GlobalLock.WaitAsync();
		var semaphore = ClassLocks.GetOrAdd(GetType(), static _ => new SemaphoreSlim(1, 1));
		await semaphore.WaitAsync();
		Console.Error.WriteLine($"[DBG] Acquired for {GetType().Name}");
	}

	[After(HookType.Test)]
	public async Task ResetDatabase()
	{
		try
		{
			Console.Error.WriteLine($"[DBG] Reset for {GetType().Name}");
			await using var context = Fixture.GetContext();
			await context.Database.ExecuteSqlRawAsync(
				"TRUNCATE TABLE " +
				"artists, albums, tracks, scrobbles, videos, " +
				"execution_logs, failed_tasks, fibery_entities, " +
				"source_records, release_progress " +
				"RESTART IDENTITY CASCADE");
		}
		finally
		{
			if (ClassLocks.TryGetValue(GetType(), out var semaphore))
				semaphore.Release();
			GlobalLock.Release();
			Console.Error.WriteLine($"[DBG] Released for {GetType().Name}");
		}
	}
}

