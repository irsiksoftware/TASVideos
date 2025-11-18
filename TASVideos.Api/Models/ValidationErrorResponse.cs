namespace TASVideos.Api.Models;

/// <summary>
/// Represents a validation error response with detailed field-level errors.
/// </summary>
public class ValidationErrorResponse
{
	/// <summary>
	/// Gets or sets the error type.
	/// </summary>
	public string Type { get; set; } = "https://tools.ietf.org/html/rfc7231#section-6.5.1";

	/// <summary>
	/// Gets or sets the error title.
	/// </summary>
	public string Title { get; set; } = "One or more validation errors occurred.";

	/// <summary>
	/// Gets or sets the HTTP status code.
	/// </summary>
	public int Status { get; set; } = 400;

	/// <summary>
	/// Gets or sets the dictionary of validation errors keyed by field name.
	/// </summary>
	public Dictionary<string, string[]> Errors { get; set; } = [];
}
