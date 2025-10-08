using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TASVideos.Data;
using TASVideos.Data.Entity;

namespace TASVideos.Pages.Reports;

[Authorize(Roles = "Admin,Moderator")]
public class IndexModel(ApplicationDbContext db) : PageModel
{
	public List<ReportEntry> Reports { get; set; } = [];

	public async Task OnGet(bool showResolved = false)
	{
		var query = db.Reports
			.Include(r => r.ForumPost)
			.Include(r => r.Reporter)
			.Include(r => r.ResolvedByUser)
			.AsQueryable();

		if (!showResolved)
		{
			query = query.Where(r => !r.Resolved);
		}

		Reports = await query
			.OrderByDescending(r => r.ReportedOn)
			.Select(r => new ReportEntry(
				r.Id,
				r.ForumPostId,
				r.Reporter!.UserName,
				r.Reason,
				r.ReportedOn,
				r.Resolved,
				r.ResolvedByUser != null ? r.ResolvedByUser.UserName : null,
				r.ResolvedOn,
				r.Resolution))
			.ToListAsync();
	}

	public record ReportEntry(
		int Id,
		int ForumPostId,
		string ReporterName,
		string Reason,
		DateTime ReportedOn,
		bool Resolved,
		string? ResolvedByUserName,
		DateTime? ResolvedOn,
		string? Resolution
	);
}
