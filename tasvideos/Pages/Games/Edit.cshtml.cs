using TASVideos.Core.Services.Wiki;
using TASVideos.Core.Settings;
using TASVideos.Data.Entity.Game;
using TASVideos.Data.Services;

namespace TASVideos.Pages.Games;

[RequirePermission(PermissionTo.CatalogMovies)]
public class EditModel(
	ApplicationDbContext db,
	IWikiPages wikiPages,
	IExternalMediaPublisher publisher,
	AppSettings settings,
	IGamesConfigService gamesConfig)
	: BasePageModel
{
	private readonly string _baseUrl = settings.BaseUrl;

	[FromRoute]
	public int? Id { get; set; }

	[BindProperty]
	public GameEdit Game { get; set; } = new();

	public bool CanDelete { get; set; }

	public List<SelectListItem> AvailableGenres { get; set; } = [];
	public List<SelectListItem> AvailableGroups { get; set; } = [];

	public async Task<IActionResult> OnGet()
	{
		if (Id.HasValue)
		{
			var gameDto = await gamesConfig.GetGameByIdAsync(Id.Value);
			if (gameDto is null)
			{
				return NotFound();
			}

			Game = new GameEdit
			{
				DisplayName = gameDto.DisplayName,
				Abbreviation = gameDto.Abbreviation,
				Aliases = gameDto.Aliases,
				ScreenshotUrl = gameDto.ScreenshotUrl,
				GameResourcesPage = gameDto.GameResourcesPage,
				Genres = [],
				Groups = []
			};
		}

		await Initialize();
		return Page();
	}

	public async Task<IActionResult> OnPost()
	{
		Game.GameResourcesPage = Game.GameResourcesPage?.Replace(_baseUrl, "").Trim('/');
		Game.Aliases = Game.Aliases?.Replace(", ", ",");

		if (!ModelState.IsValid)
		{
			await Initialize();
			return Page();
		}

		if (!string.IsNullOrEmpty(Game.GameResourcesPage))
		{
			var page = await wikiPages.Page(Game.GameResourcesPage);
			if (page is null)
			{
				ModelState.AddModelError($"{nameof(Game)}.{nameof(Game.GameResourcesPage)}", $"Page {Game.GameResourcesPage} not found");
			}
		}

		if (Game.Abbreviation is not null)
		{
			var allGames = await gamesConfig.GetAllGamesAsync();
			if (allGames.Any(g => g.Id != Id && g.Abbreviation == Game.Abbreviation))
			{
				ModelState.AddModelError($"{nameof(Game)}.{nameof(Game.Abbreviation)}", $"Abbreviation {Game.Abbreviation} already exists");
			}
		}

		if (!ModelState.IsValid)
		{
			await Initialize();
			return Page();
		}

		// Games are now read-only from configuration, cannot update
		ErrorStatusMessage("Games are read-only and cannot be modified");
		await Initialize();
		return Page();
	}

	public Task<IActionResult> OnPostDelete()
	{
		// Games are now read-only from configuration, cannot delete
		ErrorStatusMessage("Games are read-only and cannot be deleted");
		return Task.FromResult<IActionResult>(BasePageRedirect("List"));
	}

	private async Task Initialize()
	{
		AvailableGenres = await db.Genres.ToDropDownList();
		AvailableGroups = await db.GameGroups.ToDropDownList();
		CanDelete = await CanBeDeleted();
	}

	private async Task<bool> CanBeDeleted()
		=> Id > 0
		&& !await db.Submissions.AnyAsync(s => s.GameId == Id)
		&& !await db.Publications.AnyAsync(p => p.GameId == Id)
		&& !await db.UserFiles.AnyAsync(u => u.GameId == Id);

	public class GameEdit
	{
		[StringLength(100)]
		public string DisplayName { get; set; } = "";

		[StringLength(24)]
		public string? Abbreviation { get; set; }

		[StringLength(250)]
		public string? Aliases { get; set; }

		[StringLength(250)]
		public string? ScreenshotUrl { get; init; }

		[StringLength(300)]
		public string? GameResourcesPage { get; set; }
		public List<int> Genres { get; init; } = [];
		public List<int> Groups { get; init; } = [];
	}
}
