namespace TASVideos.Data.Entity;

public enum EventLinkType
{
	[Display(Name = "Streaming Video")]
	Streaming = 1,

	[Display(Name = "Download")]
	Download = 2
}

public class EventUrl
{
	public int Id { get; set; }
	public int EventId { get; set; }
	public Event? Event { get; set; }

	public string Url { get; set; } = "";
	public EventLinkType Type { get; set; }
	public string? DisplayName { get; set; }
}
