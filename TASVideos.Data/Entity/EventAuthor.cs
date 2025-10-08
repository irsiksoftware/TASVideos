namespace TASVideos.Data.Entity;

public class EventAuthor
{
	public int UserId { get; set; }
	public User? Author { get; set; }

	public int EventId { get; set; }
	public Event? Event { get; set; }

	public int Ordinal { get; set; }
}
