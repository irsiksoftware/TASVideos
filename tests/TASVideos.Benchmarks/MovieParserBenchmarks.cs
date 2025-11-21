using BenchmarkDotNet.Attributes;
using TASVideos.Parsers;
using TASVideos.Parsers.Parsers;

namespace TASVideos.Benchmarks;

/// <summary>
/// Benchmarks for movie file parsing - critical for file upload performance.
/// Tests multiple format parsers to identify performance bottlenecks.
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class MovieParserBenchmarks
{
	private byte[] _bk2FileData = null!;
	private byte[] _fm2FileData = null!;
	private Bk2 _bk2Parser = null!;
	private Fm2 _fm2Parser = null!;

	[GlobalSetup]
	public void Setup()
	{
		// Load sample movie files
		_bk2FileData = File.ReadAllBytes(Path.Combine("SampleFiles", "System-Nes.bk2"));
		_fm2FileData = File.ReadAllBytes(Path.Combine("SampleFiles", "ntsc.fm2"));

		_bk2Parser = new Bk2();
		_fm2Parser = new Fm2();
	}

	[Benchmark]
	public IParseResult ParseBk2File()
	{
		using var stream = new MemoryStream(_bk2FileData);
		return _bk2Parser.Parse(stream, _bk2FileData.Length);
	}

	[Benchmark]
	public IParseResult ParseFm2File()
	{
		using var stream = new MemoryStream(_fm2FileData);
		return _fm2Parser.Parse(stream, _fm2FileData.Length);
	}

	[Benchmark]
	public IMovieParser? GetParserByExtension()
	{
		// Tests parser reflection/lookup performance
		return MovieParser.GetParser(".bk2");
	}

	[Benchmark]
	public IEnumerable<IMovieParser> GetAllParsers()
	{
		// Tests parser enumeration performance
		return MovieParser.GetAllParsers();
	}
}
