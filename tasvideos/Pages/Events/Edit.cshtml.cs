namespace TASVideos.Pages.Events;

[RequirePermission(PermissionTo.EditPublicationMetaData)]
public class EditModel(ApplicationDbContext db) : BasePageModel
{
	[FromRoute]
	public int Id { get; set; }

	[BindProperty]
	public EventEditModel Event { get; set; } = new();

	public async Task<IActionResult> OnGet()
	{
		var evt = await db.Events
			.Include(e => e.EventUrls)
			.SingleOrDefaultAsync(e => e.Id == Id);

		if (evt is null)
		{
			return NotFound();
		}

		Event = new EventEditModel
		{
			Title = evt.Title,
			EventName = evt.EventName,
			EventDate = evt.EventDate,
			EmulatorVersion = evt.EmulatorVersion,
			Urls = evt.EventUrls
				.Select(u => new EventUrlModel
				{
					Url = u.Url,
					Type = u.Type,
					DisplayName = u.DisplayName
				})
				.ToList()
		};

		return Page();
	}

	public async Task<IActionResult> OnPost()
	{
		if (!ModelState.IsValid)
		{
			return Page();
		}

		var evt = await db.Events
			.Include(e => e.EventUrls)
			.SingleOrDefaultAsync(e => e.Id == Id);

		if (evt is null)
		{
			return NotFound();
		}

		evt.Title = Event.Title;
		evt.EventName = Event.EventName;
		evt.EventDate = Event.EventDate;
		evt.EmulatorVersion = Event.EmulatorVersion;

		// Update URLs
		db.EventUrls.RemoveRange(evt.EventUrls);
		foreach (var url in Event.Urls.Where(u => !string.IsNullOrWhiteSpace(u.Url)))
		{
			evt.EventUrls.Add(new EventUrl
			{
				Url = url.Url,
				Type = url.Type,
				DisplayName = url.DisplayName
			});
		}

		await db.SaveChangesAsync();

		return RedirectToPage("View", new { Id });
	}

	public class EventEditModel
	{
		[Required]
		public string Title { get; set; } = "";

		[Required]
		public string EventName { get; set; } = "";

		public DateTime? EventDate { get; set; }

		public string? EmulatorVersion { get; set; }

		public List<EventUrlModel> Urls { get; set; } = [];
	}

	public class EventUrlModel
	{
		public string Url { get; set; } = "";
		public EventLinkType Type { get; set; }
		public string? DisplayName { get; set; }
	}
}
