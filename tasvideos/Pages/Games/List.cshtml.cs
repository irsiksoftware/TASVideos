using TASVideos.Data.Entity.Game;
using TASVideos.Data.Services;

namespace TASVideos.Pages.Games;

[AllowAnonymous]
public class ListModel(ApplicationDbContext db, IGamesConfigService gamesConfig) : BasePageModel
{
	[FromQuery]
	[StringLength(50, MinimumLength = 3)]
	public string? SearchTerms { get; set; }

	[FromQuery]
	public GameListRequest Search { get; set; } = new();

	public PageOf<GameEntry, GameListRequest> Games { get; set; } = new([], new());

	public List<SelectListItem> SystemList { get; set; } = [];

	public List<SelectListItem> LetterList { get; set; } = [];

	public List<SelectListItem> GenreList { get; set; } = [];

	public List<SelectListItem> GroupList { get; set; } = [];

	public async Task OnGet()
	{
		if (ModelState.IsValid)
		{
			Games = await GetPageOfGames(Search);
		}

		SystemList = (await db.GameSystems
			.ToDropDownList())
			.WithAnyEntry();

		var allGames = await gamesConfig.GetAllGamesAsync();
		LetterList = allGames
			.Select(g => g.DisplayName.Length > 0 ? g.DisplayName.Substring(0, 1) : "")
			.Distinct()
			.OrderBy(s => s)
			.Select(s => new SelectListItem { Text = s, Value = s })
			.ToList();
		LetterList.Insert(0, new SelectListItem { Text = "Any", Value = "" });

		GenreList = (await db.Genres
			.Select(g => g.DisplayName)
			.Distinct()
			.ToDropDownList())
			.WithAnyEntry();

		GroupList = (await db.GameGroups
			.Select(g => g.Name)
			.Distinct()
			.ToDropDownList())
			.WithAnyEntry();
	}

	public async Task<IActionResult> OnGetFrameRateDropDownForSystem(int systemId, bool includeEmpty)
	{
		var items = await db.GameSystemFrameRates.ToDropDownList(systemId);
		return ToDropdownResult(items, includeEmpty);
	}

	public Task<IActionResult> OnGetGameDropDownForSystem(int systemId, bool includeEmpty)
	{
		// Games are now read-only from configuration, cannot filter by system
		var items = new List<SelectListItem>();
		return Task.FromResult<IActionResult>(ToDropdownResult(items, includeEmpty));
	}

	public async Task<IActionResult> OnGetVersionDropDownForGame(int gameId, int systemId, bool includeEmpty)
	{
		var items = await db.GameVersions.ToDropDownList(systemId, gameId);
		return ToDropdownResult(items, includeEmpty);
	}

	public Task<IActionResult> OnGetGameGoalDropDownForGame(int gameId, bool includeEmpty)
	{
		// GameGoals are now read-only from configuration
		var items = new List<SelectListItem>();
		return Task.FromResult<IActionResult>(ToDropdownResult(items, includeEmpty));
	}

	private async Task<PageOf<GameEntry, GameListRequest>> GetPageOfGames(GameListRequest paging)
	{
		// Games are now read-only from configuration, cannot query with complex filters
		var allGames = await gamesConfig.GetAllGamesAsync();
		var filteredGames = allGames
			.Where(g => string.IsNullOrEmpty(paging.StartsWith) || g.DisplayName.StartsWith(paging.StartsWith))
			.Select(g => new GameEntry
			{
				Id = g.Id,
				Name = g.DisplayName,
				Systems = []
			})
			.ToList();

		return new PageOf<GameEntry, GameListRequest>(filteredGames, paging);
	}

	[PagingDefaults(PageSize = 50, Sort = "Name")]
	public class GameListRequest : PagingModel
	{
		public string? System { get; set; }

		public string? StartsWith { get; init; }

		public string? Genre { get; init; }

		public string? Group { get; init; }

		public string? SearchTerms { get; init; }
	}

	public class GameEntry
	{
		[Sortable]
		public int Id { get; init; }

		[Sortable]
		public string Name { get; init; } = "";
		public List<string> Systems { get; init; } = [];
	}
}
