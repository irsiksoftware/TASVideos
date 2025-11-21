using BenchmarkDotNet.Attributes;
using Microsoft.EntityFrameworkCore;
using TASVideos.Data;
using TASVideos.Data.Entity;
using TASVideos.Data.Entity.Game;

namespace TASVideos.Benchmarks;

/// <summary>
/// Benchmarks for common data query patterns - identifies LINQ and EF Core performance issues.
/// Measures allocation rates and execution time for hot database query paths.
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class DataQueryBenchmarks
{
	private ApplicationDbContext _db = null!;
	private List<int> _publicationIds = null!;

	[GlobalSetup]
	public void Setup()
	{
		var options = new DbContextOptionsBuilder<ApplicationDbContext>()
			.UseInMemoryDatabase(databaseName: $"QueryBenchDb_{Guid.NewGuid()}")
			.Options;

		_db = new ApplicationDbContext(options);
		SeedData();
	}

	[GlobalCleanup]
	public void Cleanup()
	{
		_db?.Dispose();
	}

	private void SeedData()
	{
		// Create 100 games
		var games = Enumerable.Range(1, 100).Select(i => new Game
		{
			DisplayName = $"Game {i}",
			Abbreviation = $"GAME{i}",
			CreateTimestamp = DateTime.UtcNow
		}).ToList();

		_db.Games.AddRange(games);
		_db.SaveChanges();

		var pubClass = new PublicationClass
		{
			Name = "Standard",
			IconPath = "/images/standard.png"
		};
		_db.PublicationClasses.Add(pubClass);

		// Create goals and publications
		var publications = new List<Publication>();
		foreach (var game in games)
		{
			var goal = new GameGoal
			{
				DisplayName = "any%",
				Game = game
			};
			_db.GameGoals.Add(goal);

			// 5 publications per game
			for (int i = 0; i < 5; i++)
			{
				publications.Add(new Publication
				{
					Game = game,
					GameGoal = goal,
					Title = $"[{game.Id * 100 + i}] {game.DisplayName} any%",
					CreateTimestamp = DateTime.UtcNow.AddDays(-i),
					PublicationClass = pubClass,
					SystemId = 1,
					SystemFrameRateId = 1,
					EmulatorVersion = "BizHawk 2.8"
				});
			}
		}

		_db.Publications.AddRange(publications);
		_db.SaveChanges();

		_publicationIds = publications.Take(50).Select(p => p.Id).ToList();
	}

	[Benchmark]
	public async Task<List<Publication>> QueryWithProjection()
	{
		// Measures Select projection performance
		var result = await _db.Publications
			.Select(p => new Publication
			{
				Id = p.Id,
				Title = p.Title,
				CreateTimestamp = p.CreateTimestamp
			})
			.Take(100)
			.ToListAsync();

		return result;
	}

	[Benchmark]
	public async Task<List<Publication>> QueryWithMultipleWhere()
	{
		// Measures chained Where clause performance
		return await _db.Publications
			.Where(p => p.CreateTimestamp > DateTime.UtcNow.AddDays(-30))
			.Where(p => !p.ObsoletedById.HasValue)
			.Take(100)
			.ToListAsync();
	}

	[Benchmark]
	public async Task<List<Publication>> QueryWithContains()
	{
		// Measures IN query performance (common pattern for filtering)
		return await _db.Publications
			.Where(p => _publicationIds.Contains(p.Id))
			.ToListAsync();
	}

	[Benchmark]
	public async Task<List<Publication>> QueryWithJoin()
	{
		// Measures JOIN performance
		return await _db.Publications
			.Join(_db.Games,
				p => p.GameId,
				g => g.Id,
				(p, g) => p)
			.Take(100)
			.ToListAsync();
	}

	[Benchmark]
	public async Task<List<Game>> QueryWithGroupBy()
	{
		// Measures GROUP BY performance
		var grouped = await _db.Publications
			.GroupBy(p => p.GameId)
			.Select(g => new
			{
				GameId = g.Key,
				Count = g.Count()
			})
			.ToListAsync();

		return await _db.Games
			.Where(g => grouped.Select(x => x.GameId).Contains(g.Id))
			.ToListAsync();
	}

	[Benchmark]
	public async Task<List<Publication>> QueryWithOrderBy()
	{
		// Measures sorting performance
		return await _db.Publications
			.OrderByDescending(p => p.CreateTimestamp)
			.Take(100)
			.ToListAsync();
	}

	[Benchmark]
	public async Task<List<Publication>> QueryWithMultipleOrderBy()
	{
		// Measures multi-column sorting performance
		return await _db.Publications
			.OrderByDescending(p => p.CreateTimestamp)
			.ThenBy(p => p.Title)
			.Take(100)
			.ToListAsync();
	}

	[Benchmark]
	public async Task<bool> QueryWithAny()
	{
		// Measures existence check performance
		return await _db.Publications
			.Where(p => p.GameId == 1)
			.AnyAsync();
	}

	[Benchmark]
	public async Task<Publication?> QueryWithFirstOrDefault()
	{
		// Measures single row retrieval performance
		return await _db.Publications
			.Where(p => p.GameId == 1)
			.FirstOrDefaultAsync();
	}

	[Benchmark]
	public async Task<Publication?> QueryWithSingleOrDefault()
	{
		// Measures unique row retrieval performance
		return await _db.Publications
			.Where(p => p.Id == _publicationIds.First())
			.SingleOrDefaultAsync();
	}
}
