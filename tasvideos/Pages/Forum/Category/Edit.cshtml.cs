namespace TASVideos.Pages.Forum.Category;

[RequirePermission(PermissionTo.EditCategories)]
public class EditModel(ApplicationDbContext db) : BasePageModel
{
	[FromRoute]
	public int Id { get; set; }

	[BindProperty]
	public CategoryEdit Category { get; set; } = new();

	public bool CanDelete { get; set; }

	public async Task<IActionResult> OnGet()
	{
		var category = await db.ForumCategories
			.Where(c => c.Id == Id)
			.Select(c => new CategoryEdit
			{
				Title = c.Title,
				Description = c.Description
			})
			.SingleOrDefaultAsync();

		if (category is null)
		{
			return NotFound();
		}

		Category = category;
		CanDelete = await CanBeDeleted();
		return Page();
	}

	public async Task<IActionResult> OnPost()
	{
		if (!ModelState.IsValid)
		{
			CanDelete = await CanBeDeleted();
			return Page();
		}

		var category = await db.ForumCategories
			.SingleOrDefaultAsync(c => c.Id == Id);

		if (category is null)
		{
			return NotFound();
		}

		category.Title = Category.Title;
		category.Description = Category.Description;

		SetMessage(await db.TrySaveChanges(), $"Category {category.Title} updated.", $"Unable to edit {category.Title}");
		return RedirectToPage("/Forum/Index");
	}

	public async Task<IActionResult> OnPostDelete()
	{
		if (!await CanBeDeleted())
		{
			return BadRequest("Cannot delete category that contains forums");
		}

		var category = await db.ForumCategories.SingleOrDefaultAsync(c => c.Id == Id);
		if (category is null)
		{
			return NotFound();
		}

		db.ForumCategories.Remove(category);
		SetMessage(await db.TrySaveChanges(), $"Category {category.Title} deleted successfully", $"Unable to delete Category {category.Title}");

		return RedirectToPage("/Forum/Index");
	}

	private async Task<bool> CanBeDeleted() => !await db.Forums.AnyAsync(f => f.CategoryId == Id);

	public class CategoryEdit
	{
		[Required]
		[StringLength(50)]
		public string Title { get; init; } = "";

		[StringLength(1000)]
		public string? Description { get; init; }
	}
}
