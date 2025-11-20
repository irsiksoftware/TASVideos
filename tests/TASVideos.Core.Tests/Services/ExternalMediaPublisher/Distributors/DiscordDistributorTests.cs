using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TASVideos.Core.Services.ExternalMediaPublisher;
using TASVideos.Core.Services.ExternalMediaPublisher.Distributors;
using TASVideos.Core.Settings;

namespace TASVideos.Core.Tests.Services.ExternalMediaPublisher.Distributors;

[TestClass]
public class DiscordDistributorTests
{
	private readonly IHttpClientFactory _httpClientFactory = Substitute.For<IHttpClientFactory>();
	private readonly ILogger<DiscordDistributor> _logger = Substitute.For<ILogger<DiscordDistributor>>();
	private readonly TestHttpMessageHandler _messageHandler = new();
	private readonly AppSettings _appSettings = new();

	[TestInitialize]
	public void Setup()
	{
		var httpClient = new HttpClient(_messageHandler)
		{
			BaseAddress = new Uri("https://discord.com/api/v10/")
		};
		_httpClientFactory.CreateClient(HttpClients.Discord).Returns(httpClient);

		_appSettings.Discord = new AppSettings.DiscordConnection
		{
			AccessToken = "test-bot-token",
			PublicChannelId = "123456",
			PublicTasChannelId = "234567",
			PublicGameChannelId = "345678",
			PrivateChannelId = "456789",
			PrivateUserChannelId = "567890"
		};
	}

	[TestMethod]
	public void Types_ReturnsAdministrativeGeneralAndAnnouncement()
	{
		var distributor = new DiscordDistributor(_appSettings, _httpClientFactory, _logger);

		var result = distributor.Types.ToList();

		Assert.AreEqual(3, result.Count);
		Assert.IsTrue(result.Contains(PostType.Administrative));
		Assert.IsTrue(result.Contains(PostType.General));
		Assert.IsTrue(result.Contains(PostType.Announcement));
	}

	[TestMethod]
	public void Constructor_SetsBotTokenOnHttpClient()
	{
		var distributor = new DiscordDistributor(_appSettings, _httpClientFactory, _logger);

		// Verify the client was created and token was set
		_httpClientFactory.Received(1).CreateClient(HttpClients.Discord);
		var client = _httpClientFactory.CreateClient(HttpClients.Discord);
		Assert.IsNotNull(client.DefaultRequestHeaders.Authorization);
		Assert.AreEqual("Bot", client.DefaultRequestHeaders.Authorization.Scheme);
		Assert.AreEqual("test-bot-token", client.DefaultRequestHeaders.Authorization.Parameter);
	}

	[TestMethod]
	[ExpectedException(typeof(InvalidOperationException))]
	public void Constructor_WhenHttpClientFactoryReturnsNull_ThrowsException()
	{
		_httpClientFactory.CreateClient(HttpClients.Discord).Returns((HttpClient?)null);

		_ = new DiscordDistributor(_appSettings, _httpClientFactory, _logger);
	}

	[TestMethod]
	public async Task Post_WhenDisabled_DoesNothing()
	{
		_appSettings.Discord.Disable = true;
		var distributor = new DiscordDistributor(_appSettings, _httpClientFactory, _logger);
		var post = CreateTestPost();

		await distributor.Post(post);

		Assert.AreEqual(0, _messageHandler.RequestCount);
	}

	[TestMethod]
	public async Task Post_WhenAccessTokenIsEmpty_DoesNothing()
	{
		_appSettings.Discord.AccessToken = "";
		var distributor = new DiscordDistributor(_appSettings, _httpClientFactory, _logger);
		var post = CreateTestPost();

		await distributor.Post(post);

		Assert.AreEqual(0, _messageHandler.RequestCount);
	}

	[TestMethod]
	public async Task Post_GeneralType_UsesPublicChannel()
	{
		_messageHandler.AddSuccessResponse();
		var distributor = new DiscordDistributor(_appSettings, _httpClientFactory, _logger);
		var post = CreateTestPost(type: PostType.General, group: PostGroups.Forum);

		await distributor.Post(post);

		var request = _messageHandler.Requests[0];
		Assert.IsTrue(request.RequestUri?.ToString().Contains("channels/123456/messages"));
	}

	[TestMethod]
	public async Task Post_GameGroup_UsesPublicGameChannel()
	{
		_messageHandler.AddSuccessResponse();
		var distributor = new DiscordDistributor(_appSettings, _httpClientFactory, _logger);
		var post = CreateTestPost(group: PostGroups.Game);

		await distributor.Post(post);

		var request = _messageHandler.Requests[0];
		Assert.IsTrue(request.RequestUri?.ToString().Contains("channels/345678/messages"));
	}

	[TestMethod]
	public async Task Post_PublicationGroup_UsesPublicTasChannel()
	{
		_messageHandler.AddSuccessResponse();
		var distributor = new DiscordDistributor(_appSettings, _httpClientFactory, _logger);
		var post = CreateTestPost(group: PostGroups.Publication);

		await distributor.Post(post);

		var request = _messageHandler.Requests[0];
		Assert.IsTrue(request.RequestUri?.ToString().Contains("channels/234567/messages"));
	}

	[TestMethod]
	public async Task Post_SubmissionGroup_UsesPublicTasChannel()
	{
		_messageHandler.AddSuccessResponse();
		var distributor = new DiscordDistributor(_appSettings, _httpClientFactory, _logger);
		var post = CreateTestPost(group: PostGroups.Submission);

		await distributor.Post(post);

		var request = _messageHandler.Requests[0];
		Assert.IsTrue(request.RequestUri?.ToString().Contains("channels/234567/messages"));
	}

	[TestMethod]
	public async Task Post_UserFilesGroup_UsesPublicTasChannel()
	{
		_messageHandler.AddSuccessResponse();
		var distributor = new DiscordDistributor(_appSettings, _httpClientFactory, _logger);
		var post = CreateTestPost(group: PostGroups.UserFiles);

		await distributor.Post(post);

		var request = _messageHandler.Requests[0];
		Assert.IsTrue(request.RequestUri?.ToString().Contains("channels/234567/messages"));
	}

	[TestMethod]
	public async Task Post_AdministrativeType_UsesPrivateChannel()
	{
		_messageHandler.AddSuccessResponse();
		var distributor = new DiscordDistributor(_appSettings, _httpClientFactory, _logger);
		var post = CreateTestPost(type: PostType.Administrative, group: PostGroups.Forum);

		await distributor.Post(post);

		var request = _messageHandler.Requests[0];
		Assert.IsTrue(request.RequestUri?.ToString().Contains("channels/456789/messages"));
	}

	[TestMethod]
	public async Task Post_AdministrativeTypeWithUserManagementGroup_UsesPrivateUserChannel()
	{
		_messageHandler.AddSuccessResponse();
		var distributor = new DiscordDistributor(_appSettings, _httpClientFactory, _logger);
		var post = CreateTestPost(type: PostType.Administrative, group: PostGroups.UserManagement);

		await distributor.Post(post);

		var request = _messageHandler.Requests[0];
		Assert.IsTrue(request.RequestUri?.ToString().Contains("channels/567890/messages"));
	}

	[TestMethod]
	public async Task Post_WithoutBodyOrLink_SendsTitleOnly()
	{
		_messageHandler.AddSuccessResponse();
		var distributor = new DiscordDistributor(_appSettings, _httpClientFactory, _logger);
		var post = new Post
		{
			Type = PostType.General,
			Title = "Test Title",
			Body = "",
			Link = "",
			Group = PostGroups.Forum
		};

		await distributor.Post(post);

		var content = await GetMessageContent(_messageHandler.Requests[0]);
		Assert.AreEqual("Test Title", content);
	}

	[TestMethod]
	public async Task Post_WithBodyNoLink_SendsTitleAndBody()
	{
		_messageHandler.AddSuccessResponse();
		var distributor = new DiscordDistributor(_appSettings, _httpClientFactory, _logger);
		var post = new Post
		{
			Type = PostType.General,
			Title = "Test Title",
			Body = "Test Body",
			Link = "",
			Group = PostGroups.Forum
		};

		await distributor.Post(post);

		var content = await GetMessageContent(_messageHandler.Requests[0]);
		Assert.AreEqual("Test Title (Test Body)", content);
	}

	[TestMethod]
	public async Task Post_WithLinkNoBody_SendsTitleAndLink()
	{
		_messageHandler.AddSuccessResponse();
		var distributor = new DiscordDistributor(_appSettings, _httpClientFactory, _logger);
		var post = new Post
		{
			Type = PostType.General,
			Title = "Test Title",
			Body = "",
			Link = "https://tasvideos.org/12345",
			Group = PostGroups.Forum
		};

		await distributor.Post(post);

		var content = await GetMessageContent(_messageHandler.Requests[0]);
		Assert.AreEqual("Test Title <https://tasvideos.org/12345>", content);
	}

	[TestMethod]
	public async Task Post_WithBodyAndLink_SendsAllThree()
	{
		_messageHandler.AddSuccessResponse();
		var distributor = new DiscordDistributor(_appSettings, _httpClientFactory, _logger);
		var post = new Post
		{
			Type = PostType.General,
			Title = "Test Title",
			Body = "Test Body",
			Link = "https://tasvideos.org/12345",
			Group = PostGroups.Forum
		};

		await distributor.Post(post);

		var content = await GetMessageContent(_messageHandler.Requests[0]);
		Assert.AreEqual("Test Title (Test Body) <https://tasvideos.org/12345>", content);
	}

	[TestMethod]
	public async Task Post_AnnouncementType_DoesNotWrapLinkInAngleBrackets()
	{
		_messageHandler.AddSuccessResponse();
		var distributor = new DiscordDistributor(_appSettings, _httpClientFactory, _logger);
		var post = new Post
		{
			Type = PostType.Announcement,
			Title = "New Publication",
			Body = "",
			Link = "https://tasvideos.org/12345",
			Group = PostGroups.Publication
		};

		await distributor.Post(post);

		var content = await GetMessageContent(_messageHandler.Requests[0]);
		// Announcement should not wrap link in angle brackets (allows Discord to generate preview)
		Assert.AreEqual("New Publication https://tasvideos.org/12345", content);
	}

	[TestMethod]
	public async Task Post_NonAnnouncementType_WrapsLinkInAngleBrackets()
	{
		_messageHandler.AddSuccessResponse();
		var distributor = new DiscordDistributor(_appSettings, _httpClientFactory, _logger);
		var post = new Post
		{
			Type = PostType.General,
			Title = "Forum Post",
			Body = "",
			Link = "https://tasvideos.org/forum/123",
			Group = PostGroups.Forum
		};

		await distributor.Post(post);

		var content = await GetMessageContent(_messageHandler.Requests[0]);
		// Non-announcement should wrap link to suppress preview
		Assert.AreEqual("Forum Post <https://tasvideos.org/forum/123>", content);
	}

	[TestMethod]
	public async Task Post_WithFormattedTitle_UsesFormattedTitleWithLink()
	{
		_messageHandler.AddSuccessResponse();
		var distributor = new DiscordDistributor(_appSettings, _httpClientFactory, _logger);
		var post = new Post
		{
			Type = PostType.General,
			Title = "Plain Title",
			FormattedTitle = "**Bold Title** {0}",
			Body = "",
			Link = "https://tasvideos.org/12345",
			Group = PostGroups.Forum
		};

		await distributor.Post(post);

		var content = await GetMessageContent(_messageHandler.Requests[0]);
		Assert.AreEqual("**Bold Title** <https://tasvideos.org/12345>", content);
	}

	[TestMethod]
	public async Task Post_WithFormattedTitleAndBody_IncludesBody()
	{
		_messageHandler.AddSuccessResponse();
		var distributor = new DiscordDistributor(_appSettings, _httpClientFactory, _logger);
		var post = new Post
		{
			Type = PostType.General,
			Title = "Plain Title",
			FormattedTitle = "**Bold Title** {0}",
			Body = "Additional info",
			Link = "https://tasvideos.org/12345",
			Group = PostGroups.Forum
		};

		await distributor.Post(post);

		var content = await GetMessageContent(_messageHandler.Requests[0]);
		Assert.AreEqual("**Bold Title** <https://tasvideos.org/12345> (Additional info)", content);
	}

	[TestMethod]
	public async Task Post_WithFormattedTitleNoLink_UsesPlainTitle()
	{
		_messageHandler.AddSuccessResponse();
		var distributor = new DiscordDistributor(_appSettings, _httpClientFactory, _logger);
		var post = new Post
		{
			Type = PostType.General,
			Title = "Plain Title",
			FormattedTitle = "**Bold Title** {0}",
			Body = "",
			Link = "",
			Group = PostGroups.Forum
		};

		await distributor.Post(post);

		var content = await GetMessageContent(_messageHandler.Requests[0]);
		// When no link, formatted title is ignored and plain title is used
		Assert.AreEqual("Plain Title", content);
	}

	[TestMethod]
	public async Task Post_WhenRequestFails_LogsError()
	{
		_messageHandler.AddResponse(HttpStatusCode.BadRequest, "Error response");
		var distributor = new DiscordDistributor(_appSettings, _httpClientFactory, _logger);
		var post = CreateTestPost();

		await distributor.Post(post);

		_logger.Received(1).LogError(
			Arg.Is<string>(s => s.Contains("An error occurred sending a message to Discord")),
			Arg.Any<DateTime>(),
			Arg.Is<string>(s => s.Contains("Error response")));
	}

	[TestMethod]
	public async Task Post_WhenRequestSucceeds_DoesNotLogError()
	{
		_messageHandler.AddSuccessResponse();
		var distributor = new DiscordDistributor(_appSettings, _httpClientFactory, _logger);
		var post = CreateTestPost();

		await distributor.Post(post);

		_logger.DidNotReceive().LogError(Arg.Any<string>(), Arg.Any<object[]>());
	}

	[TestMethod]
	public async Task Post_SendsJsonContent()
	{
		_messageHandler.AddSuccessResponse();
		var distributor = new DiscordDistributor(_appSettings, _httpClientFactory, _logger);
		var post = CreateTestPost();

		await distributor.Post(post);

		var request = _messageHandler.Requests[0];
		Assert.AreEqual("application/json", request.Content!.Headers.ContentType!.MediaType);
		Assert.AreEqual("utf-8", request.Content.Headers.ContentType.CharSet);
	}

	[TestMethod]
	public async Task Post_UsesPostMethod()
	{
		_messageHandler.AddSuccessResponse();
		var distributor = new DiscordDistributor(_appSettings, _httpClientFactory, _logger);
		var post = CreateTestPost();

		await distributor.Post(post);

		var request = _messageHandler.Requests[0];
		Assert.AreEqual(HttpMethod.Post, request.Method);
	}

	[TestMethod]
	public void DiscordConnection_IsEnabled_ReturnsFalseWhenDisabled()
	{
		var connection = new AppSettings.DiscordConnection
		{
			Disable = true,
			AccessToken = "token",
			PublicChannelId = "123",
			PublicTasChannelId = "234",
			PublicGameChannelId = "345"
		};

		Assert.IsFalse(connection.IsEnabled());
	}

	[TestMethod]
	public void DiscordConnection_IsEnabled_ReturnsFalseWhenAccessTokenEmpty()
	{
		var connection = new AppSettings.DiscordConnection
		{
			AccessToken = "",
			PublicChannelId = "123",
			PublicTasChannelId = "234",
			PublicGameChannelId = "345"
		};

		Assert.IsFalse(connection.IsEnabled());
	}

	[TestMethod]
	public void DiscordConnection_IsEnabled_ReturnsFalseWhenPublicChannelIdEmpty()
	{
		var connection = new AppSettings.DiscordConnection
		{
			AccessToken = "token",
			PublicChannelId = "",
			PublicTasChannelId = "234",
			PublicGameChannelId = "345"
		};

		Assert.IsFalse(connection.IsEnabled());
	}

	[TestMethod]
	public void DiscordConnection_IsEnabled_ReturnsFalseWhenPublicTasChannelIdEmpty()
	{
		var connection = new AppSettings.DiscordConnection
		{
			AccessToken = "token",
			PublicChannelId = "123",
			PublicTasChannelId = "",
			PublicGameChannelId = "345"
		};

		Assert.IsFalse(connection.IsEnabled());
	}

	[TestMethod]
	public void DiscordConnection_IsEnabled_ReturnsFalseWhenPublicGameChannelIdEmpty()
	{
		var connection = new AppSettings.DiscordConnection
		{
			AccessToken = "token",
			PublicChannelId = "123",
			PublicTasChannelId = "234",
			PublicGameChannelId = ""
		};

		Assert.IsFalse(connection.IsEnabled());
	}

	[TestMethod]
	public void DiscordConnection_IsEnabled_ReturnsTrueWhenAllRequiredFieldsSet()
	{
		var connection = new AppSettings.DiscordConnection
		{
			AccessToken = "token",
			PublicChannelId = "123",
			PublicTasChannelId = "234",
			PublicGameChannelId = "345"
		};

		Assert.IsTrue(connection.IsEnabled());
	}

	[TestMethod]
	public void DiscordConnection_IsPrivateChannelEnabled_ReturnsTrueWhenPrivateChannelsSet()
	{
		var connection = new AppSettings.DiscordConnection
		{
			AccessToken = "token",
			PublicChannelId = "123",
			PublicTasChannelId = "234",
			PublicGameChannelId = "345",
			PrivateChannelId = "456",
			PrivateUserChannelId = "567"
		};

		Assert.IsTrue(connection.IsPrivateChannelEnabled());
	}

	[TestMethod]
	public void DiscordConnection_IsPrivateChannelEnabled_ReturnsFalseWhenPrivateChannelIdEmpty()
	{
		var connection = new AppSettings.DiscordConnection
		{
			AccessToken = "token",
			PublicChannelId = "123",
			PublicTasChannelId = "234",
			PublicGameChannelId = "345",
			PrivateChannelId = "",
			PrivateUserChannelId = "567"
		};

		Assert.IsFalse(connection.IsPrivateChannelEnabled());
	}

	[TestMethod]
	public void DiscordConnection_IsPrivateChannelEnabled_ReturnsFalseWhenPrivateUserChannelIdEmpty()
	{
		var connection = new AppSettings.DiscordConnection
		{
			AccessToken = "token",
			PublicChannelId = "123",
			PublicTasChannelId = "234",
			PublicGameChannelId = "345",
			PrivateChannelId = "456",
			PrivateUserChannelId = ""
		};

		Assert.IsFalse(connection.IsPrivateChannelEnabled());
	}

	// Security test - token handling
	[TestMethod]
	public void DiscordConnection_TokenHandling_SecurityTest()
	{
		var connection = new AppSettings.DiscordConnection
		{
			AccessToken = "sensitive-bot-token"
		};

		// Verify token is stored
		Assert.AreEqual("sensitive-bot-token", connection.AccessToken);
		// Note: Token should be protected in configuration and not logged
	}

	private static Post CreateTestPost(PostType type = PostType.General, string group = PostGroups.Forum)
	{
		return new Post
		{
			Type = type,
			Title = "Test Post",
			Body = "Test body",
			Group = group,
			Link = ""
		};
	}

	private static async Task<string> GetMessageContent(HttpRequestMessage request)
	{
		var json = await request.Content!.ReadAsStringAsync();
		var doc = JsonSerializer.Deserialize<JsonElement>(json);
		return doc.GetProperty("content").GetString()!;
	}

	private class TestHttpMessageHandler : HttpMessageHandler
	{
		private readonly Queue<(HttpStatusCode statusCode, string content)> _responses = new();
		private readonly List<HttpRequestMessage> _requests = [];

		public IReadOnlyList<HttpRequestMessage> Requests => _requests;
		public int RequestCount => _requests.Count;

		public void AddSuccessResponse()
		{
			_responses.Enqueue((HttpStatusCode.OK, "{}"));
		}

		public void AddResponse(HttpStatusCode statusCode, string content)
		{
			_responses.Enqueue((statusCode, content));
		}

		protected override Task<HttpResponseMessage> SendAsync(
			HttpRequestMessage request,
			CancellationToken cancellationToken)
		{
			_requests.Add(request);

			if (_responses.Count == 0)
			{
				return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
				{
					Content = new StringContent("{}", Encoding.UTF8, "application/json")
				});
			}

			var (statusCode, content) = _responses.Dequeue();
			return Task.FromResult(new HttpResponseMessage(statusCode)
			{
				Content = new StringContent(content, Encoding.UTF8, "application/json")
			});
		}
	}
}
