using BenchmarkDotNet.Attributes;
using TASVideos.Common;

namespace TASVideos.Benchmarks;

/// <summary>
/// Benchmarks for common utility functions used throughout the application.
/// Tests HtmlWriter performance and other shared utilities.
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class CommonUtilityBenchmarks
{
	private const string SampleText = "This is a sample text with <html> tags & special characters like 'quotes' and \"double quotes\".";
	private const string LongText = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. " +
		"Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. " +
		"Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris.";

	[Benchmark]
	public async Task<string> HtmlWriterSimpleText()
	{
		using var sw = new StringWriter();
		var writer = new HtmlWriter(sw);

		await writer.Text(SampleText);

		return sw.ToString();
	}

	[Benchmark]
	public async Task<string> HtmlWriterWithElements()
	{
		using var sw = new StringWriter();
		var writer = new HtmlWriter(sw);

		await writer.StartElement("div");
		await writer.Attribute("class", "test-class");
		await writer.Text(SampleText);
		await writer.EndElement();

		return sw.ToString();
	}

	[Benchmark]
	public async Task<string> HtmlWriterNestedElements()
	{
		using var sw = new StringWriter();
		var writer = new HtmlWriter(sw);

		await writer.StartElement("div");
		await writer.Attribute("class", "outer");

		await writer.StartElement("p");
		await writer.Attribute("class", "inner");
		await writer.Text(SampleText);
		await writer.EndElement();

		await writer.StartElement("span");
		await writer.Text("More text");
		await writer.EndElement();

		await writer.EndElement();

		return sw.ToString();
	}

	[Benchmark]
	public async Task<string> HtmlWriterLongContent()
	{
		using var sw = new StringWriter();
		var writer = new HtmlWriter(sw);

		for (int i = 0; i < 10; i++)
		{
			await writer.StartElement("p");
			await writer.Text(LongText);
			await writer.EndElement();
		}

		return sw.ToString();
	}

	[Benchmark]
	public async Task<string> HtmlWriterVoidElement()
	{
		using var sw = new StringWriter();
		var writer = new HtmlWriter(sw);

		await writer.VoidElement("br");
		await writer.VoidElement("hr");
		await writer.VoidElement("img");

		return sw.ToString();
	}

	[Benchmark]
	public async Task<string> HtmlWriterMultipleAttributes()
	{
		using var sw = new StringWriter();
		var writer = new HtmlWriter(sw);

		await writer.StartElement("a");
		await writer.Attribute("href", "https://tasvideos.org");
		await writer.Attribute("class", "link");
		await writer.Attribute("target", "_blank");
		await writer.Attribute("rel", "noopener");
		await writer.Text("Link Text");
		await writer.EndElement();

		return sw.ToString();
	}

	[Benchmark]
	public string StringConcatenationSmall()
	{
		// Compare against StringBuilder for small strings
		return "Hello " + "World " + "from " + "TASVideos";
	}

	[Benchmark]
	public string StringBuilderSmall()
	{
		var sb = new System.Text.StringBuilder();
		sb.Append("Hello ");
		sb.Append("World ");
		sb.Append("from ");
		sb.Append("TASVideos");
		return sb.ToString();
	}

	[Benchmark]
	public string StringConcatenationLarge()
	{
		var result = "";
		for (int i = 0; i < 100; i++)
		{
			result += $"Item {i} ";
		}
		return result;
	}

	[Benchmark]
	public string StringBuilderLarge()
	{
		var sb = new System.Text.StringBuilder();
		for (int i = 0; i < 100; i++)
		{
			sb.Append($"Item {i} ");
		}
		return sb.ToString();
	}
}
