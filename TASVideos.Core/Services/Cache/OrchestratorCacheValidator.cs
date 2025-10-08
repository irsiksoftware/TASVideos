namespace TASVideos.Core.Services;

public class OrchestratorCacheValidator
{
	private readonly ICacheService _cacheService;

	public OrchestratorCacheValidator(ICacheService cacheService)
	{
		_cacheService = cacheService;
	}

	public bool ValidateCache(string key)
	{
		if (string.IsNullOrWhiteSpace(key))
		{
			return false;
		}

		return _cacheService.TryGetValue<object>(key, out _);
	}

	public void InvalidateCache(string key)
	{
		if (!string.IsNullOrWhiteSpace(key))
		{
			_cacheService.Remove(key);
		}
	}
}

