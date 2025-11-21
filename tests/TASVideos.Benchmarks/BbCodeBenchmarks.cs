using System.Text;
using BenchmarkDotNet.Attributes;
using TASVideos.Common;
using TASVideos.ForumEngine;

namespace TASVideos.Benchmarks;

/// <summary>
/// Benchmarks for BBCode parsing and rendering - critical for forum performance.
/// Tests parsing complexity, HTML rendering, and meta description generation.
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class BbCodeBenchmarks
{
	private readonly IWriterHelper _writerHelper = NullWriterHelper.Instance;
	private const string SimplePost = "[b]Bold[/b] and [i]italic[/i] with [url=https://tasvideos.org]link[/url].";

	private const string ComplexPost = """
		[b]Bold text[/b] and [i]italic text[/i] with [u]underlined[/u] and [s]strikethrough[/s].

		[quote="TestUser"]This is a quote with [highlight]highlighted[/highlight] content.[/quote]

		[spoiler]This is spoiler content.[/spoiler]

		[color=red]red text[/color], [bgcolor=yellow]yellow background[/bgcolor]

		H[sub]2[/sub]O and E=mc[sup]2[/sup]

		[movie=1000]Movie[/movie] [submission=5000]Submission[/submission]

		[code=cpp]
		#include <iostream>
		int main() { return 0; }
		[/code]

		[list]
		[*]First item
		[*]Second item
		[*]Third item
		[/list]

		[table]
		[tr][th]Header 1[/th][th]Header 2[/th][/tr]
		[tr][td]Cell A1[/td][td]Cell B1[/td][/tr]
		[/table]
		""";

	[Benchmark]
	public Element ParseSimpleBbCode()
	{
		return PostParser.Parse(SimplePost, enableBbCode: true, enableHtml: false);
	}

	[Benchmark]
	public Element ParseComplexBbCode()
	{
		return PostParser.Parse(ComplexPost, enableBbCode: true, enableHtml: false);
	}

	[Benchmark]
	public async Task<string> RenderSimpleToHtml()
	{
		var element = PostParser.Parse(SimplePost, enableBbCode: true, enableHtml: false);
		using var writer = new StringWriter();
		var htmlWriter = new HtmlWriter(writer);
		await element.WriteHtml(htmlWriter, _writerHelper);
		return writer.ToString();
	}

	[Benchmark]
	public async Task<string> RenderComplexToHtml()
	{
		var element = PostParser.Parse(ComplexPost, enableBbCode: true, enableHtml: false);
		using var writer = new StringWriter();
		var htmlWriter = new HtmlWriter(writer);
		await element.WriteHtml(htmlWriter, _writerHelper);
		return writer.ToString();
	}

	[Benchmark]
	public async Task<string> RenderToMetaDescription()
	{
		var element = PostParser.Parse(ComplexPost, enableBbCode: true, enableHtml: false);
		var sb = new StringBuilder();
		await element.WriteMetaDescription(sb, _writerHelper);
		return sb.ToString();
	}

	[Benchmark]
	public Element ParseWithHtmlEnabled()
	{
		return PostParser.Parse(ComplexPost, enableBbCode: true, enableHtml: true);
	}

	[Benchmark]
	public Element ParseBbCodeOnly()
	{
		return PostParser.Parse(ComplexPost, enableBbCode: true, enableHtml: false);
	}
}
