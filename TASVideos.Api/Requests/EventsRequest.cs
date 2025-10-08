namespace TASVideos.Api.Requests;

public record EventsRequest : IFieldSelectable
{
	public ICollection<int> Years { get; init; } = [];
	public ICollection<string> Tags { get; init; } = [];
	public ICollection<int> Authors { get; init; } = [];
	public ICollection<string> EventNames { get; init; } = [];
	public string? SortBy { get; init; }
	public int? Limit { get; init; }
	public string? Fields { get; init; }
}
