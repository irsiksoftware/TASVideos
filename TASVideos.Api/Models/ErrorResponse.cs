namespace TASVideos.Api.Models;

/// <summary>
/// Represents a standardized error response returned by the API.
/// </summary>
public class ErrorResponse
{
	/// <summary>
	/// Gets or sets the error title.
	/// </summary>
	public string Title { get; set; } = "";

	/// <summary>
	/// Gets or sets the HTTP status code.
	/// </summary>
	public int Status { get; set; }

	/// <summary>
	/// Gets or sets additional error details (optional).
	/// </summary>
	public string? Message { get; set; }
}
