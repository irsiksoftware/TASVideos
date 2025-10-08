using TASVideos.Data.Entity.Forum;
using TASVideos.Data.Entity.Game;
using TASVideos.Data.Services;
using TASVideos.WikiModules;

namespace TASVideos.Pages.Games;

[AllowAnonymous]
public class IndexModel(ApplicationDbContext db, IGamesConfigService gamesConfig) : BasePageModel
{
	[FromRoute]
	public string Id { get; set; } = "";

	public int ParsedId => int.TryParse(Id, out var id) ? id : -2;

	public GameDisplay Game { get; set; } = new();
	public List<TabMiniMovieModel> Movies { get; set; } = [];
	public List<WatchFile> WatchFiles { get; set; } = [];
	public List<TopicEntry> Topics { get; set; } = [];
	public List<GoalEntry> PlaygroundGoals { get; set; } = [];

	public async Task<IActionResult> OnGet()
	{
		GameDto? gameDto;
		if (ParsedId > -2)
		{
			gameDto = await gamesConfig.GetGameByIdAsync(ParsedId);
		}
		else
		{
			var allGames = await gamesConfig.GetAllGamesAsync();
			gameDto = allGames.FirstOrDefault(g => g.Abbreviation == Id);
		}

		if (gameDto is null)
		{
			return NotFound();
		}

		Game = new GameDisplay
		{
			Id = gameDto.Id,
			DisplayName = gameDto.DisplayName,
			Abbreviation = gameDto.Abbreviation,
			Aliases = gameDto.Aliases,
			ScreenshotUrl = gameDto.ScreenshotUrl,
			GameResourcesPage = gameDto.GameResourcesPage,
			Genres = [],
			Versions = [],
			GameGroups = [],
			PublicationCount = 0,
			ObsoletePublicationCount = 0,
			SubmissionCount = 0,
			UserFilesCount = 0,
			PlaygroundSubmissions = []
		};
		var movies = await db.Publications
			.Where(p => p.GameId == Game.Id && p.ObsoletedById == null)
			.OrderBy(p => p.GameGoal!.DisplayName == "baseline" ? -1 : p.GameGoal!.DisplayName.Length)
			.ThenBy(p => p.Frames)
			.Select(p => new
			{
				p.Id,
				p.Title,
				Goal = p.GameGoal!.DisplayName,
				Screenshot = p.Files
					.Where(f => f.Type == FileType.Screenshot)
					.Select(f => new DisplayMiniMovie.MiniMovieModel.ScreenshotFile
					{
						Path = f.Path,
						Description = f.Description
					})
					.First(),
				OnlineWatchingUrl = p.PublicationUrls
					.First(u => u.Type == PublicationUrlType.Streaming).Url,
				GameTitle = p.GameVersion != null && p.GameVersion.TitleOverride != null
					? p.GameVersion.TitleOverride
					: p.Game!.DisplayName
			})
			.ToListAsync();

		Movies = [.. movies
			.Select(m => new TabMiniMovieModel(
				movies.Count(mm => mm.Goal == m.Goal) > 1
					? m.GameTitle
					: m.Goal == "baseline"
					? "(baseline)"
					: "",
				m.Goal == "baseline"
					? ""
					: m.Goal,
				new DisplayMiniMovie.MiniMovieModel
				{
					Id = m.Id,
					Title = m.Title,
					Goal = m.Goal,
					Screenshot = m.Screenshot,
					OnlineWatchingUrl = m.OnlineWatchingUrl
				}))];

		PlaygroundGoals = [.. Game.PlaygroundSubmissions
			.GroupBy(s => new
			{
				s.Goal,
				GameTitle = s.Version.TitleOverride
					?? s.GameTitle
			})
			.OrderBy(gg => gg.Key.Goal.DisplayName.Length)
			.Select(gg => new GoalEntry(
				gg.Key.Goal.Id,
				gg.Key.Goal.DisplayName == "baseline"
					? "(baseline)"
					: gg.Key.Goal.DisplayName,
				gg.Key.GameTitle,
				[.. gg.OrderByDescending(ggs => ggs.Id).Select(ggs => new SubmissionEntry(ggs.Id, ggs.SubmissionTitle))]))];

		WatchFiles = await db.UserFiles
			.ForGame(Game.Id)
			.ThatArePublic()
			.Where(u => u.Type == "wch")
			.Select(u => new WatchFile(u.Id, u.FileName))
			.ToListAsync();

		Topics = await db.ForumTopics
			.ForGame(Game.Id)
			.Select(t => new TopicEntry(t.Id, t.Title))
			.ToListAsync();

		return Page();
	}

	public record WatchFile(long Id, string FileName);
	public record TopicEntry(int Id, string Title);
	public record SubmissionEntry(int Id, string Title);
	public record GoalEntry(int Id, string Name, string GameTitle, List<SubmissionEntry> Submissions);
	public record PlaygroundSubmission(int Id, string SubmissionTitle, string GameTitle, GameGoal Goal, GameVersion Version);

	public class GameDisplay
	{
		public int Id { get; init; }
		public string DisplayName { get; init; } = "";
		public string? Abbreviation { get; init; }
		public string? Aliases { get; init; }
		public string? ScreenshotUrl { get; init; }
		public string? GameResourcesPage { get; init; }
		public List<string> Genres { get; init; } = [];
		public List<GameVersion> Versions { get; init; } = [];
		public List<GameGroup> GameGroups { get; init; } = [];
		public List<PlaygroundSubmission> PlaygroundSubmissions { get; init; } = [];
		public int PublicationCount { get; init; }
		public int ObsoletePublicationCount { get; init; }
		public int SubmissionCount { get; init; }
		public int UserFilesCount { get; init; }

		public record GameVersion(
			VersionTypes Type,
			string? Md5,
			string? Sha1,
			string Name,
			string? Region,
			string? Version,
			string? SystemCode,
			string? TitleOverride);

		public record GameGroup(int Id, string Name);
	}

	/// <summary>
	/// Tab for MiniMovieModel
	/// </summary>
	/// <param name="TabTitleRegular">for baseline and disambiguating the clashes</param>
	/// <param name="TabTitleBold">for actual branch labels that appear in movie titles</param>
	/// <param name="Movie">MiniMovieModel</param>
	public record TabMiniMovieModel(
		string TabTitleRegular,
		string TabTitleBold,
		DisplayMiniMovie.MiniMovieModel Movie);
}
