using BenchmarkDotNet.Attributes;
using TASVideos.WikiEngine;
using TASVideos.WikiEngine.AST;

namespace TASVideos.Benchmarks;

/// <summary>
/// Benchmarks for Wiki markup parsing and rendering - critical for wiki page performance.
/// Tests parsing complexity, HTML rendering, and text extraction.
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class WikiEngineBenchmarks
{
	private readonly IWriterHelper _writerHelper = NullWriterHelper.Instance;

	private const string SimpleWikiMarkup = """
		!!!Welcome to TASVideos

		This is a __simple__ wiki page with some ''formatting''.

		* List item 1
		* List item 2
		* List item 3
		""";

	private const string ComplexWikiMarkup = """
		!!!Encoder Guidelines

		This page describes the encoding guidelines for TASVideos publications.

		!!General Requirements

		* High quality video encoding
		* Proper audio synchronization
		* Consistent frame rates

		!!Technical Details

		The following __technical specifications__ must be met:

		# Video codec: ''H.264''
		# Audio codec: ''AAC''
		# Container: ''MP4''

		!!!Example Code

		{{{
		ffmpeg -i input.avi -c:v libx264 -preset slow -crf 18 -c:a aac output.mp4
		}}}

		!!See Also

		* [Guidelines]
		* [EncodingGuide/Legacy]
		* [PublicationManual]

		[module:listpublications|limit=10]

		----

		__''This is a test of nested formatting.''__

		[http://tasvideos.org TASVideos Homepage]
		""";

	[Benchmark]
	public List<INode> ParseSimpleMarkup()
	{
		return NewParser.Parse(SimpleWikiMarkup);
	}

	[Benchmark]
	public List<INode> ParseComplexMarkup()
	{
		return NewParser.Parse(ComplexWikiMarkup);
	}

	[Benchmark]
	public async Task<string> RenderSimpleToHtml()
	{
		using var writer = new StringWriter();
		await Util.RenderHtmlAsync(SimpleWikiMarkup, writer, _writerHelper);
		return writer.ToString();
	}

	[Benchmark]
	public async Task<string> RenderComplexToHtml()
	{
		using var writer = new StringWriter();
		await Util.RenderHtmlAsync(ComplexWikiMarkup, writer, _writerHelper);
		return writer.ToString();
	}

	[Benchmark]
	public async Task<string> RenderSimpleToText()
	{
		using var writer = new StringWriter();
		await Util.RenderTextAsync(SimpleWikiMarkup, writer, _writerHelper);
		return writer.ToString();
	}

	[Benchmark]
	public async Task<string> RenderComplexToText()
	{
		using var writer = new StringWriter();
		await Util.RenderTextAsync(ComplexWikiMarkup, writer, _writerHelper);
		return writer.ToString();
	}
}
