namespace TASVideos.Data.Entity;

public class MovieFile : BaseEntity
{
	public int Id { get; set; }

	public byte[] FileData { get; set; } = [];

	public Compression CompressionType { get; set; }

	public int OriginalLength { get; set; }

	public string FileName { get; set; } = "";
}
