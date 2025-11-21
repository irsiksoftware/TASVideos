using BenchmarkDotNet.Attributes;
using Microsoft.EntityFrameworkCore;
using TASVideos.Core.Services;
using TASVideos.Data;
using TASVideos.Data.Entity;
using TASVideos.Data.Entity.Game;

namespace TASVideos.Benchmarks;

/// <summary>
/// Benchmarks for publication queries - critical for site performance.
/// Measures the O(n²) problem in PublicationHistory and other hot query paths.
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class PublicationQueryBenchmarks
{
	private ApplicationDbContext _db = null!;
	private IPublicationHistory _publicationHistory = null!;
	private int _testGameId;
	private int _testPublicationId;

	[GlobalSetup]
	public void Setup()
	{
		// Create in-memory database with test data
		var options = new DbContextOptionsBuilder<ApplicationDbContext>()
			.UseInMemoryDatabase(databaseName: $"BenchmarkDb_{Guid.NewGuid()}")
			.Options;

		_db = new ApplicationDbContext(options);

		// Seed test data
		SeedTestData();

		_publicationHistory = new PublicationHistory(_db);
	}

	[GlobalCleanup]
	public void Cleanup()
	{
		_db?.Dispose();
	}

	private void SeedTestData()
	{
		// Create a game
		var game = new Game
		{
			DisplayName = "Super Mario Bros.",
			Abbreviation = "SMB",
			CreateTimestamp = DateTime.UtcNow
		};
		_db.Games.Add(game);
		_db.SaveChanges();
		_testGameId = game.Id;

		// Create a publication class
		var pubClass = new PublicationClass
		{
			Name = "Standard",
			IconPath = "/images/standard.png"
		};
		_db.PublicationClasses.Add(pubClass);

		// Create a game goal
		var gameGoal = new GameGoal
		{
			DisplayName = "any%",
			Game = game
		};
		_db.GameGoals.Add(gameGoal);
		_db.SaveChanges();

		// Create a chain of publications (simulating obsolescence chain)
		// This tests the O(n²) problem in PublicationHistory
		Publication? previousPub = null;
		for (int i = 1; i <= 20; i++)
		{
			var pub = new Publication
			{
				Game = game,
				GameGoal = gameGoal,
				Title = $"[{i}] SMB any% in {100 - i}:00",
				CreateTimestamp = DateTime.UtcNow.AddDays(-100 + i),
				ObsoletedById = null,
				PublicationClass = pubClass,
				SystemId = 1,
				SystemFrameRateId = 1,
				EmulatorVersion = "BizHawk 2.8"
			};

			if (previousPub != null)
			{
				previousPub.ObsoletedById = pub.Id;
			}

			_db.Publications.Add(pub);
			previousPub = pub;
		}

		// Add some parallel branches (different goals for same game)
		for (int i = 1; i <= 10; i++)
		{
			var altGoal = new GameGoal
			{
				DisplayName = $"warpless-{i}",
				Game = game
			};
			_db.GameGoals.Add(altGoal);

			var pub = new Publication
			{
				Game = game,
				GameGoal = altGoal,
				Title = $"[{20 + i}] SMB warpless-{i}",
				CreateTimestamp = DateTime.UtcNow.AddDays(-50 + i),
				PublicationClass = pubClass,
				SystemId = 1,
				SystemFrameRateId = 1,
				EmulatorVersion = "BizHawk 2.8"
			};

			_db.Publications.Add(pub);
		}

		_db.SaveChanges();
		_testPublicationId = previousPub!.Id;
	}

	[Benchmark]
	public async Task<PublicationHistoryGroup?> GetPublicationHistoryForGame()
	{
		// This benchmark measures the O(n²) problem in PublicationHistory.cs:46-51
		// where ObsoleteList is built by filtering entire publication list for each publication
		return await _publicationHistory.ForGame(_testGameId);
	}

	[Benchmark]
	public async Task<PublicationHistoryGroup?> GetPublicationHistoryByPublication()
	{
		return await _publicationHistory.ForGameByPublication(_testPublicationId);
	}

	[Benchmark]
	public async Task<List<Publication>> QueryPublicationsByGame()
	{
		return await _db.Publications
			.Where(p => p.GameId == _testGameId)
			.ToListAsync();
	}

	[Benchmark]
	public async Task<List<Publication>> QueryPublicationsWithIncludes()
	{
		return await _db.Publications
			.Include(p => p.Game)
			.Include(p => p.GameGoal)
			.Include(p => p.PublicationClass)
			.Include(p => p.PublicationFlags)
			.Where(p => p.GameId == _testGameId)
			.ToListAsync();
	}

	[Benchmark]
	public async Task<List<Publication>> QueryNonObsoletePublications()
	{
		return await _db.Publications
			.Where(p => p.GameId == _testGameId && !p.ObsoletedById.HasValue)
			.ToListAsync();
	}

	[Benchmark]
	public async Task<int> CountPublicationsByGame()
	{
		return await _db.Publications
			.Where(p => p.GameId == _testGameId)
			.CountAsync();
	}
}
