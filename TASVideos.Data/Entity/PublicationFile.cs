namespace TASVideos.Data.Entity;

public enum FileType
{
	Screenshot, MovieFile
}

public class PublicationFile : BaseEntity
{
	public int Id { get; set; }

	public int PublicationId { get; set; }
	public Publication? Publication { get; set; }

	public string Path { get; set; } = "";
	public FileType Type { get; set; }

	public string? Description { get; set; }

	[Obsolete("Use MovieFileId instead")]
	public byte[]? FileData { get; set; }

	[Obsolete("Use MovieFileId instead")]
	public Compression? CompressionType { get; set; }

	public int? MovieFileId { get; set; }
	public MovieFile? MovieFileData { get; set; }
}

public static class PublicationFileExtensions
{
	public static IQueryable<PublicationFile> ForPublication(this IQueryable<PublicationFile> query, int publicationId)
		=> query.Where(pf => pf.PublicationId == publicationId);

	public static IQueryable<PublicationFile> ThatAreMovieFiles(this IQueryable<PublicationFile> query)
		=> query.Where(pf => pf.FileData != null);
}
