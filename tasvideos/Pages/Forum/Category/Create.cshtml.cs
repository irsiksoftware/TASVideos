namespace TASVideos.Pages.Forum.Category;

[RequirePermission(PermissionTo.EditCategories)]
public class CreateModel(ApplicationDbContext db) : BasePageModel
{
	[BindProperty]
	public CategoryCreateEdit Category { get; set; } = new();

	public async Task<IActionResult> OnPost()
	{
		if (!ModelState.IsValid)
		{
			return Page();
		}

		var maxOrdinal = await db.ForumCategories
			.Select(c => (int?)c.Ordinal)
			.MaxAsync() ?? 0;

		var category = db.ForumCategories.Add(new Data.Entity.Forum.ForumCategory
		{
			Title = Category.Title,
			Description = Category.Description,
			Ordinal = maxOrdinal + 1
		}).Entity;

		SetMessage(await db.TrySaveChanges(), $"Forum Category {category.Title} created successfully.", "Unable to create category.");
		return RedirectToPage("/Forum/Index");
	}

	public class CategoryCreateEdit
	{
		[Required]
		[StringLength(50)]
		public string Title { get; init; } = "";

		[StringLength(1000)]
		public string? Description { get; init; }
	}
}
