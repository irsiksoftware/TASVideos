using TASVideos.Data.AutoHistory;

namespace TASVideos.Data.Entity;

[IncludeInAutoHistory]
public class Event : BaseEntity
{
	public int Id { get; set; }

	public ICollection<EventFile> Files { get; init; } = [];
	public ICollection<EventTag> EventTags { get; init; } = [];
	public ICollection<EventFlag> EventFlags { get; init; } = [];

	public ICollection<EventUrl> EventUrls { get; init; } = [];

	// Events don't have single game/system, but can have multiple
	// We'll store the event name as the title
	public string Title { get; set; } = "";

	public int SubmissionId { get; set; }
	public Submission? Submission { get; set; }
	public ICollection<EventAuthor> Authors { get; init; } = [];

	public string? AdditionalAuthors { get; set; }

	// Events may or may not have frame data
	public int Frames { get; set; }
	public int RerecordCount { get; set; }
	public int? SystemFrameRateId { get; set; }
	public GameSystemFrameRate? SystemFrameRate { get; set; }

	public string? EmulatorVersion { get; set; }

	// Event-specific fields
	public string EventName { get; set; } = "";
	public DateTime? EventDate { get; set; }
}

public static class EventExtensions
{
	public static IQueryable<Event> IncludeTitleTables(this DbSet<Event> query)
		=> query
			.Include(e => e.Authors)
			.ThenInclude(ea => ea.Author)
			.Include(e => e.SystemFrameRate);
}
