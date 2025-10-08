namespace TASVideos.Pages.Events;

[AllowAnonymous]
public class ViewModel(ApplicationDbContext db) : BasePageModel
{
	[FromRoute]
	public int Id { get; set; }

	public EventDisplay Event { get; set; } = new();

	public async Task<IActionResult> OnGet()
	{
		var evt = await db.Events
			.Include(e => e.Authors)
			.ThenInclude(a => a.Author)
			.Include(e => e.EventUrls)
			.Include(e => e.EventTags)
			.ThenInclude(t => t.Tag)
			.Include(e => e.EventFlags)
			.ThenInclude(f => f.Flag)
			.Include(e => e.Submission)
			.ThenInclude(s => s!.Topic)
			.SingleOrDefaultAsync(e => e.Id == Id);

		if (evt is null)
		{
			return NotFound();
		}

		Event = new EventDisplay
		{
			Id = evt.Id,
			Title = evt.Title,
			EventName = evt.EventName,
			EventDate = evt.EventDate,
			CreateTimestamp = evt.CreateTimestamp,
			Authors = evt.Authors
				.OrderBy(a => a.Ordinal)
				.Select(a => a.Author!.UserName)
				.ToList(),
			AdditionalAuthors = evt.AdditionalAuthors,
			Urls = evt.EventUrls.ToList(),
			Tags = evt.EventTags.Select(t => t.Tag!.Code).ToList(),
			Flags = evt.EventFlags.Select(f => f.Flag!.Name).ToList(),
			SubmissionId = evt.SubmissionId,
			SubmissionTopicId = evt.Submission?.TopicId,
			Frames = evt.Frames,
			RerecordCount = evt.RerecordCount,
			EmulatorVersion = evt.EmulatorVersion
		};

		return Page();
	}

	public class EventDisplay
	{
		public int Id { get; init; }
		public string Title { get; init; } = "";
		public string EventName { get; init; } = "";
		public DateTime? EventDate { get; init; }
		public DateTime CreateTimestamp { get; init; }
		public List<string> Authors { get; init; } = [];
		public string? AdditionalAuthors { get; init; }
		public List<EventUrl> Urls { get; init; } = [];
		public List<string> Tags { get; init; } = [];
		public List<string> Flags { get; init; } = [];
		public int SubmissionId { get; init; }
		public int? SubmissionTopicId { get; init; }
		public int Frames { get; init; }
		public int RerecordCount { get; init; }
		public string? EmulatorVersion { get; init; }
	}
}
