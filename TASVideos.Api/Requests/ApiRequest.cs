namespace TASVideos.Api.Requests;

/// <summary>
/// Represents a standard API GET request.
/// Supports sorting, paging, and field selection parameters.
/// </summary>
/// <remarks>
/// <para><strong>Field Selection Behavior:</strong></para>
/// <para>
/// When using the <see cref="Fields"/> parameter, the API applies field selection as post-processing
/// and returns distinct objects. This means the actual number of returned records may be less than
/// the requested <see cref="PageSize"/> due to deduplication.
/// </para>
/// <para>
/// <strong>Example:</strong> If you request 100 publications with pageSize=100 and fields=class,
/// and only 5 unique classes exist in the result set, you will receive only 5 records instead of 100.
/// </para>
/// <para>
/// This is intentional behavior to avoid returning duplicate data when only specific fields are requested.
/// If you need a guaranteed number of records, do not use field selection or request a larger page size.
/// </para>
/// </remarks>
internal class ApiRequest : IFieldSelectable, ISortable, IPageable
{
	/// <summary>
	/// Gets or sets the total number of records to return per page.
	/// </summary>
	/// <value>
	/// The number of records per page. Maximum value is 100. Defaults to 100 if not specified.
	/// </value>
	/// <remarks>
	/// When field selection is used, the actual number of returned records may be less than this value due to deduplication.
	/// </remarks>
	[SwaggerParameter("The total number of records to return. Maximum is 100. Defaults to 100 if not specified. Note: When using field selection, actual returned count may be less due to distinct/deduplication.")]
	public int? PageSize { get; init; } = 100;

	/// <summary>
	/// Gets or sets the current page number (1-based).
	/// </summary>
	/// <value>
	/// The page number to retrieve. Defaults to 1 if not specified.
	/// </value>
	[SwaggerParameter("The page number to retrieve (1-based). Defaults to 1 if not specified.")]
	public int? CurrentPage { get; init; } = 1;

	/// <summary>
	/// Gets or sets the fields to sort by.
	/// </summary>
	/// <value>
	/// A comma-separated list of field names. Prefix with '+' for ascending or '-' for descending order.
	/// Example: "-createTimestamp,+title" sorts by createTimestamp descending, then title ascending.
	/// </value>
	[SwaggerParameter("The fields to sort by. Comma-separated list. Prefix with '+' for ascending or '-' for descending. Example: '-createTimestamp,+title'. If not specified, a default sort is used.")]
	public string? Sort { get; init; }

	/// <summary>
	/// Gets or sets the fields to return in the response.
	/// </summary>
	/// <value>
	/// A comma-separated list of field names to include in the response. If not specified, all fields are returned.
	/// Example: "id,title,class" returns only those three fields.
	/// </value>
	/// <remarks>
	/// <strong>Important:</strong> Field selection applies deduplication, so the actual returned count may be less than pageSize.
	/// See class documentation for more details.
	/// </remarks>
	[SwaggerParameter("The fields to return (comma-separated). Example: 'id,title,class'. If not specified, all fields are returned. WARNING: Field selection applies deduplication which may reduce the actual returned count.")]
	public string? Fields { get; init; }

	/// <summary>
	/// The maximum allowed page size for any API request.
	/// </summary>
	public const int MaxPageSize = 100;
}

internal static class RequestableExtensions
{
	public static IQueryable<T> SortAndPaginate<T>(this IQueryable<T> source, ApiRequest request)
	{
		int offset = request.Offset();
		int limit = request.PageSize ?? ApiRequest.MaxPageSize;
		return source
			.SortBy(request)
			.Skip(offset)
			.Take(limit);
	}
}
