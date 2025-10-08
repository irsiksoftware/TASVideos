using System.Text.Json;
using Microsoft.Extensions.Options;
using TASVideos.Data.Configuration;

namespace TASVideos.Data.Services;

public interface IGamesJsonStorageService
{
	Task<IEnumerable<GameStorageDto>> GetAllGamesAsync();
	Task<GameStorageDto?> GetGameByIdAsync(int id);
	Task SaveGameAsync(GameStorageDto game);
	Task DeleteGameAsync(int id);
}

public class GamesJsonStorageService : IGamesJsonStorageService
{
	private readonly string _storagePath;
	private readonly SemaphoreSlim _lock = new(1, 1);

	public GamesJsonStorageService(IOptions<GamesJsonStorageConfig> config)
	{
		_storagePath = config.Value.StoragePath;
		EnsureDirectoryExists();
	}

	private void EnsureDirectoryExists()
	{
		var directory = Path.GetDirectoryName(_storagePath);
		if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
		{
			Directory.CreateDirectory(directory);
		}
	}

	public async Task<IEnumerable<GameStorageDto>> GetAllGamesAsync()
	{
		await _lock.WaitAsync();
		try
		{
			if (!File.Exists(_storagePath))
			{
				return [];
			}

			var json = await File.ReadAllTextAsync(_storagePath);
			return JsonSerializer.Deserialize<List<GameStorageDto>>(json) ?? [];
		}
		finally
		{
			_lock.Release();
		}
	}

	public async Task<GameStorageDto?> GetGameByIdAsync(int id)
	{
		var games = await GetAllGamesAsync();
		return games.FirstOrDefault(g => g.Id == id);
	}

	public async Task SaveGameAsync(GameStorageDto game)
	{
		await _lock.WaitAsync();
		try
		{
			var games = (await GetAllGamesAsync()).ToList();
			var existing = games.FirstOrDefault(g => g.Id == game.Id);

			if (existing != null)
			{
				games.Remove(existing);
			}

			games.Add(game);

			var json = JsonSerializer.Serialize(games, new JsonSerializerOptions { WriteIndented = true });
			await File.WriteAllTextAsync(_storagePath, json);
		}
		finally
		{
			_lock.Release();
		}
	}

	public async Task DeleteGameAsync(int id)
	{
		await _lock.WaitAsync();
		try
		{
			var games = (await GetAllGamesAsync()).ToList();
			var game = games.FirstOrDefault(g => g.Id == id);

			if (game != null)
			{
				games.Remove(game);
				var json = JsonSerializer.Serialize(games, new JsonSerializerOptions { WriteIndented = true });
				await File.WriteAllTextAsync(_storagePath, json);
			}
		}
		finally
		{
			_lock.Release();
		}
	}
}

public class GameStorageDto
{
	public int Id { get; set; }
	public string DisplayName { get; set; } = "";
	public string? Abbreviation { get; set; }
	public string? Aliases { get; set; }
	public string? ScreenshotUrl { get; set; }
	public string? GameResourcesPage { get; set; }
	public List<int> GenreIds { get; set; } = [];
	public List<int> GameGroupIds { get; set; } = [];
	public List<GameGoalStorageDto> Goals { get; set; } = [];
	public List<GameVersionStorageDto> Versions { get; set; } = [];
}

public class GameGoalStorageDto
{
	public int Id { get; set; }
	public int GameId { get; set; }
	public string DisplayName { get; set; } = "";
}

public class GameVersionStorageDto
{
	public int Id { get; set; }
	public int GameId { get; set; }
	public int SystemId { get; set; }
	public string Name { get; set; } = "";
	public string? Region { get; set; }
	public string? Version { get; set; }
	public string? Sha1 { get; set; }
	public string? Md5 { get; set; }
	public string? TitleOverride { get; set; }
}
