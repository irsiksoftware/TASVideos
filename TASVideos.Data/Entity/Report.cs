using TASVideos.Data.Entity.Forum;

namespace TASVideos.Data.Entity;

public class Report : BaseEntity
{
	public int Id { get; set; }
	public int ForumPostId { get; set; }
	public ForumPost? ForumPost { get; set; }
	public int ReporterId { get; set; }
	public User? Reporter { get; set; }
	public string Reason { get; set; } = "";
	public DateTime ReportedOn { get; set; }
	public bool Resolved { get; set; }
	public int? ResolvedByUserId { get; set; }
	public User? ResolvedByUser { get; set; }
	public DateTime? ResolvedOn { get; set; }
	public string? Resolution { get; set; }
}
