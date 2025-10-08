using TASVideos.Data.Entity.Game;
using TASVideos.Data.Services;
using TASVideos.WikiEngine;

namespace TASVideos.WikiModules;

[WikiModule(ModuleNames.MoviesGameList)]
public class MoviesGameList(ApplicationDbContext db, IGamesConfigService gamesConfig) : WikiViewComponent
{
	public int? SystemId { get; set; }
	public string? SystemCode { get; set; }
	public List<GameEntry> Games { get; set; } = [];

	public async Task<IViewComponentResult> InvokeAsync(int? system)
	{
		SystemId = system;
		var systemObj = await db.GameSystems.SingleOrDefaultAsync(s => s.Id == system);
		if (SystemId is null || systemObj is null)
		{
			return View();
		}

		SystemCode = systemObj.Code;

		// Games are now read-only from configuration, cannot query with navigation properties
		var allGames = await gamesConfig.GetAllGamesAsync();
		Games = allGames
			.Select(g => new GameEntry(
				g.Id,
				g.DisplayName,
				[]))
			.ToList();

		return View();
	}

	public record GameEntry(int Id, string Name, List<int> PublicationIds);
}
