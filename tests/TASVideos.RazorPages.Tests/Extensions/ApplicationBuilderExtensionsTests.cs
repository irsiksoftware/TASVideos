using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using TASVideos.Extensions;

namespace TASVideos.RazorPages.Tests.Extensions;

[TestClass]
public class ApplicationBuilderExtensionsTests
{
	[TestMethod]
	public void GetCrossOriginResourcePolicy_AuthenticatedUser_ReturnsSameOrigin()
	{
		// Arrange
		var context = CreateHttpContext(isAuthenticated: true, path: "/some/page");

		// Act
		var result = InvokeGetCrossOriginResourcePolicy(context);

		// Assert
		Assert.AreEqual("same-origin", result);
	}

	[TestMethod]
	public void GetCrossOriginResourcePolicy_AuthenticatedUserWithStaticAsset_ReturnsSameOrigin()
	{
		// Arrange - Even for static assets, authenticated users get same-origin
		var context = CreateHttpContext(isAuthenticated: true, path: "/images/test.png");

		// Act
		var result = InvokeGetCrossOriginResourcePolicy(context);

		// Assert
		Assert.AreEqual("same-origin", result);
	}

	[TestMethod]
	[DataRow("/js/site.js")]
	[DataRow("/css/style.css")]
	[DataRow("/images/logo.png")]
	[DataRow("/images/award.jpg")]
	[DataRow("/images/icon.svg")]
	[DataRow("/fonts/custom.woff2")]
	[DataRow("/media/video.mp4")]
	[DataRow("/awards/2024/winner.png")]
	public void GetCrossOriginResourcePolicy_UnauthenticatedUserWithStaticAsset_ReturnsCrossOrigin(string path)
	{
		// Arrange
		var context = CreateHttpContext(isAuthenticated: false, path: path);

		// Act
		var result = InvokeGetCrossOriginResourcePolicy(context);

		// Assert
		Assert.AreEqual("cross-origin", result);
	}

	[TestMethod]
	[DataRow("/Publications/1234")]
	[DataRow("/Submissions/View")]
	[DataRow("/Profile/Settings")]
	[DataRow("/api/endpoint")]
	[DataRow("/")]
	public void GetCrossOriginResourcePolicy_UnauthenticatedUserWithDynamicContent_ReturnsSameSite(string path)
	{
		// Arrange
		var context = CreateHttpContext(isAuthenticated: false, path: path);

		// Act
		var result = InvokeGetCrossOriginResourcePolicy(context);

		// Assert
		Assert.AreEqual("same-site", result);
	}

	[TestMethod]
	[DataRow("/script.JS")]
	[DataRow("/STYLE.CSS")]
	[DataRow("/Image.PNG")]
	public void IsStaticAsset_CaseInsensitive_ReturnsTrue(string path)
	{
		// Arrange
		var context = CreateHttpContext(isAuthenticated: false, path: path);

		// Act
		var result = InvokeIsStaticAsset(path);

		// Assert
		Assert.IsTrue(result);
	}

	[TestMethod]
	[DataRow(".js")]
	[DataRow(".css")]
	[DataRow(".png")]
	[DataRow(".jpg")]
	[DataRow(".jpeg")]
	[DataRow(".gif")]
	[DataRow(".svg")]
	[DataRow(".ico")]
	[DataRow(".webp")]
	[DataRow(".woff")]
	[DataRow(".woff2")]
	[DataRow(".ttf")]
	[DataRow(".eot")]
	[DataRow(".otf")]
	[DataRow(".json")]
	[DataRow(".xml")]
	[DataRow(".txt")]
	[DataRow(".mp4")]
	[DataRow(".webm")]
	[DataRow(".pdf")]
	[DataRow(".zip")]
	public void IsStaticAsset_SupportedExtensions_ReturnsTrue(string extension)
	{
		// Arrange
		var path = $"/path/to/file{extension}";

		// Act
		var result = InvokeIsStaticAsset(path);

		// Assert
		Assert.IsTrue(result, $"Extension {extension} should be recognized as static asset");
	}

	[TestMethod]
	[DataRow("/page.html")]
	[DataRow("/page.cshtml")]
	[DataRow("/page.aspx")]
	[DataRow("/api/endpoint")]
	public void IsStaticAsset_DynamicExtensions_ReturnsFalse(string path)
	{
		// Arrange & Act
		var result = InvokeIsStaticAsset(path);

		// Assert
		Assert.IsFalse(result, $"Path {path} should not be recognized as static asset");
	}

	[TestMethod]
	public void GetCrossOriginResourcePolicy_EmptyPath_ReturnsSameSite()
	{
		// Arrange
		var context = CreateHttpContext(isAuthenticated: false, path: "");

		// Act
		var result = InvokeGetCrossOriginResourcePolicy(context);

		// Assert
		Assert.AreEqual("same-site", result);
	}

	[TestMethod]
	public void GetCrossOriginResourcePolicy_NullPath_ReturnsSameSite()
	{
		// Arrange
		var context = CreateHttpContext(isAuthenticated: false, path: null);

		// Act
		var result = InvokeGetCrossOriginResourcePolicy(context);

		// Assert
		Assert.AreEqual("same-site", result);
	}

	private static HttpContext CreateHttpContext(bool isAuthenticated, string? path)
	{
		var context = new DefaultHttpContext();
		context.Request.Path = path ?? string.Empty;

		if (isAuthenticated)
		{
			var identity = new ClaimsIdentity("TestAuth");
			identity.AddClaim(new Claim(ClaimTypes.Name, "TestUser"));
			context.User = new ClaimsPrincipal(identity);
		}
		else
		{
			context.User = new ClaimsPrincipal();
		}

		return context;
	}

	// Use reflection to invoke the private static methods
	private static string InvokeGetCrossOriginResourcePolicy(HttpContext context)
	{
		var method = typeof(ApplicationBuilderExtensions)
			.GetMethod("GetCrossOriginResourcePolicy", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

		return (string)method!.Invoke(null, [context])!;
	}

	private static bool InvokeIsStaticAsset(string path)
	{
		var method = typeof(ApplicationBuilderExtensions)
			.GetMethod("IsStaticAsset", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

		return (bool)method!.Invoke(null, [path])!;
	}
}
