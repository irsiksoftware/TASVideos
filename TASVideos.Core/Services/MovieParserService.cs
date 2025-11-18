using System.IO.Compression;
using Microsoft.AspNetCore.Http;
using TASVideos.MovieParsers;
using TASVideos.MovieParsers.Result;

namespace TASVideos.Core.Services;

public interface IMovieParserService
{
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
	/// Maps a parse result to submission data with system and frame rate information
	/// </summary>
	Task<ParsedSubmissionData?> MapParsedResult(IParseResult parseResult);
}

internal class MovieParserService(
	ApplicationDbContext db,
	IMovieParser movieParser,
	IFileService fileService)
	: IMovieParserService
{
	public async Task<(IParseResult ParseResult, byte[] MovieFileBytes)> ParseMovieFileOrZip(IFormFile movieFile)
	{
		// Inline implementation of DecompressOrTakeRaw
		var rawFileStream = new MemoryStream();
		await movieFile.CopyToAsync(rawFileStream);

		MemoryStream fileStream;
		try
		{
			rawFileStream.Position = 0;
			using var gzip = new GZipStream(rawFileStream, CompressionMode.Decompress, leaveOpen: true);
			var decompressedFileStream = new MemoryStream();
			await gzip.CopyToAsync(decompressedFileStream);
			await rawFileStream.DisposeAsync();
			decompressedFileStream.Position = 0;
			fileStream = decompressedFileStream;
		}
		catch (InvalidDataException)
		{
			rawFileStream.Position = 0;
			fileStream = rawFileStream;
		}

		byte[] fileBytes = fileStream.ToArray();

		// Inline implementation of IsZip
		bool isZip = movieFile.FileName.EndsWith(".zip")
			&& movieFile.ContentType is "application/x-zip-compressed" or "application/zip";

		var parseResult = isZip
			? await movieParser.ParseZip(fileStream)
			: await movieParser.ParseFile(movieFile.FileName, fileStream);

		byte[] movieFileBytes = isZip
			? fileBytes
			: await fileService.ZipFile(fileBytes, movieFile.FileName);

		return (parseResult, movieFileBytes);
	}

	public async Task<(IParseResult ParseResult, byte[] MovieFileBytes)> ParseMovieFile(IFormFile movieFile)
	{
		var rawFileStream = new MemoryStream();
		await movieFile.CopyToAsync(rawFileStream);

		MemoryStream fileStream;
		try
		{
			rawFileStream.Position = 0;
			using var gzip = new GZipStream(rawFileStream, CompressionMode.Decompress, leaveOpen: true);
			var decompressedFileStream = new MemoryStream();
			await gzip.CopyToAsync(decompressedFileStream);
			await rawFileStream.DisposeAsync();
			decompressedFileStream.Position = 0;
			fileStream = decompressedFileStream;
		}
		catch (InvalidDataException)
		{
			rawFileStream.Position = 0;
			fileStream = rawFileStream;
		}

		// Parse the individual movie file (not a zip)
		var parseResult = await movieParser.ParseFile(movieFile.FileName, fileStream);

		// Get the file bytes for storage
		byte[] movieFileBytes = fileStream.ToArray();

		return (parseResult, movieFileBytes);
	}

	public async Task<ParsedSubmissionData?> MapParsedResult(IParseResult parseResult)
	{
		if (!parseResult.Success)
		{
			throw new InvalidOperationException("Cannot mapped failed parse result.");
		}

		var system = await db.GameSystems
			.ForCode(parseResult.SystemCode)
			.SingleOrDefaultAsync();

		if (system is null)
		{
			return null;
		}

		var annotations = parseResult.Annotations.CapAndEllipse(3500);
		var warnings = parseResult.Warnings.ToList();
		string? warningsString = null;
		if (warnings.Any())
		{
			warningsString = string.Join(",", warnings).Cap(500);
		}

		GameSystemFrameRate? systemFrameRate;
		if (parseResult.FrameRateOverride.HasValue)
		{
			// ReSharper disable CompareOfFloatsByEqualityOperator
			var frameRate = await db.GameSystemFrameRates
				.ForSystem(system.Id)
				.FirstOrDefaultAsync(sf => sf.FrameRate == parseResult.FrameRateOverride.Value);

			if (frameRate is null)
			{
				frameRate = new GameSystemFrameRate
				{
					System = system,
					FrameRate = parseResult.FrameRateOverride.Value,
					RegionCode = parseResult.Region.ToString().ToUpper()
				};
				db.GameSystemFrameRates.Add(frameRate);
				await db.SaveChangesAsync();
			}

			systemFrameRate = frameRate;
		}
		else
		{
			// SingleOrDefault should work here because the only time there could be more than one for a system and region are formats that return a framerate override
			// Those systems should never hit this code block.  But just in case.
			systemFrameRate = await db.GameSystemFrameRates
				.ForSystem(system.Id)
				.ForRegion(parseResult.Region.ToString().ToUpper())
				.FirstOrDefaultAsync();
		}

		return new ParsedSubmissionData(
			(int)parseResult.StartType,
			parseResult.Frames,
			parseResult.RerecordCount,
			parseResult.FileExtension,
			system,
			parseResult.CycleCount,
			annotations,
			warningsString,
			systemFrameRate);
	}
}
