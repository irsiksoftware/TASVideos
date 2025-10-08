using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TASVideos.Data;
using TASVideos.Data.Entity;

namespace TASVideos.Pages.Reports;

[Authorize]
public class CreateModel(ApplicationDbContext db) : PageModel
{
	[BindProperty]
	public InputModel Input { get; set; } = new();

	[BindProperty(SupportsGet = true)]
	public int ForumPostId { get; set; }

	public async Task<IActionResult> OnPost()
	{
		if (!ModelState.IsValid)
		{
			return Page();
		}

		var userId = User.GetUserId();
		var report = new Report
		{
			ForumPostId = Input.ForumPostId,
			ReporterId = userId,
			Reason = Input.Reason,
			ReportedOn = DateTime.UtcNow,
			Resolved = false
		};

		db.Reports.Add(report);
		await db.SaveChangesAsync();

		return RedirectToPage("/Reports/Index");
	}

	public class InputModel
	{
		public int ForumPostId { get; set; }
		public string Reason { get; set; } = "";
	}
}
