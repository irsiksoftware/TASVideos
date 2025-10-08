using TASVideos.Data.Entity.Game;
using TASVideos.Data.Services;

namespace TASVideos.Pages.Games.Goals;

[AllowAnonymous]
public class ListModel(ApplicationDbContext db, IGamesConfigService gamesConfig) : BasePageModel
{
	public string Game { get; set; } = "";

	[FromRoute]
	public int GameId { get; set; }

	[FromQuery]
	public int? GoalToEdit { get; set; }

	public List<GoalEntry> Goals { get; set; } = [];

	public async Task<IActionResult> OnGet()
	{
		var gameDto = await gamesConfig.GetGameByIdAsync(GameId);
		if (gameDto is null)
		{
			return NotFound();
		}

		Game = gameDto.DisplayName;

		// GameGoals are now read-only from configuration, cannot query with navigation properties
		Goals = [];

		return Page();
	}

	public Task<IActionResult> OnPost(string? goalToCreate)
	{
		// GameGoals are now read-only from configuration, cannot create
		ErrorStatusMessage("Game goals are read-only and cannot be modified");
		return Task.FromResult<IActionResult>(BackToList());
	}

	public Task<IActionResult> OnPostEdit(int gameGoalId, string? newGoalName)
	{
		// GameGoals are now read-only from configuration, cannot edit
		ErrorStatusMessage("Game goals are read-only and cannot be modified");
		return Task.FromResult<IActionResult>(BackToList());
	}

	public Task<IActionResult> OnGetDelete(int gameGoalId)
	{
		// GameGoals are now read-only from configuration, cannot delete
		ErrorStatusMessage("Game goals are read-only and cannot be deleted");
		return Task.FromResult<IActionResult>(BackToList());
	}

	private IActionResult BackToList() => BasePageRedirect("List", new { GameId });

	public record GoalEntry(int Id, string Name, List<PublicationEntry> Publications, List<SubmissionEntry> Submissions);

	public record PublicationEntry(int Id, string Title, bool Obs);

	public record SubmissionEntry(int Id, string Title);
}
