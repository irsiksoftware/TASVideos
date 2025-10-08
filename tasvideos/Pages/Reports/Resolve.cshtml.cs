using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TASVideos.Data;

namespace TASVideos.Pages.Reports;

[Authorize(Roles = "Admin,Moderator")]
public class ResolveModel(ApplicationDbContext db) : PageModel
{
	[BindProperty]
	public InputModel Input { get; set; } = new();

	[BindProperty(SupportsGet = true)]
	public int Id { get; set; }

	public async Task<IActionResult> OnGet()
	{
		var report = await db.Reports.FindAsync(Id);
		if (report == null)
		{
			return NotFound();
		}

		Input.ReportId = Id;
		return Page();
	}

	public async Task<IActionResult> OnPost()
	{
		if (!ModelState.IsValid)
		{
			return Page();
		}

		var report = await db.Reports.FindAsync(Input.ReportId);
		if (report == null)
		{
			return NotFound();
		}

		var userId = User.GetUserId();
		report.Resolved = true;
		report.ResolvedByUserId = userId;
		report.ResolvedOn = DateTime.UtcNow;
		report.Resolution = Input.Resolution;

		await db.SaveChangesAsync();

		return RedirectToPage("/Reports/Index");
	}

	public class InputModel
	{
		public int ReportId { get; set; }
		public string Resolution { get; set; } = "";
	}
}
