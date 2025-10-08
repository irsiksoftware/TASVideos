using TASVideos.Data.Services;
using TASVideos.WikiEngine;

namespace TASVideos.WikiModules;

[WikiModule(ModuleNames.DisplayGameName)]
public class DisplayGameName(ApplicationDbContext db, IGamesConfigService gamesConfig) : WikiViewComponent
{
	public async Task<IViewComponentResult> InvokeAsync(IList<int> gid)
	{
		if (!gid.Any())
		{
			return Error("No game ID specified");
		}

		var allGames = await gamesConfig.GetAllGamesAsync();
		var games = allGames
			.Where(g => gid.Contains(g.Id))
			.OrderBy(g => g.DisplayName)
			.ToList();

		var displayNames = games
			.Select(g => $"{g.DisplayName}");

		return String(string.Join(", ", displayNames));
	}
}
