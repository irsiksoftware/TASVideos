namespace TASVideos.Pages.Events;

[AllowAnonymous]
public class IndexModel(ApplicationDbContext db) : BasePageModel
{
	public List<EventDisplay> Events { get; set; } = [];

	public async Task OnGet()
	{
		Events = await db.Events
			.Include(e => e.Authors)
			.ThenInclude(a => a.Author)
			.OrderByDescending(e => e.CreateTimestamp)
			.Select(e => new EventDisplay
			{
				Id = e.Id,
				Title = e.Title,
				EventName = e.EventName,
				EventDate = e.EventDate,
				CreateTimestamp = e.CreateTimestamp,
				Authors = e.Authors
					.OrderBy(a => a.Ordinal)
					.Select(a => a.Author!.UserName)
					.ToList()
			})
			.ToListAsync();
	}

	public class EventDisplay
	{
		public int Id { get; init; }
		public string Title { get; init; } = "";
		public string EventName { get; init; } = "";
		public DateTime? EventDate { get; init; }
		public DateTime CreateTimestamp { get; init; }
		public List<string> Authors { get; init; } = [];
	}
}
