namespace TASVideos.Data.Entity;

public class MovieFile : BaseEntity
{
	public int Id { get; set; }

	public int? SubmissionId { get; set; }
	public Submission? Submission { get; set; }

	public int? PublicationId { get; set; }
	public Publication? Publication { get; set; }

	public byte[] FileData { get; set; } = [];
	public string? FileExtension { get; set; }
	public string? FileName { get; set; }
	public Compression? CompressionType { get; set; }
}

public static class MovieFileExtensions
{
	public static IQueryable<MovieFile> ForSubmission(this IQueryable<MovieFile> query, int submissionId)
		=> query.Where(mf => mf.SubmissionId == submissionId);

	public static IQueryable<MovieFile> ForPublication(this IQueryable<MovieFile> query, int publicationId)
		=> query.Where(mf => mf.PublicationId == publicationId);
}
