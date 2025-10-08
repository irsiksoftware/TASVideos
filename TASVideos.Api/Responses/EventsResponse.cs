namespace TASVideos.Api.Responses;

public record EventsResponse
{
	public int Id { get; init; }
	public string Title { get; init; } = "";
	public string EventName { get; init; } = "";
	public DateTime? EventDate { get; init; }
	public int SubmissionId { get; init; }
	public DateTime CreateTimestamp { get; init; }
	public List<string> Authors { get; init; } = [];
	public string? AdditionalAuthors { get; init; }
	public List<string> Tags { get; init; } = [];
	public List<EventUrlResponse> Urls { get; init; } = [];
}

public record EventUrlResponse
{
	public string Url { get; init; } = "";
	public string Type { get; init; } = "";
	public string? DisplayName { get; init; }
}
