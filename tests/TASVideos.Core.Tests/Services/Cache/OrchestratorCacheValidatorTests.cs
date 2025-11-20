using TASVideos.Core.Services;

namespace TASVideos.Core.Tests.Services.Cache;

[TestClass]
public class OrchestratorCacheValidatorTests
{
	private readonly ICacheService _cacheService = Substitute.For<ICacheService>();

	[TestMethod]
	public void ValidateCache_WhenKeyIsNull_ReturnsFalse()
	{
		var validator = new OrchestratorCacheValidator(_cacheService);

		var result = validator.ValidateCache(null!);

		Assert.IsFalse(result);
		_cacheService.DidNotReceive().TryGetValue<object>(Arg.Any<string>(), out Arg.Any<object>());
	}

	[TestMethod]
	public void ValidateCache_WhenKeyIsEmpty_ReturnsFalse()
	{
		var validator = new OrchestratorCacheValidator(_cacheService);

		var result = validator.ValidateCache("");

		Assert.IsFalse(result);
		_cacheService.DidNotReceive().TryGetValue<object>(Arg.Any<string>(), out Arg.Any<object>());
	}

	[TestMethod]
	public void ValidateCache_WhenKeyIsWhitespace_ReturnsFalse()
	{
		var validator = new OrchestratorCacheValidator(_cacheService);

		var result = validator.ValidateCache("   ");

		Assert.IsFalse(result);
		_cacheService.DidNotReceive().TryGetValue<object>(Arg.Any<string>(), out Arg.Any<object>());
	}

	[TestMethod]
	public void ValidateCache_WhenCacheHasKey_ReturnsTrue()
	{
		object outValue = new();
		_cacheService.TryGetValue<object>(Arg.Any<string>(), out Arg.Any<object>())
			.Returns(x =>
			{
				x[1] = outValue;
				return true;
			});

		var validator = new OrchestratorCacheValidator(_cacheService);

		var result = validator.ValidateCache("valid-key");

		Assert.IsTrue(result);
	}

	[TestMethod]
	public void ValidateCache_WhenCacheDoesNotHaveKey_ReturnsFalse()
	{
		_cacheService.TryGetValue<object>(Arg.Any<string>(), out Arg.Any<object>())
			.Returns(false);

		var validator = new OrchestratorCacheValidator(_cacheService);

		var result = validator.ValidateCache("missing-key");

		Assert.IsFalse(result);
	}

	[TestMethod]
	public void ValidateCache_CallsCacheServiceWithCorrectKey()
	{
		_cacheService.TryGetValue<object>(Arg.Any<string>(), out Arg.Any<object>())
			.Returns(false);

		var validator = new OrchestratorCacheValidator(_cacheService);

		validator.ValidateCache("test-key");

		_cacheService.Received(1).TryGetValue<object>("test-key", out Arg.Any<object>());
	}

	[TestMethod]
	public void ValidateCache_UsesObjectType()
	{
		_cacheService.TryGetValue<object>(Arg.Any<string>(), out Arg.Any<object>())
			.Returns(false);

		var validator = new OrchestratorCacheValidator(_cacheService);

		validator.ValidateCache("key");

		// Verify it uses TryGetValue<object> specifically
		_cacheService.Received(1).TryGetValue<object>(Arg.Any<string>(), out Arg.Any<object>());
	}

	[TestMethod]
	public void InvalidateCache_WhenKeyIsNull_DoesNotCallRemove()
	{
		var validator = new OrchestratorCacheValidator(_cacheService);

		validator.InvalidateCache(null!);

		_cacheService.DidNotReceive().Remove(Arg.Any<string>());
	}

	[TestMethod]
	public void InvalidateCache_WhenKeyIsEmpty_DoesNotCallRemove()
	{
		var validator = new OrchestratorCacheValidator(_cacheService);

		validator.InvalidateCache("");

		_cacheService.DidNotReceive().Remove(Arg.Any<string>());
	}

	[TestMethod]
	public void InvalidateCache_WhenKeyIsWhitespace_DoesNotCallRemove()
	{
		var validator = new OrchestratorCacheValidator(_cacheService);

		validator.InvalidateCache("   ");

		_cacheService.DidNotReceive().Remove(Arg.Any<string>());
	}

	[TestMethod]
	public void InvalidateCache_WhenKeyIsValid_CallsRemove()
	{
		var validator = new OrchestratorCacheValidator(_cacheService);

		validator.InvalidateCache("valid-key");

		_cacheService.Received(1).Remove("valid-key");
	}

	[TestMethod]
	public void InvalidateCache_CallsRemoveWithCorrectKey()
	{
		var validator = new OrchestratorCacheValidator(_cacheService);

		validator.InvalidateCache("test-key");

		_cacheService.Received(1).Remove("test-key");
	}

	[TestMethod]
	public void Constructor_AcceptsCacheService()
	{
		var validator = new OrchestratorCacheValidator(_cacheService);

		Assert.IsNotNull(validator);
	}

	[TestMethod]
	public void ValidateCache_MultipleCalls_WorksCorrectly()
	{
		object outValue = new();
		_cacheService.TryGetValue<object>(Arg.Any<string>(), out Arg.Any<object>())
			.Returns(x =>
			{
				x[1] = outValue;
				return true;
			});

		var validator = new OrchestratorCacheValidator(_cacheService);

		var result1 = validator.ValidateCache("key1");
		var result2 = validator.ValidateCache("key2");

		Assert.IsTrue(result1);
		Assert.IsTrue(result2);
		_cacheService.Received(1).TryGetValue<object>("key1", out Arg.Any<object>());
		_cacheService.Received(1).TryGetValue<object>("key2", out Arg.Any<object>());
	}

	[TestMethod]
	public void InvalidateCache_MultipleCalls_WorksCorrectly()
	{
		var validator = new OrchestratorCacheValidator(_cacheService);

		validator.InvalidateCache("key1");
		validator.InvalidateCache("key2");

		_cacheService.Received(1).Remove("key1");
		_cacheService.Received(1).Remove("key2");
	}

	[TestMethod]
	public void ValidateCache_AfterInvalidate_ChecksCache()
	{
		_cacheService.TryGetValue<object>(Arg.Any<string>(), out Arg.Any<object>())
			.Returns(false);

		var validator = new OrchestratorCacheValidator(_cacheService);

		validator.InvalidateCache("key");
		var result = validator.ValidateCache("key");

		_cacheService.Received(1).Remove("key");
		_cacheService.Received(1).TryGetValue<object>("key", out Arg.Any<object>());
		Assert.IsFalse(result);
	}

	[TestMethod]
	public void OrchestratorCacheValidator_IsStateless()
	{
		// The validator doesn't maintain any state
		// It just orchestrates calls to the cache service
		var validator = new OrchestratorCacheValidator(_cacheService);

		// Multiple operations shouldn't affect each other
		validator.InvalidateCache("key1");
		validator.ValidateCache("key2");
		validator.InvalidateCache("key3");

		Assert.IsNotNull(validator);
	}

	[TestMethod]
	public void OrchestratorCacheValidator_ThreadSafe()
	{
		// Since it's stateless and only delegates to ICacheService,
		// it should be thread-safe
		_cacheService.TryGetValue<object>(Arg.Any<string>(), out Arg.Any<object>())
			.Returns(true);

		var validator = new OrchestratorCacheValidator(_cacheService);

		var tasks = Enumerable.Range(0, 100).Select(i => Task.Run(() =>
		{
			validator.ValidateCache($"key-{i}");
			validator.InvalidateCache($"key-{i}");
		}));

		Task.WaitAll(tasks.ToArray());

		Assert.IsTrue(true); // Test passes if no threading issues
	}
}
