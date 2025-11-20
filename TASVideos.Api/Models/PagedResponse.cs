namespace TASVideos.Api.Models;

/// <summary>
/// Wrapper for paginated API responses that includes metadata about field selection.
/// </summary>
/// <typeparam name="T">The type of items in the response.</typeparam>
public class PagedResponse<T>
{
	/// <summary>
	/// Gets or sets the collection of items.
	/// </summary>
	public IEnumerable<T> Items { get; set; } = [];

	/// <summary>
	/// Gets or sets the total number of items available (before pagination).
	/// </summary>
	public int TotalCount { get; set; }

	/// <summary>
	/// Gets or sets the requested page size.
	/// </summary>
	public int PageSize { get; set; }

	/// <summary>
	/// Gets or sets the current page number.
	/// </summary>
	public int CurrentPage { get; set; }

	/// <summary>
	/// Gets or sets the actual number of items returned.
	/// Note: When field selection is used, this may be less than PageSize due to deduplication.
	/// </summary>
	public int ActualCount { get; set; }

	/// <summary>
	/// Gets or sets whether field selection was applied.
	/// </summary>
	public bool FieldSelectionApplied { get; set; }

	/// <summary>
	/// Gets or sets a warning message if applicable (e.g., when ActualCount &lt; PageSize due to field selection).
	/// </summary>
	public string? Warning { get; set; }
}
