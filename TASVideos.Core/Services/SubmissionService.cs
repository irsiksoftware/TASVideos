using Microsoft.AspNetCore.Http;
using TASVideos.Core.Services.Wiki;
using TASVideos.Core.Services.Youtube;
using TASVideos.Core.Settings;
using TASVideos.Data.Helpers;
using TASVideos.MovieParsers.Result;
using static TASVideos.Data.Entity.SubmissionStatus;

namespace TASVideos.Core.Services;

public interface ISubmissionService
{
	/// <summary>
	/// Returns whether a submission can be deleted, does not affect the submission
	/// </summary>
	Task<DeleteSubmissionResult> CanDeleteSubmission(int submissionId);

	/// <summary>
	/// Deletes a submission permanently
	/// </summary>
	Task<DeleteSubmissionResult> DeleteSubmission(int submissionId);

	/// <summary>
	/// Returns whether the user has exceeded the submission limit
	/// </summary>
	/// <returns>Next time the user can submit, if the limit has been exceeded, else null</returns>
	Task<DateTime?> ExceededSubmissionLimit(int userId);

	/// <summary>
	/// Returns the total numbers of submissions the given user has submitted
	/// </summary>
	Task<int> GetSubmissionCount(int userId);

	/// <summary>
	/// Updates a submission with the provided data
	/// </summary>
	/// <returns>The result of the update operation</returns>
	Task<UpdateSubmissionResult> UpdateSubmission(UpdateSubmissionRequest request);

	/// <summary>
	/// Creates a new submission
	/// </summary>
	/// <returns>The submission on success or error message on error</returns>
	Task<SubmitResult> Submit(SubmitRequest request);
}

internal class SubmissionService(
	AppSettings settings,
	ApplicationDbContext db,
	IYoutubeSync youtubeSync,
	ITASVideoAgent tva,
	IWikiPages wikiPages,
	IMovieParserService movieParserService,
	IMovieFormatDeprecator deprecator,
	IForumService forumService,
	ITASVideosGrue tasvideosGrue)
	: ISubmissionService
{
	public async Task<DeleteSubmissionResult> CanDeleteSubmission(int submissionId)
	{
		var sub = await db.Submissions
			.Where(s => s.Id == submissionId)
			.Select(s => new
			{
				s.Title,
				IsPublished = s.PublisherId.HasValue
			})
			.SingleOrDefaultAsync();

		if (sub is null)
		{
			return DeleteSubmissionResult.NotFound();
		}

		return sub.IsPublished
			? DeleteSubmissionResult.IsPublished(sub.Title)
			: DeleteSubmissionResult.Success(sub.Title);
	}

	public async Task<DeleteSubmissionResult> DeleteSubmission(int submissionId)
	{
		var submission = await db.Submissions
			.Include(s => s.SubmissionAuthors)
			.Include(s => s.History)
			.SingleOrDefaultAsync(s => s.Id == submissionId);

		if (submission is null)
		{
			return DeleteSubmissionResult.NotFound();
		}

		if (submission.PublisherId.HasValue)
		{
			return DeleteSubmissionResult.IsPublished(submission.Title);
		}

		submission.SubmissionAuthors.Clear();
		submission.History.Clear();
		db.Submissions.Remove(submission);
		if (submission.TopicId.HasValue)
		{
			var topic = await db.ForumTopics
				.Include(t => t.ForumPosts)
				.Include(t => t.Poll)
				.ThenInclude(p => p!.PollOptions)
				.ThenInclude(o => o.Votes)
				.SingleAsync(t => t.Id == submission.TopicId);

			db.ForumPosts.RemoveRange(topic.ForumPosts);
			if (topic.Poll is not null)
			{
				db.ForumPollOptionVotes.RemoveRange(topic.Poll.PollOptions.SelectMany(po => po.Votes));
				db.ForumPollOptions.RemoveRange(topic.Poll.PollOptions);
				db.ForumPolls.Remove(topic.Poll);
			}

			db.ForumTopics.Remove(topic);
		}

		await db.SaveChangesAsync();
		await wikiPages.Delete(WikiHelper.ToSubmissionWikiPageName(submissionId));

		return DeleteSubmissionResult.Success(submission.Title);
	}

	public async Task<DateTime?> ExceededSubmissionLimit(int userId)
	{
		var subs = await db.Submissions
			.Where(s => s.SubmitterId == userId
				&& s.CreateTimestamp > DateTime.UtcNow.AddDays(-settings.SubmissionRate.Days))
			.Select(s => s.CreateTimestamp)
			.ToListAsync();

		if (subs.Count >= settings.SubmissionRate.Submissions)
		{
			return subs.Min().AddDays(settings.SubmissionRate.Days);
		}

		return null;
	}

	public async Task<int> GetSubmissionCount(int userId)
		=> await db.Submissions.CountAsync(s => s.SubmitterId == userId);

	public async Task<UpdateSubmissionResult> UpdateSubmission(UpdateSubmissionRequest request)
	{
		var submission = await db.Submissions
			.Include(s => s.SubmissionAuthors)
			.ThenInclude(sa => sa.Author)
			.Include(s => s.System)
			.Include(s => s.SystemFrameRate)
			.Include(s => s.Game)
			.Include(s => s.GameVersion)
			.Include(s => s.GameGoal)
			.Include(gg => gg.GameGoal)
			.Include(s => s.Topic)
			.Include(s => s.Judge)
			.Include(s => s.Publisher)
			.SingleOrDefaultAsync(s => s.Id == request.SubmissionId);

		if (submission is null)
		{
			return UpdateSubmissionResult.Error("Submission not found");
		}

		if (request.ReplaceMovieFile is not null)
		{
			var (parseResult, movieFileBytes) = await movieParserService.ParseMovieFileOrZip(request.ReplaceMovieFile);
			if (!parseResult.Success)
			{
				return UpdateSubmissionResult.Error("Movie file parsing failed");
			}

			var deprecated = await deprecator.IsDeprecated("." + parseResult.FileExtension);
			if (deprecated)
			{
				return UpdateSubmissionResult.Error($".{parseResult.FileExtension} is no longer submittable");
			}

			var mapResult = await movieParserService.MapParsedResult(parseResult);
			if (mapResult is null)
			{
				return UpdateSubmissionResult.Error($"Unknown system type of {parseResult.SystemCode}");
			}

			submission.MovieStartType = mapResult.MovieStartType;
			submission.Frames = mapResult.Frames;
			submission.RerecordCount = mapResult.RerecordCount;
			submission.MovieExtension = mapResult.MovieExtension;
			submission.System = mapResult.System;
			submission.CycleCount = mapResult.CycleCount;
			submission.Annotations = mapResult.Annotations;
			submission.Warnings = mapResult.Warnings;
			submission.SystemFrameRate = mapResult.SystemFrameRate;

			submission.MovieFile = movieFileBytes;
			submission.SyncedOn = null;
			submission.SyncedByUserId = null;

			if (parseResult.Hashes.Count > 0)
			{
				submission.HashType = parseResult.Hashes.First().Key.ToString();
				submission.Hash = parseResult.Hashes.First().Value;
			}
			else
			{
				submission.HashType = null;
				submission.Hash = null;
			}
		}

		if (SubmissionHelper.JudgeIsClaiming(submission.Status, request.Status))
		{
			submission.Judge = await db.Users.SingleAsync(u => u.UserName == request.UserName);
		}
		else if (SubmissionHelper.JudgeIsUnclaiming(request.Status))
		{
			submission.Judge = null;
		}

		if (SubmissionHelper.PublisherIsClaiming(submission.Status, request.Status))
		{
			submission.Publisher = await db.Users.SingleAsync(u => u.UserName == request.UserName);
		}
		else if (SubmissionHelper.PublisherIsUnclaiming(submission.Status, request.Status))
		{
			submission.Publisher = null;
		}

		bool statusHasChanged = submission.Status != request.Status;
		var previousStatus = submission.Status;
		bool requiresTopicMove = false;
		int? moveTopicToForumId = null;

		if (statusHasChanged)
		{
			db.SubmissionStatusHistory.Add(submission.Id, request.Status);

			if (submission.Topic is not null)
			{
				if (submission.Topic.ForumId != SiteGlobalConstants.PlaygroundForumId
					&& request.Status == Playground)
				{
					requiresTopicMove = true;
					moveTopicToForumId = SiteGlobalConstants.PlaygroundForumId;
				}
				else if (submission.Topic.ForumId != SiteGlobalConstants.WorkbenchForumId
						&& request.Status.IsWorkInProgress())
				{
					requiresTopicMove = true;
					moveTopicToForumId = SiteGlobalConstants.WorkbenchForumId;
				}
			}

			// reject/cancel topic move is handled later with TVG's post
			if (requiresTopicMove && moveTopicToForumId.HasValue && submission.Topic is not null)
			{
				submission.Topic.ForumId = moveTopicToForumId.Value;
				var postsToMove = await db.ForumPosts
					.ForTopic(submission.Topic.Id)
					.ToListAsync();
				foreach (var post in postsToMove)
				{
					post.ForumId = moveTopicToForumId.Value;
				}
			}
		}

		submission.RejectionReasonId = request.Status == Rejected
			? request.RejectionReason
			: null;

		submission.IntendedClass = request.IntendedPublicationClass.HasValue
			? await db.PublicationClasses.FindAsync(request.IntendedPublicationClass.Value)
			: null;

		submission.SubmittedGameVersion = request.GameVersion;
		submission.GameName = request.GameName;
		submission.EmulatorVersion = request.Emulator;
		submission.Branch = request.Goal;
		submission.RomName = request.RomName;
		submission.EncodeEmbedLink = youtubeSync.ConvertToEmbedLink(request.EncodeEmbedLink);
		submission.Status = request.Status;
		submission.AdditionalAuthors = request.ExternalAuthors.NormalizeCsv();

		submission.SubmissionAuthors.Clear();
		submission.SubmissionAuthors.AddRange(await db.Users
			.ToSubmissionAuthors(submission.Id, request.Authors)
			.ToListAsync());

		submission.GenerateTitle();

		if (request.MarkupChanged)
		{
			var revision = new WikiCreateRequest
			{
				PageName = $"{LinkConstants.SubmissionWikiPage}{request.SubmissionId}",
				Markup = request.Markup ?? "",
				MinorEdit = request.MinorEdit,
				RevisionMessage = request.RevisionMessage,
				AuthorId = request.UserId
			};
			_ = await wikiPages.Add(revision) ?? throw new InvalidOperationException("Unable to save wiki revision!");
		}

		await db.SaveChangesAsync();

		var topic = await db.ForumTopics.FindAsync(submission.TopicId);
		if (topic is not null)
		{
			topic.Title = submission.Title;
			await db.SaveChangesAsync();
		}

		if (requiresTopicMove)
		{
			forumService.ClearLatestPostCache();
			forumService.ClearTopicActivityCache();
		}

		if (statusHasChanged && request.Status.IsGrueFood())
		{
			await tasvideosGrue.RejectAndMove(request.SubmissionId);
		}

		return new UpdateSubmissionResult(
			null,
			previousStatus,
			submission.Title);
	}

	public async Task<SubmitResult> Submit(SubmitRequest request)
	{
		try
		{
			using var dbTransaction = await db.BeginTransactionAsync();

			var mapResult = await movieParserService.MapParsedResult(request.ParseResult);
			if (mapResult is null)
			{
				return new FailedSubmitResult($"Unknown system type of {request.ParseResult.SystemCode}");
			}

			var submission = db.Submissions.Add(new Submission
			{
				SubmittedGameVersion = request.GameVersion,
				GameName = request.GameName,
				Branch = request.GoalName?.Trim('"'),
				RomName = request.RomName,
				EmulatorVersion = request.Emulator,
				EncodeEmbedLink = youtubeSync.ConvertToEmbedLink(request.EncodeEmbeddedLink),
				AdditionalAuthors = request.ExternalAuthors.NormalizeCsv(),
				MovieFile = request.MovieFile,
				Submitter = request.Submitter,
				MovieStartType = mapResult.MovieStartType,
				Frames = mapResult.Frames,
				RerecordCount = mapResult.RerecordCount,
				MovieExtension = mapResult.MovieExtension,
				System = mapResult.System,
				CycleCount = mapResult.CycleCount,
				Annotations = mapResult.Annotations,
				Warnings = mapResult.Warnings,
				SystemFrameRate = mapResult.SystemFrameRate,
				IsEventSubmission = request.IsEventSubmission
			}).Entity;

			if (request.ParseResult.Hashes.Count > 0)
			{
				submission.HashType = request.ParseResult.Hashes.First().Key.ToString();
				submission.Hash = request.ParseResult.Hashes.First().Value;
			}

			// Save submission to get ID
			await db.SaveChangesAsync();

			// Create wiki page
			await wikiPages.Add(new WikiCreateRequest
			{
				PageName = LinkConstants.SubmissionWikiPage + submission.Id,
				RevisionMessage = $"Auto-generated from Submission #{submission.Id}",
				Markup = request.Markup,
				AuthorId = request.Submitter.Id
			});

			// Create submission authors
			db.SubmissionAuthors.AddRange(await db.Users
				.ToSubmissionAuthors(submission.Id, request.Authors)
				.ToListAsync());

			// Generate title and create the forum topic
			submission.GenerateTitle();
			submission.TopicId = await tva.PostSubmissionTopic(submission.Id, submission.Title);
			await db.SaveChangesAsync();

			// Commit transaction
			await dbTransaction.CommitAsync();

			// Handle screenshot download and publisher notification (after transaction commit)
			byte[]? screenshotFile = null;
			if (youtubeSync.IsYoutubeUrl(submission.EncodeEmbedLink))
			{
				try
				{
					var youtubeEmbedImageLink = "https://i.ytimg.com/vi/" + submission.EncodeEmbedLink!.Split('/').Last() + "/hqdefault.jpg";
					using var client = new HttpClient();
					var response = await client.GetAsync(youtubeEmbedImageLink);
					if (response.IsSuccessStatusCode)
					{
						screenshotFile = await response.Content.ReadAsByteArrayAsync();
					}
				}
				catch
				{
					// Ignore screenshot download failures
				}
			}

			return new SubmitResult(null, submission.Id, submission.Title, screenshotFile);
		}
		catch (Exception ex)
		{
			return new FailedSubmitResult(ex.ToString());
		}
	}
}
