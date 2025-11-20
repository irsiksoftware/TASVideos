using TASVideos.Core.Services;

namespace TASVideos.Core.Tests.Services.Cache;

[TestClass]
public class NoCacheServiceTests
{
	[TestMethod]
	public void TryGetValue_AlwaysReturnsFalse()
	{
		var cache = new NoCacheService();

		var result = cache.TryGetValue<string>("any-key", out var value);

		Assert.IsFalse(result);
		Assert.IsNull(value);
	}

	[TestMethod]
	public void TryGetValue_ReturnsDefaultValue()
	{
		var cache = new NoCacheService();

		cache.TryGetValue<int>("any-key", out var intValue);
		cache.TryGetValue<string>("any-key", out var stringValue);
		cache.TryGetValue<List<string>>("any-key", out var listValue);

		Assert.AreEqual(0, intValue);
		Assert.IsNull(stringValue);
		Assert.IsNull(listValue);
	}

	[TestMethod]
	public void Set_DoesNothing()
	{
		var cache = new NoCacheService();

		// Should complete without any action
		cache.Set("key", "value");
		cache.Set("key", 42);
		cache.Set("key", new List<string>());

		Assert.IsTrue(true); // Test passes if no exception thrown
	}

	[TestMethod]
	public void Set_WithNullValue_DoesNotThrow()
	{
		var cache = new NoCacheService();

		cache.Set<string?>("key", null);

		Assert.IsTrue(true);
	}

	[TestMethod]
	public void Set_WithCacheTime_DoesNothing()
	{
		var cache = new NoCacheService();

		cache.Set("key", "value", TimeSpan.FromMinutes(5));

		Assert.IsTrue(true);
	}

	[TestMethod]
	public void Remove_DoesNothing()
	{
		var cache = new NoCacheService();

		cache.Remove("key");

		Assert.IsTrue(true);
	}

	[TestMethod]
	public void NoCacheService_ImplementsICacheService()
	{
		var cache = new NoCacheService();

		Assert.IsInstanceOfType<ICacheService>(cache);
	}

	[TestMethod]
	public void NoCacheService_NeverCachesAnything()
	{
		var cache = new NoCacheService();

		// Set a value
		cache.Set("key", "value");

		// Try to get it back - should still return false
		var result = cache.TryGetValue<string>("key", out var value);

		Assert.IsFalse(result);
		Assert.IsNull(value);
	}

	[TestMethod]
	public void NoCacheService_IsNullObjectPattern()
	{
		// NoCacheService is an implementation of the Null Object pattern
		// It provides a do-nothing implementation of ICacheService
		// This allows code to work without a real cache without null checks

		var cache = new NoCacheService();

		// All operations are safe no-ops
		cache.Set("key", "value");
		cache.Remove("key");
		var hasValue = cache.TryGetValue<string>("key", out _);

		Assert.IsFalse(hasValue);
	}

	[TestMethod]
	public void NoCacheService_ThreadSafe()
	{
		// Since NoCacheService does nothing, it's inherently thread-safe
		var cache = new NoCacheService();

		var tasks = Enumerable.Range(0, 100).Select(i => Task.Run(() =>
		{
			cache.Set($"key-{i}", $"value-{i}");
			cache.TryGetValue<string>($"key-{i}", out _);
			cache.Remove($"key-{i}");
		}));

		// Should complete without any threading issues
		Task.WaitAll(tasks.ToArray());

		Assert.IsTrue(true);
	}

	[TestMethod]
	public void NoCacheService_MultipleInstances_Independent()
	{
		var cache1 = new NoCacheService();
		var cache2 = new NoCacheService();

		cache1.Set("key", "value1");
		cache2.Set("key", "value2");

		// Both should return false - no caching occurs
		var result1 = cache1.TryGetValue<string>("key", out _);
		var result2 = cache2.TryGetValue<string>("key", out _);

		Assert.IsFalse(result1);
		Assert.IsFalse(result2);
	}
}
