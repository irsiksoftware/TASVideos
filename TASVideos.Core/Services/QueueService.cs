using Microsoft.AspNetCore.Http;
using TASVideos.MovieParsers.Result;
using static TASVideos.Data.Entity.SubmissionStatus;

namespace TASVideos.Core.Services;

/// <summary>
/// Orchestrator service for queue/submission operations.
/// Delegates to specialized services for specific responsibilities.
/// </summary>
public interface IQueueService
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

	int HoursRemainingForJudging(ISubmissionDisplay submission);

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

	/// <summary>
	/// Publishes a submission by creating a publication with all necessary related data
	/// </summary>
	/// <returns>The publication ID on success or error message on error</returns>
	Task<PublishSubmissionResult> Publish(PublishSubmissionRequest request);

	Task<ObsoletePublicationResult?> GetObsoletePublicationTags(int publicationId);

	/// <summary>
	/// Parses a movie file and returns the parse result along with the movie file bytes
	/// Supports both zip files and individual movie files
	/// </summary>
	Task<(IParseResult ParseResult, byte[] MovieFileBytes)> ParseMovieFileOrZip(IFormFile movieFile);

	/// <summary>
	/// Parses an individual movie file and returns the parse result along with the movie file bytes
	/// Does not support zip files - only individual movie files
	/// </summary>
	Task<(IParseResult ParseResult, byte[] MovieFileBytes)> ParseMovieFile(IFormFile movieFile);

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

/// <summary>
/// Orchestrator service that delegates to specialized services.
/// This service maintains backward compatibility while delegating work to focused services.
/// </summary>
internal class QueueService(
	ISubmissionAuthorizationService authorizationService,
	ISubmissionService submissionService,
	IMovieParserService movieParserService,
	ISubmissionPublicationService publicationService,
	ISubmissionClaimService claimService)
	: IQueueService
{
	// Delegation to SubmissionAuthorizationService
	public ICollection<SubmissionStatus> AvailableStatuses(
		SubmissionStatus currentStatus,
		IEnumerable<PermissionTo> userPermissions,
		DateTime submitDate,
		bool isAuthorOrSubmitter,
		bool isJudge,
		bool isPublisher)
		=> authorizationService.AvailableStatuses(
			currentStatus,
			userPermissions,
			submitDate,
			isAuthorOrSubmitter,
			isJudge,
			isPublisher);

	public int HoursRemainingForJudging(ISubmissionDisplay submission)
		=> authorizationService.HoursRemainingForJudging(submission);

	// Delegation to SubmissionService
	public Task<DeleteSubmissionResult> CanDeleteSubmission(int submissionId)
		=> submissionService.CanDeleteSubmission(submissionId);

	public Task<DeleteSubmissionResult> DeleteSubmission(int submissionId)
		=> submissionService.DeleteSubmission(submissionId);

	public Task<DateTime?> ExceededSubmissionLimit(int userId)
		=> submissionService.ExceededSubmissionLimit(userId);

	public Task<int> GetSubmissionCount(int userId)
		=> submissionService.GetSubmissionCount(userId);

	public Task<UpdateSubmissionResult> UpdateSubmission(UpdateSubmissionRequest request)
		=> submissionService.UpdateSubmission(request);

	public Task<SubmitResult> Submit(SubmitRequest request)
		=> submissionService.Submit(request);

	// Delegation to MovieParserService
	public Task<(IParseResult ParseResult, byte[] MovieFileBytes)> ParseMovieFileOrZip(IFormFile movieFile)
		=> movieParserService.ParseMovieFileOrZip(movieFile);

	public Task<(IParseResult ParseResult, byte[] MovieFileBytes)> ParseMovieFile(IFormFile movieFile)
		=> movieParserService.ParseMovieFile(movieFile);

	// Delegation to SubmissionPublicationService
	public Task<PublishSubmissionResult> Publish(PublishSubmissionRequest request)
		=> publicationService.Publish(request);

	public Task<ObsoletePublicationResult?> GetObsoletePublicationTags(int publicationId)
		=> publicationService.GetObsoletePublicationTags(publicationId);

	// Delegation to SubmissionClaimService
	public Task<ClaimSubmissionResult> ClaimForJudging(int submissionId, int userId, string userName)
		=> claimService.ClaimForJudging(submissionId, userId, userName);

	public Task<ClaimSubmissionResult> ClaimForPublishing(int submissionId, int userId, string userName)
		=> claimService.ClaimForPublishing(submissionId, userId, userName);
}

public interface ISubmissionDisplay
{
	SubmissionStatus Status { get; }
	DateTime Date { get; }
}

public record DeleteSubmissionResult(
	DeleteSubmissionResult.DeleteStatus Status,
	string SubmissionTitle,
	string ErrorMessage)
{
	public enum DeleteStatus { Success, NotFound, NotAllowed }

	public bool True => Status == DeleteStatus.Success;

	internal static DeleteSubmissionResult NotFound() => new(DeleteStatus.NotFound, "", "");

	internal static DeleteSubmissionResult IsPublished(string submissionTitle) => new(
		DeleteStatus.NotAllowed,
		submissionTitle,
		"Cannot delete a submission that is published");

	internal static DeleteSubmissionResult Success(string submissionTitle)
		=> new(DeleteStatus.Success, submissionTitle, "");
}

public record SubmitRequest(
	string GameName,
	string RomName,
	string? GameVersion,
	string? GoalName,
	string? Emulator,
	string? EncodeEmbeddedLink,
	IList<string> Authors,
	string? ExternalAuthors,
	string Markup,
	byte[] MovieFile,
	IParseResult ParseResult,
	User Submitter,
	bool IsEventSubmission = false);

public record SubmitResult(string? ErrorMessage, int Id, string Title, byte[]? Screenshot)
{
	public bool Success => ErrorMessage == null;
}

public record FailedSubmitResult(string ErrorMessage) : SubmitResult(ErrorMessage, -1, "", null);

public record PublishSubmissionRequest(
	int SubmissionId,
	string MovieDescription,
	string MovieFilename,
	string MovieExtension,
	string OnlineWatchingUrl,
	string? AlternateOnlineWatchingUrl,
	string? AlternateOnlineWatchUrlName,
	string? MirrorSiteUrl,
	IFormFile Screenshot,
	string? ScreenshotDescription,
	List<int> SelectedFlags,
	List<int> SelectedTags,
	int? MovieToObsolete,
	int UserId);

public record PublishSubmissionResult(string? ErrorMessage, int PublicationId, string PublicationTitle, string ScreenshotFilePath, byte[] ScreenshotBytes)
{
	public bool Success => ErrorMessage == null;
}

public record FailedPublishSubmissionResult(string ErrorMessage) : PublishSubmissionResult(ErrorMessage, -1, "", "", []);

public record ObsoletePublicationResult(string Title, List<int> Tags, string Markup);

public record ParsedSubmissionData(
	int MovieStartType,
	int Frames,
	int RerecordCount,
	string MovieExtension,
	GameSystem System,
	long? CycleCount,
	string? Annotations,
	string? Warnings,
	GameSystemFrameRate? SystemFrameRate);

public record UpdateSubmissionRequest(
	int SubmissionId,
	string UserName,
	IFormFile? ReplaceMovieFile,
	int? IntendedPublicationClass,
	int? RejectionReason,
	string GameName,
	string? GameVersion,
	string? RomName,
	string? Goal,
	string? Emulator,
	string? EncodeEmbedLink,
	List<string> Authors,
	string? ExternalAuthors,
	SubmissionStatus Status,
	bool MarkupChanged,
	string? Markup,
	string? RevisionMessage,
	bool MinorEdit,
	int UserId);

public record UpdateSubmissionResult(
	string? ErrorMessage,
	SubmissionStatus PreviousStatus,
	string SubmissionTitle)
{
	public bool Success => ErrorMessage == null;

	public static UpdateSubmissionResult Error(string message) => new(message, New, "");
}

public record ClaimSubmissionResult(
	bool Success,
	string? ErrorMessage,
	string SubmissionTitle)
{
	public static ClaimSubmissionResult Error(string errorMessage) => new(false, errorMessage, "");
	public static ClaimSubmissionResult Successful(string submissionTitle) => new(true, null, submissionTitle);
}
