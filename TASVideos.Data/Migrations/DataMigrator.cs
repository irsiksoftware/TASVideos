using System.IO.Compression;
using Microsoft.EntityFrameworkCore;
using TASVideos.Data.Entity;

namespace TASVideos.Data.Migrations;

/// <summary>
/// Migrates existing movie file data from legacy byte array columns to the new MovieFiles table.
/// This migrator handles the conversion of submission and publication movie files,
/// applying gzip compression and storing the result in the centralized MovieFiles table.
/// </summary>
public class DataMigrator
{
	private readonly ApplicationDbContext _db;

	public DataMigrator(ApplicationDbContext db)
	{
		_db = db;
	}

	/// <summary>
	/// Migrates all submission movie files to the new MovieFiles table.
	/// For each submission with a movie file, it will:
	/// 1. Decompress if the file is zipped
	/// 2. Attempt to compress with gzip
	/// 3. Store the smaller of raw or gzipped data
	/// </summary>
	public async Task MigrateSubmissionFiles()
	{
		var submissions = await _db.Submissions
#pragma warning disable CS0618 // Type or member is obsolete
			.Where(s => s.MovieFile.Length > 0 && s.MovieFileId == null)
#pragma warning restore CS0618 // Type or member is obsolete
			.ToListAsync();

		foreach (var submission in submissions)
		{
#pragma warning disable CS0618 // Type or member is obsolete
			var rawData = submission.MovieFile;
#pragma warning restore CS0618 // Type or member is obsolete

			// Decompress if zipped
			if (IsZipped(rawData))
			{
				rawData = DecompressZip(rawData);
			}

			var movieFile = await CompressAndStore(rawData, submission.MovieExtension ?? "");
			submission.MovieFileId = movieFile.Id;
		}

		await _db.SaveChangesAsync();
	}

	/// <summary>
	/// Migrates all publication movie files to the new MovieFiles table.
	/// For each publication with a movie file, it will:
	/// 1. Decompress if the file is zipped
	/// 2. Attempt to compress with gzip
	/// 3. Store the smaller of raw or gzipped data
	/// </summary>
	public async Task MigratePublicationFiles()
	{
		var publications = await _db.Publications
#pragma warning disable CS0618 // Type or member is obsolete
			.Where(p => p.MovieFile.Length > 0 && p.MovieFileId == null)
#pragma warning restore CS0618 // Type or member is obsolete
			.ToListAsync();

		foreach (var publication in publications)
		{
#pragma warning disable CS0618 // Type or member is obsolete
			var rawData = publication.MovieFile;
#pragma warning restore CS0618 // Type or member is obsolete

			// Decompress if zipped
			if (IsZipped(rawData))
			{
				rawData = DecompressZip(rawData);
			}

			var movieFile = await CompressAndStore(rawData, publication.MovieFileName);
			publication.MovieFileId = movieFile.Id;
		}

		await _db.SaveChangesAsync();
	}

	/// <summary>
	/// Migrates all publication_files movie files to the new MovieFiles table.
	/// For each publication file with movie file data, it will:
	/// 1. Decompress if the file is zipped
	/// 2. Attempt to compress with gzip
	/// 3. Store the smaller of raw or gzipped data
	/// </summary>
	public async Task MigratePublicationFileRecords()
	{
		var publicationFiles = await _db.PublicationFiles
#pragma warning disable CS0618 // Type or member is obsolete
			.Where(pf => pf.FileData != null && pf.FileData.Length > 0 && pf.MovieFileId == null && pf.Type == FileType.MovieFile)
#pragma warning restore CS0618 // Type or member is obsolete
			.ToListAsync();

		foreach (var publicationFile in publicationFiles)
		{
#pragma warning disable CS0618 // Type or member is obsolete
			var rawData = publicationFile.FileData!;
#pragma warning restore CS0618 // Type or member is obsolete

			// Decompress if zipped
			if (IsZipped(rawData))
			{
				rawData = DecompressZip(rawData);
			}

			var movieFile = await CompressAndStore(rawData, publicationFile.Path);
			publicationFile.MovieFileId = movieFile.Id;
		}

		await _db.SaveChangesAsync();
	}

	/// <summary>
	/// Compresses data with gzip and stores in MovieFiles table.
	/// Stores the smaller of raw or gzipped data.
	/// </summary>
	private async Task<MovieFile> CompressAndStore(byte[] rawData, string fileName)
	{
		byte[] dataToStore;
		Compression compressionType;
		var originalLength = rawData.Length;

		// Try to compress with gzip
		var gzippedData = CompressGzip(rawData);

		if (gzippedData.Length < rawData.Length)
		{
			dataToStore = gzippedData;
			compressionType = Compression.Gzip;
		}
		else
		{
			dataToStore = rawData;
			compressionType = Compression.None;
		}

		var movieFile = new MovieFile
		{
			FileData = dataToStore,
			CompressionType = compressionType,
			OriginalLength = originalLength,
			FileName = fileName
		};

		_db.MovieFiles.Add(movieFile);
		await _db.SaveChangesAsync();

		return movieFile;
	}

	private static bool IsZipped(byte[] data)
	{
		// ZIP files start with "PK" (0x50 0x4B)
		return data.Length >= 2 && data[0] == 0x50 && data[1] == 0x4B;
	}

	private static byte[] DecompressZip(byte[] data)
	{
		using var inputStream = new MemoryStream(data);
		using var archive = new ZipArchive(inputStream, ZipArchiveMode.Read);

		// Get first entry (movie file should be the only or first entry)
		var entry = archive.Entries.FirstOrDefault();
		if (entry == null)
		{
			return data; // Return original if no entries
		}

		using var entryStream = entry.Open();
		using var outputStream = new MemoryStream();
		entryStream.CopyTo(outputStream);
		return outputStream.ToArray();
	}

	private static byte[] CompressGzip(byte[] data)
	{
		using var outputStream = new MemoryStream();
		using (var gzipStream = new GZipStream(outputStream, CompressionMode.Compress))
		{
			gzipStream.Write(data, 0, data.Length);
		}
		return outputStream.ToArray();
	}

	public static byte[] DecompressGzip(byte[] data)
	{
		using var inputStream = new MemoryStream(data);
		using var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress);
		using var outputStream = new MemoryStream();
		gzipStream.CopyTo(outputStream);
		return outputStream.ToArray();
	}
}
