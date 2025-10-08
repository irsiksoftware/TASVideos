using TASVideos.Data.Services;
using TASVideos.WikiEngine;

namespace TASVideos.WikiModules;

[WikiModule(ModuleNames.NoGameGenre)]
public class NoGameGenres(ApplicationDbContext db, IGamesConfigService gamesConfig) : WikiViewComponent
{
	public List<Entry> Games { get; set; } = [];

	public async Task<IViewComponentResult> InvokeAsync()
	{
		// Games and GameGenres are now read-only from configuration, cannot query with navigation properties
		Games = [];

		return View();
	}

	public record Entry(int Id, string DisplayName);
}
