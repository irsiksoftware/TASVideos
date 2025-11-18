using TASVideos.Core.Services.Wiki;
using static TASVideos.Data.Entity.SubmissionStatus;

namespace TASVideos.Core.Services;

public interface ISubmissionClaimService
{
	/// <summary>
	/// Claims a submission for judging by the specified user
	/// </summary>
	/// <param name="submissionId">The ID of the submission to claim</param>
	/// <param name="userId">The ID of the user claiming the submission</param>
	/// <param name="userName">The username of the user claiming the submission</param>
	/// <returns>The result of the claim operation</returns>
	Task<ClaimSubmissionResult> ClaimForJudging(int submissionId, int userId, string userName);

	/// <summary>
	/// Claims a submission for publishing by the specified user
	/// </summary>
	/// <param name="submissionId">The ID of the submission to claim</param>
	/// <param name="userId">The ID of the user claiming the submission</param>
	/// <param name="userName">The username of the user claiming the submission</param>
	/// <returns>The result of the claim operation</returns>
	Task<ClaimSubmissionResult> ClaimForPublishing(int submissionId, int userId, string userName);
}

internal class SubmissionClaimService(
	ApplicationDbContext db,
	IWikiPages wikiPages,
	ITopicWatcher topicWatcher)
	: ISubmissionClaimService
{
	public Task<ClaimSubmissionResult> ClaimForJudging(int submissionId, int userId, string userName)
		=> ClaimSubmission(
			submissionId,
			userId,
			userName,
			requiredStatus: New,
			targetStatus: JudgingUnderWay,
			assignToJudge: true,
			wikiMessage: "Claiming for judging.",
			revisionMessage: "Claimed for judging",
			watchTopic: true);

	public Task<ClaimSubmissionResult> ClaimForPublishing(int submissionId, int userId, string userName)
		=> ClaimSubmission(
			submissionId,
			userId,
			userName,
			requiredStatus: Accepted,
			targetStatus: PublicationUnderway,
			assignToJudge: false,
			wikiMessage: "Processing...",
			revisionMessage: "Claimed for publication",
			watchTopic: false);

	private async Task<ClaimSubmissionResult> ClaimSubmission(
		int submissionId,
		int userId,
		string userName,
		SubmissionStatus requiredStatus,
		SubmissionStatus targetStatus,
		bool assignToJudge,
		string wikiMessage,
		string revisionMessage,
		bool watchTopic)
	{
		var submission = await db.Submissions.FindAsync(submissionId);
		if (submission is null)
		{
			return ClaimSubmissionResult.Error("Submission not found");
		}

		if (submission.Status != requiredStatus)
		{
			return ClaimSubmissionResult.Error("Submission can not be claimed");
		}

		var submissionPage = (await wikiPages.SubmissionPage(submissionId))!;
		db.SubmissionStatusHistory.Add(submission.Id, submission.Status);

		submission.Status = targetStatus;
		if (assignToJudge)
		{
			submission.JudgeId = userId;
		}
		else
		{
			submission.PublisherId = userId;
		}

		await wikiPages.Add(new WikiCreateRequest
		{
			PageName = submissionPage.PageName,
			Markup = submissionPage.Markup + $"\n----\n[user:{userName}]: {wikiMessage}",
			RevisionMessage = revisionMessage,
			AuthorId = userId
		});

		if (watchTopic && submission.TopicId.HasValue)
		{
			await topicWatcher.WatchTopic(submission.TopicId.Value, userId, true);
		}

		var result = await db.TrySaveChanges();
		return result.IsSuccess()
			? ClaimSubmissionResult.Successful(submission.Title)
			: ClaimSubmissionResult.Error("Unable to claim");
	}
}
