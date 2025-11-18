using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TASVideos.Core.Services.ExternalMediaPublisher;
using TASVideos.Core.Services.ExternalMediaPublisher.Distributors;
using TASVideos.Core.Settings;

namespace TASVideos.Core.Tests.Services.ExternalMediaPublisher.Distributors;

[TestClass]
public class BlueskyDistributorTests
{
	private readonly IHttpClientFactory _httpClientFactory = Substitute.For<IHttpClientFactory>();
	private readonly ILogger<BlueskyDistributor> _logger = Substitute.For<ILogger<BlueskyDistributor>>();
	private readonly TestHttpMessageHandler _messageHandler = new();
	private readonly AppSettings _appSettings = new();

	[TestInitialize]
	public void Setup()
	{
		var httpClient = new HttpClient(_messageHandler)
		{
			BaseAddress = new Uri("https://bsky.social/xrpc/")
		};
		_httpClientFactory.CreateClient(HttpClients.Bluesky).Returns(httpClient);

		_appSettings.Bluesky = new AppSettings.BlueskyConnection
		{
			Identifier = "test.user",
			Password = "test-password"
		};
	}

	[TestMethod]
	public void Types_ReturnsOnlyAnnouncement()
	{
		var distributor = new BlueskyDistributor(_appSettings, _httpClientFactory, _logger);

		var result = distributor.Types.ToList();

		Assert.AreEqual(1, result.Count);
		Assert.AreEqual(PostType.Announcement, result.Single());
	}

	[TestMethod]
	public async Task Post_WhenDisabled_DoesNothing()
	{
		_appSettings.Bluesky.Disable = true;
		var distributor = new BlueskyDistributor(_appSettings, _httpClientFactory, _logger);
		var post = CreateTestPost();

		await distributor.Post(post);

		Assert.AreEqual(0, _messageHandler.RequestCount);
	}

	[TestMethod]
	public async Task Post_WhenIdentifierIsEmpty_DoesNothing()
	{
		_appSettings.Bluesky.Identifier = "";
		var distributor = new BlueskyDistributor(_appSettings, _httpClientFactory, _logger);
		var post = CreateTestPost();

		await distributor.Post(post);

		Assert.AreEqual(0, _messageHandler.RequestCount);
	}

	[TestMethod]
	public async Task Post_WhenPasswordIsEmpty_DoesNothing()
	{
		_appSettings.Bluesky.Password = "";
		var distributor = new BlueskyDistributor(_appSettings, _httpClientFactory, _logger);
		var post = CreateTestPost();

		await distributor.Post(post);

		Assert.AreEqual(0, _messageHandler.RequestCount);
	}

	[TestMethod]
	public async Task Post_ResetsAuthorizationBeforeCreatingSession()
	{
		_messageHandler.AddResponse("com.atproto.server.createSession", new
		{
			accessJwt = "test-jwt-token",
			did = "did:plc:test123"
		});
		_messageHandler.AddResponse("com.atproto.repo.createRecord", new { uri = "at://test" });

		var distributor = new BlueskyDistributor(_appSettings, _httpClientFactory, _logger);
		var post = CreateTestPost();

		await distributor.Post(post);

		var firstRequest = _messageHandler.Requests[0];
		Assert.IsNull(firstRequest.Headers.Authorization);
	}

	[TestMethod]
	public async Task Post_SendsCorrectSessionCreationRequest()
	{
		_messageHandler.AddResponse("com.atproto.server.createSession", new
		{
			accessJwt = "test-jwt-token",
			did = "did:plc:test123"
		});
		_messageHandler.AddResponse("com.atproto.repo.createRecord", new { uri = "at://test" });

		var distributor = new BlueskyDistributor(_appSettings, _httpClientFactory, _logger);
		var post = CreateTestPost();

		await distributor.Post(post);

		var sessionRequest = _messageHandler.Requests[0];
		Assert.IsTrue(sessionRequest.RequestUri?.ToString().Contains("com.atproto.server.createSession"));

		var content = await sessionRequest.Content!.ReadAsStringAsync();
		var sessionData = JsonSerializer.Deserialize<JsonElement>(content);

		Assert.AreEqual("test.user", sessionData.GetProperty("identifier").GetString());
		Assert.AreEqual("test-password", sessionData.GetProperty("password").GetString());
	}

	[TestMethod]
	public async Task Post_WhenSessionCreationFails_LogsErrorAndReturns()
	{
		_messageHandler.AddResponse("com.atproto.server.createSession", new { }, HttpStatusCode.Unauthorized);

		var distributor = new BlueskyDistributor(_appSettings, _httpClientFactory, _logger);
		var post = CreateTestPost();

		await distributor.Post(post);

		Assert.AreEqual(1, _messageHandler.RequestCount);
		_logger.Received(1).LogError("Failed to create Bluesky session");
	}

	[TestMethod]
	public async Task Post_SetsBearerTokenAfterSuccessfulSession()
	{
		_messageHandler.AddResponse("com.atproto.server.createSession", new
		{
			accessJwt = "test-jwt-token-12345",
			did = "did:plc:test123"
		});
		_messageHandler.AddResponse("com.atproto.repo.createRecord", new { uri = "at://test" });

		var distributor = new BlueskyDistributor(_appSettings, _httpClientFactory, _logger);
		var post = CreateTestPost();

		await distributor.Post(post);

		var createRecordRequest = _messageHandler.Requests[1];
		Assert.IsNotNull(createRecordRequest.Headers.Authorization);
		Assert.AreEqual("Bearer", createRecordRequest.Headers.Authorization.Scheme);
		Assert.AreEqual("test-jwt-token-12345", createRecordRequest.Headers.Authorization.Parameter);
	}

	[TestMethod]
	public async Task Post_WithoutImage_CreatesPostWithoutEmbed()
	{
		_messageHandler.AddResponse("com.atproto.server.createSession", new
		{
			accessJwt = "test-jwt",
			did = "did:plc:test"
		});
		_messageHandler.AddResponse("com.atproto.repo.createRecord", new { uri = "at://test" });

		var distributor = new BlueskyDistributor(_appSettings, _httpClientFactory, _logger);
		var post = CreateTestPost(imageData: null);

		await distributor.Post(post);

		Assert.AreEqual(2, _messageHandler.RequestCount); // Session + CreateRecord only

		var createRecordRequest = _messageHandler.Requests[1];
		var content = await createRecordRequest.Content!.ReadAsStringAsync();
		var recordData = JsonSerializer.Deserialize<JsonElement>(content);

		var hasEmbed = recordData.GetProperty("record").TryGetProperty("embed", out _);
		Assert.IsFalse(hasEmbed);
	}

	[TestMethod]
	public async Task Post_WithImage_UploadsBlobAndIncludesInPost()
	{
		_messageHandler.AddResponse("com.atproto.server.createSession", new
		{
			accessJwt = "test-jwt",
			did = "did:plc:test"
		});
		_messageHandler.AddResponse("com.atproto.repo.uploadBlob", new
		{
			blob = new
			{
				type = "blob",
				@ref = new { link = "bafytest123" },
				mimeType = "image/png",
				size = 12345
			}
		});
		_messageHandler.AddResponse("com.atproto.repo.createRecord", new { uri = "at://test" });

		var distributor = new BlueskyDistributor(_appSettings, _httpClientFactory, _logger);
		var imageData = Encoding.UTF8.GetBytes("fake-image-data");
		var post = CreateTestPost(imageData: imageData, imageMimeType: "image/png", imageWidth: 800, imageHeight: 600);

		await distributor.Post(post);

		Assert.AreEqual(3, _messageHandler.RequestCount); // Session + UploadBlob + CreateRecord

		var uploadBlobRequest = _messageHandler.Requests[1];
		Assert.IsTrue(uploadBlobRequest.RequestUri?.ToString().Contains("com.atproto.repo.uploadBlob"));
		Assert.AreEqual("image/png", uploadBlobRequest.Content!.Headers.ContentType!.MediaType);

		var blobContent = await uploadBlobRequest.Content.ReadAsByteArrayAsync();
		CollectionAssert.AreEqual(imageData, blobContent);
	}

	[TestMethod]
	public async Task Post_WithImage_IncludesEmbedInRecord()
	{
		_messageHandler.AddResponse("com.atproto.server.createSession", new
		{
			accessJwt = "test-jwt",
			did = "did:plc:test"
		});
		_messageHandler.AddResponse("com.atproto.repo.uploadBlob", new
		{
			blob = new
			{
				type = "blob",
				@ref = new { link = "bafytest123" },
				mimeType = "image/png",
				size = 12345
			}
		});
		_messageHandler.AddResponse("com.atproto.repo.createRecord", new { uri = "at://test" });

		var distributor = new BlueskyDistributor(_appSettings, _httpClientFactory, _logger);
		var post = CreateTestPost(imageData: [1, 2, 3], imageMimeType: "image/jpeg", imageWidth: 1920, imageHeight: 1080);

		await distributor.Post(post);

		var createRecordRequest = _messageHandler.Requests[2];
		var content = await createRecordRequest.Content!.ReadAsStringAsync();
		var recordData = JsonSerializer.Deserialize<JsonElement>(content);

		var embed = recordData.GetProperty("record").GetProperty("embed");
		Assert.AreEqual("app.bsky.embed.images", embed.GetProperty("$type").GetString());

		var images = embed.GetProperty("images");
		Assert.AreEqual(1, images.GetArrayLength());

		var image = images[0];
		Assert.AreEqual("", image.GetProperty("alt").GetString());
		Assert.AreEqual(1920, image.GetProperty("aspectRatio").GetProperty("width").GetInt32());
		Assert.AreEqual(1080, image.GetProperty("aspectRatio").GetProperty("height").GetInt32());
	}

	[TestMethod]
	public async Task Post_WhenBlobUploadFails_ContinuesWithoutEmbed()
	{
		_messageHandler.AddResponse("com.atproto.server.createSession", new
		{
			accessJwt = "test-jwt",
			did = "did:plc:test"
		});
		_messageHandler.AddResponse("com.atproto.repo.uploadBlob", new { }, HttpStatusCode.InternalServerError);
		_messageHandler.AddResponse("com.atproto.repo.createRecord", new { uri = "at://test" });

		var distributor = new BlueskyDistributor(_appSettings, _httpClientFactory, _logger);
		var post = CreateTestPost(imageData: [1, 2, 3], imageMimeType: "image/png");

		await distributor.Post(post);

		Assert.AreEqual(3, _messageHandler.RequestCount);

		var createRecordRequest = _messageHandler.Requests[2];
		var content = await createRecordRequest.Content!.ReadAsStringAsync();
		var recordData = JsonSerializer.Deserialize<JsonElement>(content);

		var hasEmbed = recordData.GetProperty("record").TryGetProperty("embed", out _);
		Assert.IsFalse(hasEmbed);
	}

	[TestMethod]
	public async Task Post_SubmissionGroup_UsesTitleAsBody()
	{
		_messageHandler.AddResponse("com.atproto.server.createSession", new
		{
			accessJwt = "test-jwt",
			did = "did:plc:test"
		});
		_messageHandler.AddResponse("com.atproto.repo.createRecord", new { uri = "at://test" });

		var distributor = new BlueskyDistributor(_appSettings, _httpClientFactory, _logger);
		var post = new Post
		{
			Type = PostType.Announcement,
			Title = "New Submission: Super Mario Bros",
			Body = "Different body text",
			Group = PostGroups.Submission,
			Link = ""
		};

		await distributor.Post(post);

		var createRecordRequest = _messageHandler.Requests[1];
		var content = await createRecordRequest.Content!.ReadAsStringAsync();
		var recordData = JsonSerializer.Deserialize<JsonElement>(content);

		var text = recordData.GetProperty("record").GetProperty("text").GetString();
		Assert.AreEqual("New Submission: Super Mario Bros", text);
	}

	[TestMethod]
	public async Task Post_PublicationGroup_UsesTitleAsBody()
	{
		_messageHandler.AddResponse("com.atproto.server.createSession", new
		{
			accessJwt = "test-jwt",
			did = "did:plc:test"
		});
		_messageHandler.AddResponse("com.atproto.repo.createRecord", new { uri = "at://test" });

		var distributor = new BlueskyDistributor(_appSettings, _httpClientFactory, _logger);
		var post = new Post
		{
			Type = PostType.Announcement,
			Title = "New Publication: Zelda OoT",
			Body = "Different body text",
			Group = PostGroups.Publication,
			Link = ""
		};

		await distributor.Post(post);

		var createRecordRequest = _messageHandler.Requests[1];
		var content = await createRecordRequest.Content!.ReadAsStringAsync();
		var recordData = JsonSerializer.Deserialize<JsonElement>(content);

		var text = recordData.GetProperty("record").GetProperty("text").GetString();
		Assert.AreEqual("New Publication: Zelda OoT", text);
	}

	[TestMethod]
	public async Task Post_OtherGroup_UsesBodyAsBody()
	{
		_messageHandler.AddResponse("com.atproto.server.createSession", new
		{
			accessJwt = "test-jwt",
			did = "did:plc:test"
		});
		_messageHandler.AddResponse("com.atproto.repo.createRecord", new { uri = "at://test" });

		var distributor = new BlueskyDistributor(_appSettings, _httpClientFactory, _logger);
		var post = new Post
		{
			Type = PostType.Announcement,
			Title = "Title text",
			Body = "Body text for the post",
			Group = PostGroups.Forum,
			Link = ""
		};

		await distributor.Post(post);

		var createRecordRequest = _messageHandler.Requests[1];
		var content = await createRecordRequest.Content!.ReadAsStringAsync();
		var recordData = JsonSerializer.Deserialize<JsonElement>(content);

		var text = recordData.GetProperty("record").GetProperty("text").GetString();
		Assert.AreEqual("Body text for the post", text);
	}

	[TestMethod]
	public async Task Post_WithLink_AddsLinkToBodyAndCreatesFacet()
	{
		_messageHandler.AddResponse("com.atproto.server.createSession", new
		{
			accessJwt = "test-jwt",
			did = "did:plc:test"
		});
		_messageHandler.AddResponse("com.atproto.repo.createRecord", new { uri = "at://test" });

		var distributor = new BlueskyDistributor(_appSettings, _httpClientFactory, _logger);
		var post = new Post
		{
			Type = PostType.Announcement,
			Body = "Check out this awesome TAS!",
			Group = PostGroups.Forum,
			Link = "https://tasvideos.org/12345"
		};

		await distributor.Post(post);

		var createRecordRequest = _messageHandler.Requests[1];
		var content = await createRecordRequest.Content!.ReadAsStringAsync();
		var recordData = JsonSerializer.Deserialize<JsonElement>(content);

		var record = recordData.GetProperty("record");
		var text = record.GetProperty("text").GetString()!;

		Assert.IsTrue(text.Contains("Check out this awesome TAS!"));
		Assert.IsTrue(text.Contains("https://tasvideos.org/12345"));

		var facets = record.GetProperty("facets");
		Assert.AreEqual(1, facets.GetArrayLength());

		var facet = facets[0];
		var index = facet.GetProperty("index");
		var byteStart = index.GetProperty("byteStart").GetInt32();
		var byteEnd = index.GetProperty("byteEnd").GetInt32();

		// Verify byte positions are calculated correctly for UTF-8
		var bodyBytes = Encoding.UTF8.GetByteCount("Check out this awesome TAS!\n");
		Assert.AreEqual(bodyBytes, byteStart);

		var features = facet.GetProperty("features");
		Assert.AreEqual(1, features.GetArrayLength());
		Assert.AreEqual("app.bsky.richtext.facet#link", features[0].GetProperty("$type").GetString());
		Assert.AreEqual("https://tasvideos.org/12345", features[0].GetProperty("uri").GetString());
	}

	[TestMethod]
	public async Task Post_LongBodyWithLink_TruncatesBodyTo300Chars()
	{
		_messageHandler.AddResponse("com.atproto.server.createSession", new
		{
			accessJwt = "test-jwt",
			did = "did:plc:test"
		});
		_messageHandler.AddResponse("com.atproto.repo.createRecord", new { uri = "at://test" });

		var distributor = new BlueskyDistributor(_appSettings, _httpClientFactory, _logger);
		var longBody = new string('A', 400);
		var link = "https://tasvideos.org/12345";
		var post = new Post
		{
			Type = PostType.Announcement,
			Body = longBody,
			Group = PostGroups.Forum,
			Link = link
		};

		await distributor.Post(post);

		var createRecordRequest = _messageHandler.Requests[1];
		var content = await createRecordRequest.Content!.ReadAsStringAsync();
		var recordData = JsonSerializer.Deserialize<JsonElement>(content);

		var text = recordData.GetProperty("record").GetProperty("text").GetString()!;

		// Should be truncated to fit within 300 chars including link and newline
		Assert.IsTrue(text.Length <= 300);
		Assert.IsTrue(text.Contains("..."));
		Assert.IsTrue(text.EndsWith(link));
	}

	[TestMethod]
	public async Task Post_LongBodyWithoutLink_TruncatesTo300Chars()
	{
		_messageHandler.AddResponse("com.atproto.server.createSession", new
		{
			accessJwt = "test-jwt",
			did = "did:plc:test"
		});
		_messageHandler.AddResponse("com.atproto.repo.createRecord", new { uri = "at://test" });

		var distributor = new BlueskyDistributor(_appSettings, _httpClientFactory, _logger);
		var longBody = new string('B', 400);
		var post = new Post
		{
			Type = PostType.Announcement,
			Body = longBody,
			Group = PostGroups.Forum,
			Link = ""
		};

		await distributor.Post(post);

		var createRecordRequest = _messageHandler.Requests[1];
		var content = await createRecordRequest.Content!.ReadAsStringAsync();
		var recordData = JsonSerializer.Deserialize<JsonElement>(content);

		var text = recordData.GetProperty("record").GetProperty("text").GetString()!;

		Assert.IsTrue(text.Length <= 300);
		Assert.IsTrue(text.EndsWith("..."));
	}

	[TestMethod]
	public async Task Post_WhenCreateRecordFails_LogsError()
	{
		_messageHandler.AddResponse("com.atproto.server.createSession", new
		{
			accessJwt = "test-jwt",
			did = "did:plc:test"
		});
		_messageHandler.AddResponse("com.atproto.repo.createRecord", new { }, HttpStatusCode.BadRequest);

		var distributor = new BlueskyDistributor(_appSettings, _httpClientFactory, _logger);
		var post = CreateTestPost();

		await distributor.Post(post);

		_logger.Received(1).LogError("Failed to create Bluesky post");
	}

	[TestMethod]
	public async Task Post_SetsCorrectRecordProperties()
	{
		_messageHandler.AddResponse("com.atproto.server.createSession", new
		{
			accessJwt = "test-jwt",
			did = "did:plc:test123"
		});
		_messageHandler.AddResponse("com.atproto.repo.createRecord", new { uri = "at://test" });

		var distributor = new BlueskyDistributor(_appSettings, _httpClientFactory, _logger);
		var post = CreateTestPost();

		await distributor.Post(post);

		var createRecordRequest = _messageHandler.Requests[1];
		var content = await createRecordRequest.Content!.ReadAsStringAsync();
		var recordData = JsonSerializer.Deserialize<JsonElement>(content);

		Assert.AreEqual("did:plc:test123", recordData.GetProperty("repo").GetString());
		Assert.AreEqual("app.bsky.feed.post", recordData.GetProperty("collection").GetString());

		var record = recordData.GetProperty("record");
		Assert.AreEqual("app.bsky.feed.post", record.GetProperty("$type").GetString());
		Assert.IsTrue(record.TryGetProperty("text", out _));
		Assert.IsTrue(record.TryGetProperty("createdAt", out _));

		var langs = record.GetProperty("langs");
		Assert.AreEqual(1, langs.GetArrayLength());
		Assert.AreEqual("en-US", langs[0].GetString());
	}

	[TestMethod]
	public async Task Post_WithUnicodeCharacters_CalculatesByteOffsetsCorrectly()
	{
		_messageHandler.AddResponse("com.atproto.server.createSession", new
		{
			accessJwt = "test-jwt",
			did = "did:plc:test"
		});
		_messageHandler.AddResponse("com.atproto.repo.createRecord", new { uri = "at://test" });

		var distributor = new BlueskyDistributor(_appSettings, _httpClientFactory, _logger);
		var post = new Post
		{
			Type = PostType.Announcement,
			Body = "🎮 Check out this speedrun! 🏃",
			Group = PostGroups.Forum,
			Link = "https://tasvideos.org/12345"
		};

		await distributor.Post(post);

		var createRecordRequest = _messageHandler.Requests[1];
		var content = await createRecordRequest.Content!.ReadAsStringAsync();
		var recordData = JsonSerializer.Deserialize<JsonElement>(content);

		var record = recordData.GetProperty("record");
		var text = record.GetProperty("text").GetString()!;
		var facets = record.GetProperty("facets");

		var facet = facets[0];
		var index = facet.GetProperty("index");
		var byteStart = index.GetProperty("byteStart").GetInt32();
		var byteEnd = index.GetProperty("byteEnd").GetInt32();

		// Verify that byte offsets account for multi-byte UTF-8 characters (emojis)
		var textBeforeLink = text.Substring(0, text.LastIndexOf("https://"));
		var expectedByteStart = Encoding.UTF8.GetByteCount(textBeforeLink);
		Assert.AreEqual(expectedByteStart, byteStart);

		var linkBytes = Encoding.UTF8.GetByteCount("https://tasvideos.org/12345");
		Assert.AreEqual(expectedByteStart + linkBytes, byteEnd);
	}

	private static Post CreateTestPost(
		byte[]? imageData = null,
		string? imageMimeType = null,
		int? imageWidth = null,
		int? imageHeight = null)
	{
		return new Post
		{
			Type = PostType.Announcement,
			Title = "Test Post",
			Body = "Test body content",
			Group = PostGroups.Forum,
			Link = "",
			ImageData = imageData,
			ImageMimeType = imageMimeType,
			ImageWidth = imageWidth,
			ImageHeight = imageHeight
		};
	}

	private class TestHttpMessageHandler : HttpMessageHandler
	{
		private readonly Queue<(string endpoint, object response, HttpStatusCode statusCode)> _responses = new();
		private readonly List<HttpRequestMessage> _requests = [];

		public IReadOnlyList<HttpRequestMessage> Requests => _requests;
		public int RequestCount => _requests.Count;

		public void AddResponse(string endpoint, object response, HttpStatusCode statusCode = HttpStatusCode.OK)
		{
			_responses.Enqueue((endpoint, response, statusCode));
		}

		protected override async Task<HttpResponseMessage> SendAsync(
			HttpRequestMessage request,
			CancellationToken cancellationToken)
		{
			_requests.Add(request);

			if (_responses.Count == 0)
			{
				return new HttpResponseMessage(HttpStatusCode.NotFound);
			}

			var (endpoint, response, statusCode) = _responses.Dequeue();

			var json = JsonSerializer.Serialize(response);
			return new HttpResponseMessage(statusCode)
			{
				Content = new StringContent(json, Encoding.UTF8, "application/json")
			};
		}
	}
}
