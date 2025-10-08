using System.Globalization;
using TASVideos.Data.Services;

namespace TASVideos.Pages.Publications;

[RequirePermission(PermissionTo.CatalogMovies)]
public class CatalogModel(ApplicationDbContext db, IExternalMediaPublisher publisher, IGamesConfigService gamesConfig) : BasePageModel
{
	[FromRoute]
	public int Id { get; set; }

	[FromQuery]
	public int? SystemId { get; set; }

	[FromQuery]
	public int? GameId { get; set; }

	[FromQuery]
	public int? GameVersionId { get; set; }

	[FromQuery]
	public int? GameGoalId { get; set; }

	[BindProperty]
	public PublicationCatalog Catalog { get; set; } = new();

	public List<SelectListItem> AvailableVersions { get; set; } = [];
	public List<SelectListItem> AvailableGames { get; set; } = [];
	public List<SelectListItem> AvailableSystems { get; set; } = [];
	public List<SelectListItem> AvailableSystemFrameRates { get; set; } = [];
	public List<SelectListItem> AvailableGoals { get; set; } = [];

	public async Task<IActionResult> OnGet()
	{
		var catalog = await db.Publications
				.Where(p => p.Id == Id)
				.Select(p => new PublicationCatalog
				{
					Title = p.Title,
					GameVersion = p.GameVersionId,
					Game = p.GameId,
					System = p.SystemId,
					SystemFramerate = p.SystemFrameRateId,
					Goal = p.GameGoalId!.Value,
					Emulator = p.EmulatorVersion
				})
				.SingleOrDefaultAsync();

		if (catalog is null)
		{
			return NotFound();
		}

		Catalog = catalog;

		if (SystemId.HasValue)
		{
			var system = await db.GameSystems.FindAsync(SystemId);
			if (system is not null)
			{
				Catalog.System = system.Id;
			}
		}

		if (GameId.HasValue)
		{
			var game = await gamesConfig.GetGameByIdAsync(GameId.Value);
			if (game is not null)
			{
				Catalog.Game = game.Id;

				// We only want to pre-populate the Game Version if a valid Game was provided
				if (GameVersionId.HasValue)
				{
					var gameVersion = await db.GameVersions.SingleOrDefaultAsync(r => r.GameId == game.Id && r.Id == GameVersionId && r.SystemId == Catalog.System);
					if (gameVersion is not null)
					{
						Catalog.GameVersion = gameVersion.Id;
					}
				}

				// GameGoals are now read-only from configuration
				if (GameGoalId.HasValue)
				{
					Catalog.Goal = GameGoalId.Value;
				}
			}
		}

		await PopulateCatalogDropDowns(Catalog.Game, Catalog.System);
		return Page();
	}

	public async Task<IActionResult> OnPost()
	{
		if (!ModelState.IsValid)
		{
			await PopulateCatalogDropDowns(Catalog.Game, Catalog.System);
			return Page();
		}

		var publication = await db.Publications
			.Include(p => p.Authors)
			.ThenInclude(pa => pa.Author)
			.Include(p => p.System)
			.Include(p => p.SystemFrameRate)
			.Include(p => p.Game)
			.Include(p => p.GameVersion)
			.Include(p => p.GameGoal)
			.SingleOrDefaultAsync(s => s.Id == Id);
		if (publication is null)
		{
			return NotFound();
		}

		var externalMessages = new List<string>();

		if (publication.SystemId != Catalog.System)
		{
			var system = await db.GameSystems.FindAsync(Catalog.System);
			if (system is null)
			{
				ModelState.AddModelError($"{nameof(Catalog)}.{nameof(Catalog.System)}", $"Unknown System Id: {Catalog.System}");
			}
			else
			{
				externalMessages.Add($"System changed from {publication.System!.Code} to {system.Code}");
				publication.SystemId = Catalog.System;
				publication.System = system;
			}
		}

		if (publication.SystemFrameRateId != Catalog.SystemFramerate)
		{
			var systemFramerate = await db.GameSystemFrameRates.FindAsync(Catalog.SystemFramerate);
			if (systemFramerate is null)
			{
				ModelState.AddModelError($"{nameof(Catalog)}.{nameof(Catalog.SystemFramerate)}", $"Unknown System Id: {Catalog.SystemFramerate}");
			}
			else if (publication.SystemFrameRateId != Catalog.SystemFramerate)
			{
				externalMessages.Add($"Framerate changed from {publication.SystemFrameRate!.FrameRate.ToString(CultureInfo.InvariantCulture)} to {systemFramerate.FrameRate.ToString(CultureInfo.InvariantCulture)}");
				publication.SystemFrameRateId = Catalog.SystemFramerate;
				publication.SystemFrameRate = systemFramerate;
			}
		}

		if (publication.GameId != Catalog.Game)
		{
			var game = await gamesConfig.GetGameByIdAsync(Catalog.Game);
			if (game is null)
			{
				ModelState.AddModelError($"{nameof(Catalog)}.{nameof(Catalog.Game)}", $"Unknown Game Id: {Catalog.Game}");
			}
			else
			{
				externalMessages.Add($"Game changed from \"{publication.Game!.DisplayName}\" to \"{game.DisplayName}\"");
				publication.GameId = Catalog.Game;
				// Note: Cannot set navigation property from DTO
			}
		}

		if (publication.GameGoalId != Catalog.Goal)
		{
			// GameGoals are now read-only from configuration, cannot validate
			externalMessages.Add($"Game Goal changed from \"{publication.GameGoal!.DisplayName}\" to goal ID {Catalog.Goal}");
			publication.GameGoalId = Catalog.Goal;
		}

		if (publication.GameVersionId != Catalog.GameVersion)
		{
			var gameVersion = await db.GameVersions.FindAsync(Catalog.GameVersion);
			if (gameVersion is null)
			{
				ModelState.AddModelError($"{nameof(Catalog)}.{nameof(Catalog.GameVersion)}", $"Unknown System Id: {Catalog.GameVersion}");
			}
			else
			{
				externalMessages.Add($"Game Version changed from \"{publication.GameVersion!.Name}\" to \"{gameVersion.Name}\"");
				publication.GameVersionId = Catalog.GameVersion;
				publication.GameVersion = gameVersion;
			}

			if (!ModelState.IsValid)
			{
				await PopulateCatalogDropDowns(Catalog.Game, Catalog.System);
				return Page();
			}
		}

		publication.EmulatorVersion = Catalog.Emulator;
		publication.Title = publication.GenerateTitle();

		var result = await db.TrySaveChanges();
		SetMessage(result, $"{Id}M catalog updated", $"Unable to save {Id}M catalog");
		if (result.IsSuccess())
		{
			await publisher.SendGameManagement(
				$"[{Id}M]({{0}}) Catalog edited by {User.Name()}",
				$"{string.Join(", ", externalMessages)} | {publication.Title}",
				$"{Id}M");
		}

		return RedirectToPage("View", new { Id });
	}

	private async Task PopulateCatalogDropDowns(int gameId, int systemId)
	{
		AvailableVersions = (await db.GameVersions.ToDropDownList(systemId, gameId)).WithDefaultEntry();
		// Games are now read-only from configuration
		AvailableGames = [];
		AvailableSystems = await db.GameSystems.ToDropDownListWithId();
		AvailableSystemFrameRates = await db.GameSystemFrameRates.ToDropDownList(systemId);
		// GameGoals are now read-only from configuration
		AvailableGoals = [];
	}

	public class PublicationCatalog
	{
		public string Title { get; init; } = "";
		public int GameVersion { get; set; }
		public int Goal { get; set; }
		public int Game { get; set; }
		public int System { get; set; }
		public int SystemFramerate { get; init; }

		[StringLength(50)]
		[Required]
		public string? Emulator { get; init; }
	}
}
