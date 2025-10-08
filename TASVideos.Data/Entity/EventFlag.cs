namespace TASVideos.Data.Entity;

public class EventFlag
{
	public int EventId { get; set; }
	public Event? Event { get; set; }

	public int FlagId { get; set; }
	public Flag? Flag { get; set; }
}
