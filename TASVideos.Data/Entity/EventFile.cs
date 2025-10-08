namespace TASVideos.Data.Entity;

public class EventFile
{
	public int Id { get; set; }
	public int EventId { get; set; }
	public Event? Event { get; set; }

	public string Path { get; set; } = "";
	public string Type { get; set; } = "";
	public string Description { get; set; } = "";
}
