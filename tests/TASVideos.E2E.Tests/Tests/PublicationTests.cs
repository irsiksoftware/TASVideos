using TASVideos.E2E.Tests.Base;

namespace TASVideos.E2E.Tests.Tests;

[TestClass]
public class PublicationTests : BaseE2ETest
{
	#region Basic Navigation Tests

	[TestMethod]
	public async Task PublicationsIndex_LoadsSuccessfully()
	{
		AssertEnabled();

		var response = await Navigate("/Movies");

		AssertResponseCode(response, 200);
		await AssertHasLink("Publications/Filter", "filter link should exist");
		await AssertElementExists(".page-title", "page title should exist");
		await AssertElementExists("a[href*='/Movies']", "publication links should exist");
	}

	[TestMethod]
	public async Task PublicationView_LoadsWithCorrectMetadata()
	{
		AssertEnabled();

		var response = await Navigate("/1007M");

		AssertResponseCode(response, 200);
		await AssertHasLink("1007M?handler=Download", "download link should exist");

		// Verify meta tags exist for SEO
		var metaDescription = await Page.GetAttributeAsync("meta[name='description']", "content");
		Assert.IsNotNull(metaDescription, "Meta description should exist for SEO");

		var metaOgTitle = await Page.GetAttributeAsync("meta[property='og:title']", "content");
		Assert.IsNotNull(metaOgTitle, "Open Graph title should exist for SEO");

		var metaOgImage = await Page.GetAttributeAsync("meta[property='og:image']", "content");
		Assert.IsNotNull(metaOgImage, "Open Graph image should exist for SEO");
	}

	[TestMethod]
	public async Task PublicationView_ContainsPublicationHistory()
	{
		AssertEnabled();

		var response = await Navigate("/1007M");

		AssertResponseCode(response, 200);
		await AssertElementContainsText("h4", "Publication History", "publication history section should exist");
	}

	[TestMethod]
	public async Task PublicationView_HasChangeLogLink()
	{
		AssertEnabled();

		var response = await Navigate("/1007M");

		AssertResponseCode(response, 200);
		await AssertHasLink("Logs/Index", "change log link should exist");
		await AssertHasLink("Wiki/PageHistory", "page history link should exist");
		await AssertHasLink("Wiki/Referrers", "referrers link should exist");
	}

	#endregion

	#region Filter and Search Tests

	[TestMethod]
	public async Task PublicationsFilter_LoadsSuccessfully()
	{
		AssertEnabled();

		var response = await Navigate("/Publications/Filter");

		AssertResponseCode(response, 200);
		await AssertElementExists("select[data-id='systems']", "platform filter should exist");
		await AssertElementExists("select[data-id='classes']", "class filter should exist");
		await AssertElementExists("select[data-id='years']", "year filter should exist");
		await AssertElementExists("select[data-id='genres']", "genre filter should exist");
		await AssertElementExists("select[data-id='flags']", "flags filter should exist");
		await AssertElementExists("select[data-id='tags']", "tags filter should exist");
		await AssertElementExists("#filter-btn", "filter button should exist");
	}

	[TestMethod]
	[DataRow("nes")]
	[DataRow("snes")]
	[DataRow("n64")]
	[DataRow("genesis")]
	public async Task MoviesByPlatform_LoadsSuccessfully(string platform)
	{
		AssertEnabled();

		var response = await Navigate($"/Movies-{platform}");

		AssertResponseCode(response, 200);
		await AssertHasLink("Publications/Filter", "filter link should exist");
		await AssertElementContainsText("label", "Showing items [1 - ", "pagination info should be displayed");
	}

	[TestMethod]
	[DataRow("standard")]
	[DataRow("stars")]
	[DataRow("moons")]
	public async Task MoviesByClass_LoadsSuccessfully(string classType)
	{
		AssertEnabled();

		var response = await Navigate($"/Movies-{classType}");

		AssertResponseCode(response, 200);
		await AssertHasLink("Publications/Filter", "filter link should exist");
		await AssertElementContainsText("label", "Showing items [1 - ", "pagination info should be displayed");
	}

	[TestMethod]
	[DataRow("Y2007")]
	[DataRow("Y2010")]
	public async Task MoviesByYear_LoadsSuccessfully(string year)
	{
		AssertEnabled();

		var response = await Navigate($"/Movies-{year}");

		AssertResponseCode(response, 200);
		await AssertHasLink("Publications/Filter", "filter link should exist");
		await AssertElementContainsText("label", "Showing items [1 - ", "pagination info should be displayed");
	}

	[TestMethod]
	[DataRow("rpg")]
	[DataRow("platformer")]
	public async Task MoviesByGenre_LoadsSuccessfully(string genre)
	{
		AssertEnabled();

		var response = await Navigate($"/Movies-{genre}");

		AssertResponseCode(response, 200);
		await AssertHasLink("Publications/Filter", "filter link should exist");
	}

	[TestMethod]
	[DataRow("author782")]
	public async Task MoviesByAuthor_LoadsSuccessfully(string author)
	{
		AssertEnabled();

		var response = await Navigate($"/Movies-{author}");

		AssertResponseCode(response, 200);
		await AssertHasLink("Publications/Filter", "filter link should exist");
		await AssertElementContainsText("label", "Showing items [1 - ", "pagination info should be displayed");
	}

	[TestMethod]
	[DataRow("2p")]
	[DataRow("group8")]
	public async Task MoviesByPlayerCount_LoadsSuccessfully(string playerCount)
	{
		AssertEnabled();

		var response = await Navigate($"/Movies-{playerCount}");

		AssertResponseCode(response, 200);
		await AssertHasLink("Publications/Filter", "filter link should exist");
		await AssertElementContainsText("label", "Showing items [1 - ", "pagination info should be displayed");
	}

	#endregion

	#region Pagination Tests

	[TestMethod]
	public async Task PublicationsList_PaginationWorks()
	{
		AssertEnabled();

		var response = await Navigate("/Movies?currentPage=2");

		AssertResponseCode(response, 200);
		await AssertElementContainsText("label", "Showing items [", "pagination info should be displayed");
		await AssertElementExists("a[href*='currentPage=']", "pagination links should exist");
	}

	[TestMethod]
	public async Task PublicationsList_HasCorrectPageSize()
	{
		AssertEnabled();

		var response = await Navigate("/Movies?pageSize=50");

		AssertResponseCode(response, 200);
		await AssertElementContainsText("label", "Showing items [1 - ", "pagination info should be displayed");
	}

	#endregion

	#region Authors Page Tests

	[TestMethod]
	public async Task PublicationAuthors_LoadsSuccessfully()
	{
		AssertEnabled();

		var response = await Navigate("/Publications/Authors");

		AssertResponseCode(response, 200);
		await AssertElementExists("table", "authors table should exist");
		await AssertElementExists("a[href*='/Movies-author']", "author links should exist");
	}

	#endregion

	#region YouTube Uploaders Tests

	[TestMethod]
	public async Task YoutubeUploaders_LoadsSuccessfully()
	{
		AssertEnabled();

		var response = await Navigate("/Publications/YoutubeUploaders");

		AssertResponseCode(response, 200);
		await AssertElementExists("table", "uploaders table should exist");
	}

	#endregion

	#region Download Tests

	[TestMethod]
	[DataRow(4987, "bk2", "nes")]
	[DataRow(5924, "ctm", "3ds")]
	[DataRow(2929, "dsm", "ds")]
	[DataRow(2679, "dtm", "wii")]
	[DataRow(1613, "fbm", "arcade")]
	[DataRow(1698, "fm2", "nes")]
	[DataRow(3582, "jrsr", "dos")]
	[DataRow(3099, "lsmv", "snes")]
	[DataRow(6473, "ltm", "dos")]
	[DataRow(1007, "m64", "n64")]
	[DataRow(2318, "omr", "msx")]
	[DataRow(1226, "vbm", "gb")]
	[DataRow(2484, "wtf", "windows")]
	public async Task PublicationFile_DownloadsAndParsesCorrectly(int id, string fileExt, string code)
	{
		AssertEnabled();

		var (downloadPath, archive) = await DownloadAndValidateZip(
			$"{id}M?handler=Download",
			$"publication_{id}M");

		var parseResult = await ParseMovieFile(downloadPath);
		Assert.IsTrue(parseResult.Success, $"Movie parsing failed with errors: {string.Join(", ", parseResult.Errors)}");
		Assert.AreEqual(fileExt, parseResult.FileExtension);
		Assert.AreEqual(code, parseResult.SystemCode);
		Assert.IsFalse(parseResult.Warnings.Any());
		Assert.IsFalse(parseResult.Errors.Any());

		CleanupZipDownload(downloadPath, archive);
	}

	#endregion

	#region Permission Tests

	[TestMethod]
	[DataRow("/Publications/AdditionalMovies/1")]
	[DataRow("/Publications/Catalog/1")]
	[DataRow("/Publications/Edit/1")]
	[DataRow("/Publications/EditClass/1")]
	[DataRow("/Publications/EditFiles/1")]
	[DataRow("/Publications/PrimaryMovie/1")]
	[DataRow("/Publications/Rate/1")]
	[DataRow("/Publications/Unpublish/1")]
	public async Task PermissionLockedPages_RedirectToLogin(string path)
	{
		AssertEnabled();

		var response = await Navigate(path);
		AssertRedirectToLogin(response);
	}

	[TestMethod]
	public async Task PublicationView_DoesNotShowEditLinksForAnonymous()
	{
		AssertEnabled();

		var response = await Navigate("/1007M");

		AssertResponseCode(response, 200);
		await AssertDoesNotHaveLink("Publications/Edit/", "edit link should not exist for anonymous users");
		await AssertDoesNotHaveLink("Publications/Catalog/", "catalog link should not exist for anonymous users");
		await AssertDoesNotHaveLink("Publications/Unpublish/", "unpublish link should not exist for anonymous users");
	}

	#endregion

	#region Legacy Routes Tests

	[TestMethod]
	[DataRow("name=smb", "1G")]
	[DataRow("id=1", "1M")]
	[DataRow("id=1,2,3", "1M-2M-3M")]
	[DataRow("rec=y", "NewcomerRec")]
	[DataRow("rec=anything", "NewcomerRec")]
	public async Task LegacyRoute_RedirectsCorrectly(string query, string expected)
	{
		AssertEnabled();

		var response = await Navigate($"/movies.cgi?{query}");
		AssertResponseCode(response, 200);
		Assert.IsNotNull(response);
		Assert.IsTrue(response.Url.Contains($"Movies-{expected}"),
			$"Expected URL to contain 'Movies-{expected}' but got {response.Url}");
	}

	#endregion

	#region Responsive Layout Tests

	[TestMethod]
	public async Task PublicationView_ResponsiveLayout_Mobile()
	{
		AssertEnabled();

		await Page.SetViewportSizeAsync(375, 667); // iPhone SE size

		var response = await Navigate("/1007M");

		AssertResponseCode(response, 200);
		await AssertHasLink("1007M?handler=Download", "download link should exist on mobile");

		// Verify content is accessible
		await AssertElementExists("body", "body should exist");
	}

	[TestMethod]
	public async Task PublicationView_ResponsiveLayout_Tablet()
	{
		AssertEnabled();

		await Page.SetViewportSizeAsync(768, 1024); // iPad size

		var response = await Navigate("/1007M");

		AssertResponseCode(response, 200);
		await AssertHasLink("1007M?handler=Download", "download link should exist on tablet");
	}

	[TestMethod]
	public async Task PublicationsList_ResponsiveLayout_Mobile()
	{
		AssertEnabled();

		await Page.SetViewportSizeAsync(375, 667); // iPhone SE size

		var response = await Navigate("/Movies");

		AssertResponseCode(response, 200);
		await AssertHasLink("Publications/Filter", "filter link should exist on mobile");
	}

	#endregion

	#region Statistics Tests

	[TestMethod]
	public async Task PublicationAuthors_ShowsStatistics()
	{
		AssertEnabled();

		var response = await Navigate("/Publications/Authors");

		AssertResponseCode(response, 200);

		// Verify statistics columns exist
		await AssertElementExists("table", "statistics table should exist");
		await AssertElementExists("th", "table headers should exist for statistics");
	}

	#endregion

	#region Combined Filter Tests

	[TestMethod]
	public async Task PublicationsList_CombinedFilters_PlatformAndClass()
	{
		AssertEnabled();

		var response = await Navigate("/Movies-nes-standard");

		AssertResponseCode(response, 200);
		await AssertHasLink("Publications/Filter", "filter link should exist");
		await AssertElementContainsText("label", "Showing items [1 - ", "pagination info should be displayed");
	}

	[TestMethod]
	public async Task PublicationsList_CombinedFilters_PlatformAndYear()
	{
		AssertEnabled();

		var response = await Navigate("/Movies-nes-Y2007");

		AssertResponseCode(response, 200);
		await AssertHasLink("Publications/Filter", "filter link should exist");
	}

	#endregion

	#region Additional View Tests

	[TestMethod]
	public async Task PublicationView_HasReferrersLink()
	{
		AssertEnabled();

		var response = await Navigate("/1007M");

		AssertResponseCode(response, 200);
		await AssertHasLink("Wiki/Referrers", "referrers link should exist");
	}

	[TestMethod]
	public async Task PublicationView_HasLatestDiffLink()
	{
		AssertEnabled();

		var response = await Navigate("/1007M");

		AssertResponseCode(response, 200);
		await AssertHasLink("Wiki/PageHistory", "page history link should exist");
		await AssertElementExists("a[href*='latest=true']", "latest diff link should exist");
	}

	#endregion
}
