using Microsoft.AspNetCore.Http;
using TASVideos.Core.Services.Wiki;
using TASVideos.Core.Services.Youtube;
using TASVideos.Data.Helpers;
using static TASVideos.Data.Entity.SubmissionStatus;

namespace TASVideos.Core.Services;

public interface ISubmissionPublicationService
{
	/// <summary>
	/// Publishes a submission by creating a publication with all necessary related data
	/// </summary>
	/// <returns>The publication ID on success or error message on error</returns>
	Task<PublishSubmissionResult> Publish(PublishSubmissionRequest request);

	/// <summary>
	/// Gets the tags and markup for a publication to be obsoleted
	/// </summary>
	Task<ObsoletePublicationResult?> GetObsoletePublicationTags(int publicationId);

	/// <summary>
	/// Marks a publication as obsolete by another publication
	/// </summary>
	Task<bool> ObsoleteWith(int publicationToObsolete, int obsoletingPublicationId);
}

internal class SubmissionPublicationService(
	ApplicationDbContext db,
	IYoutubeSync youtubeSync,
	ITASVideoAgent tva,
	IWikiPages wikiPages,
	IMediaFileUploader uploader,
	IFileService fileService,
	IUserManager userManager)
	: ISubmissionPublicationService
{
	public async Task<PublishSubmissionResult> Publish(PublishSubmissionRequest request)
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
			.Include(s => s.IntendedClass)
			.SingleOrDefaultAsync(s => s.Id == request.SubmissionId);

		if (submission is null || !submission.CanPublish())
		{
			return new FailedPublishSubmissionResult("Submission not found or cannot be published");
		}

		var movieFileName = request.MovieFilename + "." + request.MovieExtension;
		if (await db.Publications.AnyAsync(p => p.MovieFileName == movieFileName))
		{
			return new FailedPublishSubmissionResult($"Movie filename {movieFileName} already exists");
		}

		int? publicationToObsolete = null;
		if (request.MovieToObsolete.HasValue)
		{
			publicationToObsolete = (await db.Publications
				.SingleOrDefaultAsync(p => p.Id == request.MovieToObsolete.Value))?.Id;
			if (publicationToObsolete is null)
			{
				return new FailedPublishSubmissionResult("Publication to obsolete does not exist");
			}
		}

		try
		{
			using var dbTransaction = await db.BeginTransactionAsync();

			var publication = new Publication
			{
				PublicationClassId = submission.IntendedClass!.Id,
				SystemId = submission.System!.Id,
				SystemFrameRateId = submission.SystemFrameRate!.Id,
				GameId = submission.Game!.Id,
				GameVersionId = submission.GameVersion!.Id,
				EmulatorVersion = submission.EmulatorVersion,
				Frames = submission.Frames,
				RerecordCount = submission.RerecordCount,
				MovieFileName = movieFileName,
				AdditionalAuthors = submission.AdditionalAuthors,
				Submission = submission,
				MovieFile = await fileService.CopyZip(submission.MovieFile, movieFileName),
				GameGoalId = submission.GameGoalId
			};

			publication.PublicationUrls.AddStreaming(request.OnlineWatchingUrl, "");
			if (!string.IsNullOrWhiteSpace(request.MirrorSiteUrl))
			{
				publication.PublicationUrls.AddMirror(request.MirrorSiteUrl);
			}

			if (!string.IsNullOrWhiteSpace(request.AlternateOnlineWatchingUrl))
			{
				publication.PublicationUrls.AddStreaming(request.AlternateOnlineWatchingUrl, request.AlternateOnlineWatchUrlName);
			}

			publication.Authors.CopyFromSubmission(submission.SubmissionAuthors);
			publication.PublicationFlags.AddFlags(request.SelectedFlags);
			publication.PublicationTags.AddTags(request.SelectedTags);

			db.Publications.Add(publication);

			await db.SaveChangesAsync(); // Need an ID for the Title
			publication.Title = publication.GenerateTitle();

			var (screenshotPath, screenshotBytes) = await uploader.UploadScreenshot(publication.Id, request.Screenshot, request.ScreenshotDescription);

			var addedWikiPage = await wikiPages.Add(new WikiCreateRequest
			{
				RevisionMessage = $"Auto-generated from Movie #{publication.Id}",
				PageName = WikiHelper.ToPublicationWikiPageName(publication.Id),
				Markup = request.MovieDescription,
				AuthorId = request.UserId
			});

			submission.Status = Published;
			db.SubmissionStatusHistory.Add(request.SubmissionId, Published);

			if (publicationToObsolete.HasValue)
			{
				await ObsoleteWith(publicationToObsolete.Value, publication.Id);
			}

			await userManager.AssignAutoAssignableRolesByPublication(publication.Authors.Select(pa => pa.UserId), publication.Title);
			await tva.PostSubmissionPublished(request.SubmissionId, publication.Id);
			await dbTransaction.CommitAsync();

			if (youtubeSync.IsYoutubeUrl(request.OnlineWatchingUrl))
			{
				var video = new YoutubeVideo(
					publication.Id,
					publication.CreateTimestamp,
					request.OnlineWatchingUrl,
					"",
					publication.Title,
					addedWikiPage!,
					submission.System!.Code,
					publication.Authors.OrderBy(pa => pa.Ordinal).Select(pa => pa.Author!.UserName),
					null);
				await youtubeSync.SyncYouTubeVideo(video);
			}

			if (youtubeSync.IsYoutubeUrl(request.AlternateOnlineWatchingUrl))
			{
				var video = new YoutubeVideo(
					publication.Id,
					publication.CreateTimestamp,
					request.AlternateOnlineWatchingUrl ?? "",
					request.AlternateOnlineWatchUrlName,
					publication.Title,
					addedWikiPage!,
					submission.System!.Code,
					publication.Authors.OrderBy(pa => pa.Ordinal).Select(pa => pa.Author!.UserName),
					null);
				await youtubeSync.SyncYouTubeVideo(video);
			}

			return new PublishSubmissionResult(null, publication.Id, publication.Title, screenshotPath, screenshotBytes);
		}
		catch (Exception ex)
		{
			return new FailedPublishSubmissionResult(ex.ToString());
		}
	}

	public async Task<ObsoletePublicationResult?> GetObsoletePublicationTags(int publicationId)
	{
		var pub = await db.Publications
			.Where(p => p.Id == publicationId)
			.Select(p => new { p.Title, Tags = p.PublicationTags.Select(pt => pt.TagId).ToList() })
			.SingleOrDefaultAsync();

		if (pub is null)
		{
			return null;
		}

		var page = await wikiPages.PublicationPage(publicationId);
		return new ObsoletePublicationResult(pub.Title, pub.Tags, page!.Markup);
	}

	public async Task<bool> ObsoleteWith(int publicationToObsolete, int obsoletingPublicationId)
	{
		var toObsolete = await db.Publications
			.Include(p => p.PublicationUrls)
			.Include(p => p.System)
			.Include(p => p.Game)
			.Include(p => p.Authors)
			.ThenInclude(pa => pa.Author)
			.SingleOrDefaultAsync(p => p.Id == publicationToObsolete);

		if (toObsolete is null)
		{
			return false;
		}

		var pageName = WikiHelper.ToPublicationWikiPageName(toObsolete.Id);
		var wikiPage = await wikiPages.Page(pageName);

		toObsolete.ObsoletedById = obsoletingPublicationId;
		await db.SaveChangesAsync();

		foreach (var url in toObsolete.PublicationUrls
					.ThatAreStreaming()
					.Where(pu => youtubeSync.IsYoutubeUrl(pu.Url)))
		{
			var obsoleteVideo = new YoutubeVideo(
				toObsolete.Id,
				toObsolete.CreateTimestamp,
				url.Url ?? "",
				url.DisplayName,
				toObsolete.Title,
				wikiPage!,
				toObsolete.System!.Code,
				toObsolete.Authors
					.OrderBy(pa => pa.Ordinal)
					.Select(pa => pa.Author!.UserName),
				obsoletingPublicationId);

			await youtubeSync.SyncYouTubeVideo(obsoleteVideo);
		}

		return true;
	}
}
