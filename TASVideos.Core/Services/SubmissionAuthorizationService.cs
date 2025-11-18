using TASVideos.Core.Settings;
using static TASVideos.Data.Entity.SubmissionStatus;

namespace TASVideos.Core.Services;

public interface ISubmissionAuthorizationService
{
	/// <summary>
	/// Returns a list of all available statuses a submission could be set to
	/// Based on the user's permissions, submission status and date, and authors.
	/// </summary>
	ICollection<SubmissionStatus> AvailableStatuses(
		SubmissionStatus currentStatus,
		IEnumerable<PermissionTo> userPermissions,
		DateTime submitDate,
		bool isAuthorOrSubmitter,
		bool isJudge,
		bool isPublisher);

	/// <summary>
	/// Returns the number of hours remaining before a submission can be judged
	/// </summary>
	int HoursRemainingForJudging(ISubmissionDisplay submission);
}

internal class SubmissionAuthorizationService(AppSettings settings)
	: ISubmissionAuthorizationService
{
	private readonly int _minimumHoursBeforeJudgment = settings.MinimumHoursBeforeJudgment;

	public ICollection<SubmissionStatus> AvailableStatuses(
		SubmissionStatus currentStatus,
		IEnumerable<PermissionTo> userPermissions,
		DateTime submitDate,
		bool isAuthorOrSubmitter,
		bool isJudge,
		bool isPublisher)
	{
		// Published submissions can not be changed
		if (currentStatus == Published)
		{
			return [Published];
		}

		var perms = userPermissions.ToList();
		if (perms.Contains(PermissionTo.OverrideSubmissionConstraints))
		{
			return [.. Enum.GetValues<SubmissionStatus>().Except([Published])]; // Published status must only be set when being published
		}

		var list = new HashSet<SubmissionStatus>
			{
				currentStatus // The current status must always be in the list
			};

		var canJudge = perms.Contains(PermissionTo.JudgeSubmissions);
		var canPublish = perms.Contains(PermissionTo.PublishMovies);
		var isAfterJudgmentWindow = submitDate < DateTime.UtcNow.AddHours(-_minimumHoursBeforeJudgment);

		if (isJudge && currentStatus == JudgingUnderWay // The judge can set back to new if they claimed the submission and are now opting out
			|| currentStatus == Rejected && isJudge // A judge can revive a rejected submission by setting it to new
			|| currentStatus == Accepted && isJudge // A judge can undo their judgment
			|| currentStatus == PublicationUnderway && isJudge // A judge can undo even if publication underway
			|| isJudge && currentStatus == Delayed // Judges can set delayed -> new
			|| isJudge && currentStatus == NeedsMoreInfo // Judges can set info -> new
			|| (isJudge || isAuthorOrSubmitter) && currentStatus == Cancelled
			|| isJudge && currentStatus == Playground)
		{
			list.Add(New);
		}

		// A judge can claim a new run, unless they are not author or the submitter
		if (new[] { New, JudgingUnderWay, Delayed, NeedsMoreInfo, Accepted, Rejected, PublicationUnderway, Cancelled, Playground }.Contains(currentStatus)
			&& canJudge
			&& !isAuthorOrSubmitter)
		{
			list.Add(JudgingUnderWay);
		}

		// A judge can set a submission to delayed or needs more info so long as they have claimed it
		if (new[] { JudgingUnderWay, Delayed, NeedsMoreInfo, Accepted, PublicationUnderway }.Contains(currentStatus)
			&& isJudge
			&& isAfterJudgmentWindow)
		{
			list.Add(JudgingUnderWay);
			list.Add(Delayed);
			list.Add(NeedsMoreInfo);
		}

		// A judge can deliver a verdict if they have claimed the submission
		if (new[] { JudgingUnderWay, Delayed, NeedsMoreInfo, PublicationUnderway }.Contains(currentStatus)
			&& isJudge
			&& isAfterJudgmentWindow)
		{
			list.Add(Accepted);
			list.Add(Rejected);
		}
		else if (currentStatus is Accepted or PublicationUnderway
			&& isJudge
			&& isAfterJudgmentWindow)
		{
			list.Add(Rejected); // A judge can overrule themselves and reject an accepted movie
		}

		// A publisher can set it to publication underway if it has been accepted
		if (currentStatus == Accepted && canPublish)
		{
			list.Add(PublicationUnderway);
		}

		// A publisher needs to be able to retract their publishing claim
		if (currentStatus == PublicationUnderway && isPublisher)
		{
			list.Add(Accepted);
		}

		// An author or a judge can cancel as long as the submission has not been published
		if ((isJudge || isAuthorOrSubmitter)
			&& new[] { New, JudgingUnderWay, Delayed, NeedsMoreInfo, Accepted, PublicationUnderway }.Contains(currentStatus))
		{
			list.Add(Cancelled);
		}

		if (new[] { JudgingUnderWay, Delayed, NeedsMoreInfo }.Contains(currentStatus)
			&& isJudge
			&& isAfterJudgmentWindow)
		{
			list.Add(Playground);
		}

		return list;
	}

	public int HoursRemainingForJudging(ISubmissionDisplay submission)
	{
		if (!submission.Status.CanBeJudged())
		{
			return 0;
		}

		var diff = (DateTime.UtcNow - submission.Date).TotalHours;
		return _minimumHoursBeforeJudgment - (int)diff;
	}
}
