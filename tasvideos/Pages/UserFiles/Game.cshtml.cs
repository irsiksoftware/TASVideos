using TASVideos.Data.Services;

namespace TASVideos.Pages.UserFiles;

[AllowAnonymous]
public class GameModel(ApplicationDbContext db, IGamesConfigService gamesConfig) : BasePageModel
{
	[FromRoute]
	public int Id { get; set; }

	public string GameName { get; set; } = "";
	public List<InfoModel.UserFileModel> Files { get; set; } = [];

	public async Task<IActionResult> OnGet()
	{
		var game = await gamesConfig.GetGameByIdAsync(Id);
		if (game is null)
		{
			return NotFound();
		}

		GameName = game.DisplayName;
		Files = await db.UserFiles
			.ForGame(game.Id)
			.HideIfNotAuthor(User.GetUserId())
			.AsQueryable()
			.OrderByDescending(uf => uf.UploadTimestamp)
			.ToUserFileModel()
			.ToListAsync();

		return Page();
	}
}
