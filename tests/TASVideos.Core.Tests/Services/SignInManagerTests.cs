using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TASVideos.Core.Services.Wiki;
using TASVideos.Data.Entity;

namespace TASVideos.Core.Tests.Services;

[TestClass]
public sealed class SignInManagerTests : TestDbBase
{
	private readonly SignInManager _signInManager;

	public SignInManagerTests()
	{
		var identityOptions = Substitute.For<IOptions<IdentityOptions>>();
		var userManager = new UserManager(
			_db,
			new TestCache(),
			Substitute.For<IPointsService>(),
			Substitute.For<ITASVideoAgent>(),
			Substitute.For<IWikiPages>(),
			Substitute.For<IUserStore<User>>(),
			identityOptions,
			Substitute.For<IPasswordHasher<User>>(),
			Substitute.For<IEnumerable<IUserValidator<User>>>(),
			Substitute.For<IEnumerable<IPasswordValidator<User>>>(),
			Substitute.For<ILookupNormalizer>(),
			new IdentityErrorDescriber(),
			Substitute.For<IServiceProvider>(),
			Substitute.For<ILogger<UserManager<User>>>());

		_signInManager = new SignInManager(
			_db,
			userManager,
			Substitute.For<IHttpContextAccessor>(),
			Substitute.For<IUserClaimsPrincipalFactory<User>>(),
			identityOptions,
			Substitute.For<ILogger<SignInManager<User>>>(),
			Substitute.For<IAuthenticationSchemeProvider>(),
			Substitute.For<IUserConfirmation<User>>());
	}

	[TestMethod]
	[DataRow(null, null, null, false)]
	[DataRow("test", "", "test", false)]
	[DataRow("test", "", "test123", true)]
	[DataRow("test", "test123@example.com", "test123", false)]
	public void IsPasswordAllowed_Tests(string userName, string email, string password, bool expected)
	{
		var actual = _signInManager.IsPasswordAllowed(userName, email, password);
		Assert.AreEqual(expected, actual);
	}

	[TestMethod]
	public void IsPasswordAllowed_WhenPasswordMatchesUsername_ReturnsFalse()
	{
		var result = _signInManager.IsPasswordAllowed("myusername", "email@test.com", "myusername");
		Assert.IsFalse(result);
	}

	[TestMethod]
	public void IsPasswordAllowed_WhenPasswordMatchesEmail_ReturnsFalse()
	{
		var result = _signInManager.IsPasswordAllowed("user", "myemail@test.com", "myemail@test.com");
		Assert.IsFalse(result);
	}

	[TestMethod]
	public void IsPasswordAllowed_WhenPasswordMatchesEmailPrefix_ReturnsFalse()
	{
		var result = _signInManager.IsPasswordAllowed("user", "myemail@test.com", "myemail");
		Assert.IsFalse(result);
	}

	[TestMethod]
	public void IsPasswordAllowed_WhenPasswordDifferentFromAll_ReturnsTrue()
	{
		var result = _signInManager.IsPasswordAllowed("user", "email@test.com", "ComplexP@ssw0rd!");
		Assert.IsTrue(result);
	}

	[TestMethod]
	public void IsPasswordAllowed_WhenUsernameIsNull_DoesNotThrow()
	{
		var result = _signInManager.IsPasswordAllowed(null, "email@test.com", "password");
		Assert.IsTrue(result);
	}

	[TestMethod]
	public void IsPasswordAllowed_WhenEmailIsNull_DoesNotThrow()
	{
		var result = _signInManager.IsPasswordAllowed("user", null, "password");
		Assert.IsTrue(result);
	}

	[TestMethod]
	public void IsPasswordAllowed_WhenPasswordIsNull_DoesNotThrow()
	{
		var result = _signInManager.IsPasswordAllowed("user", "email@test.com", null);
		Assert.IsTrue(result);
	}

	[TestMethod]
	public async Task EmailExists_WhenEmailIsEmpty_ReturnsFalse()
	{
		var result = await _signInManager.EmailExists("");
		Assert.IsFalse(result);
	}

	[TestMethod]
	public async Task EmailExists_WhenEmailIsNull_ReturnsFalse()
	{
		var result = await _signInManager.EmailExists(null!);
		Assert.IsFalse(result);
	}

	[TestMethod]
	public async Task EmailExists_WithPlusAlias_StripsAliasBeforeChecking()
	{
		_db.Users.Add(new User
		{
			UserName = "testuser",
			Email = "test@example.com"
		});
		await _db.SaveChangesAsync();

		var result = await _signInManager.EmailExists("test+alias@example.com");
		Assert.IsTrue(result);
	}

	[TestMethod]
	public async Task EmailExists_WhenNotFound_ReturnsFalse()
	{
		var result = await _signInManager.EmailExists("nonexistent@example.com");
		Assert.IsFalse(result);
	}

	[TestMethod]
	public async Task UsernameIsAllowed_WhenNoDisallows_ReturnsTrue()
	{
		var result = await _signInManager.UsernameIsAllowed("validusername");
		Assert.IsTrue(result);
	}

	[TestMethod]
	public async Task UsernameIsAllowed_WhenMatchesDisallowPattern_ReturnsFalse()
	{
		_db.UserDisallows.Add(new UserDisallow { RegexPattern = "^admin.*" });
		await _db.SaveChangesAsync();

		var result = await _signInManager.UsernameIsAllowed("admin123");
		Assert.IsFalse(result);
	}

	[TestMethod]
	public async Task UsernameIsAllowed_WhenDoesNotMatchDisallowPattern_ReturnsTrue()
	{
		_db.UserDisallows.Add(new UserDisallow { RegexPattern = "^admin.*" });
		await _db.SaveChangesAsync();

		var result = await _signInManager.UsernameIsAllowed("user123");
		Assert.IsTrue(result);
	}

	[TestMethod]
	public async Task UsernameIsAllowed_WithMultiplePatterns_ChecksAll()
	{
		_db.UserDisallows.Add(new UserDisallow { RegexPattern = "^admin.*" });
		_db.UserDisallows.Add(new UserDisallow { RegexPattern = ".*moderator.*" });
		await _db.SaveChangesAsync();

		var result1 = await _signInManager.UsernameIsAllowed("admin123");
		var result2 = await _signInManager.UsernameIsAllowed("user_moderator_test");
		var result3 = await _signInManager.UsernameIsAllowed("normaluser");

		Assert.IsFalse(result1);
		Assert.IsFalse(result2);
		Assert.IsTrue(result3);
	}
}
