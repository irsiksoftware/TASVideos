using System.Text.Json;
using TASVideos.Data.Entity.Game;

namespace TASVideos.Data.Services;

public interface IGamesConfigService
{
	Task<IEnumerable<GameDto>> GetAllGamesAsync();
	Task<GameDto?> GetGameByIdAsync(int id);
}

public class GamesConfigService : IGamesConfigService
{
	private readonly string _configPath;
	private GamesConfig? _cache;

	public GamesConfigService()
	{
		_configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configuration", "GamesConfig.json");
	}

	private async Task<GamesConfig> LoadConfigAsync()
	{
		if (_cache != null)
		{
			return _cache;
		}

		if (!File.Exists(_configPath))
		{
			_cache = new GamesConfig();
			return _cache;
		}

		var json = await File.ReadAllTextAsync(_configPath);
		_cache = JsonSerializer.Deserialize<GamesConfig>(json) ?? new GamesConfig();
		return _cache;
	}

	public async Task<IEnumerable<GameDto>> GetAllGamesAsync()
	{
		var config = await LoadConfigAsync();
		return config.Games;
	}

	public async Task<GameDto?> GetGameByIdAsync(int id)
	{
		var config = await LoadConfigAsync();
		return config.Games.FirstOrDefault(g => g.Id == id);
	}
}

public class GamesConfig
{
	public List<GameDto> Games { get; set; } = [];
	public List<GameGenreDto> GameGenres { get; set; } = [];
	public List<GameGameGroupDto> GameGameGroups { get; set; } = [];
	public List<GameGoalDto> GameGoals { get; set; } = [];
}

public class GameDto
{
	public int Id { get; set; }
	public string DisplayName { get; set; } = "";
	public string? Abbreviation { get; set; }
	public string? Aliases { get; set; }
	public string? ScreenshotUrl { get; set; }
	public string? GameResourcesPage { get; set; }
}

public class GameGenreDto
{
	public int GameId { get; set; }
	public int GenreId { get; set; }
}

public class GameGameGroupDto
{
	public int GameId { get; set; }
	public int GameGroupId { get; set; }
}

public class GameGoalDto
{
	public int Id { get; set; }
	public int GameId { get; set; }
	public string DisplayName { get; set; } = "";
}
