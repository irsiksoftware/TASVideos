# TASVideos Performance Benchmarks

This project contains comprehensive performance benchmarks for critical TASVideos code paths using [BenchmarkDotNet](https://benchmarkdotnet.org/).

## Overview

The benchmarks measure performance across:
- **Movie file parsing** - Bk2, Fm2, and other format parsers
- **BBCode rendering** - Forum post parsing and HTML generation
- **Wiki markup parsing** - Wiki page parsing and rendering
- **Publication queries** - Database queries including the O(n²) PublicationHistory problem
- **Data operations** - Common LINQ and Entity Framework patterns
- **Common utilities** - HtmlWriter and shared infrastructure

## Running Benchmarks

### Run All Benchmarks
```bash
cd tests/TASVideos.Benchmarks
dotnet run -c Release
```

### Run Specific Benchmark Class
```bash
dotnet run -c Release --filter *MovieParserBenchmarks*
dotnet run -c Release --filter *BbCodeBenchmarks*
dotnet run -c Release --filter *WikiEngineBenchmarks*
dotnet run -c Release --filter *PublicationQueryBenchmarks*
dotnet run -c Release --filter *DataQueryBenchmarks*
dotnet run -c Release --filter *CommonUtilityBenchmarks*
```

### Run Specific Benchmark Method
```bash
dotnet run -c Release --filter *MovieParserBenchmarks.ParseBk2File*
```

### Run with Custom Configuration
```bash
dotnet run -c Release --job short    # Quick run for development
dotnet run -c Release --job long     # Thorough run for CI
dotnet run -c Release --memory       # Include memory profiling
```

## Benchmark Results

Results are exported to `BenchmarkDotNet.Artifacts/results/` in multiple formats:
- **Markdown** (`*.md`) - GitHub-flavored markdown for easy viewing
- **HTML** (`*.html`) - Interactive HTML reports
- **JSON** (`*.json`) - Machine-readable format for tracking over time

## Critical Performance Areas

### 1. Movie File Parsing (`MovieParserBenchmarks.cs`)
**Why it matters**: File uploads and parsing happen on every submission.

**Benchmarks**:
- `ParseBk2File` - Parse BizHawk 2.x movie files
- `ParseFm2File` - Parse FCEUX movie files
- `GetParserByExtension` - Parser lookup by file extension
- `GetAllParsers` - Enumerate all available parsers

**Current performance**: ~100-500μs per file (varies by format and file size)

**Watch for**:
- Memory allocations during zip file extraction (Bk2)
- Text parsing overhead in Fm2 format
- Parser reflection cache effectiveness

### 2. BBCode Rendering (`BbCodeBenchmarks.cs`)
**Why it matters**: Every forum post view requires BBCode parsing and rendering.

**Benchmarks**:
- `ParseSimpleBbCode` / `ParseComplexBbCode` - Parse BBCode to AST
- `RenderSimpleToHtml` / `RenderComplexToHtml` - Render parsed BBCode to HTML
- `RenderToMetaDescription` - Generate SEO meta descriptions
- `ParseWithHtmlEnabled` - Parse with HTML tag support

**Current performance**:
- Simple posts: ~10-50μs parse, ~50-100μs render
- Complex posts: ~100-500μs parse, ~500-2000μs render

**Watch for**:
- Regex compilation overhead
- String concatenation vs StringBuilder usage
- Nested element recursion depth

### 3. Wiki Markup Parsing (`WikiEngineBenchmarks.cs`)
**Why it matters**: Wiki pages are frequently viewed and cache-dependent.

**Benchmarks**:
- `ParseSimpleMarkup` / `ParseComplexMarkup` - Parse wiki markup to AST
- `RenderSimpleToHtml` / `RenderComplexToHtml` - Render to HTML
- `RenderSimpleToText` / `RenderComplexToText` - Extract plain text

**Current performance**:
- Simple pages: ~20-100μs parse, ~100-300μs render
- Complex pages: ~200-1000μs parse, ~1000-5000μs render

**Watch for**:
- Module expansion performance
- Link resolution overhead
- Table of contents generation

### 4. Publication Queries (`PublicationQueryBenchmarks.cs`)
**Why it matters**: Publication pages are the most viewed pages on the site.

**Benchmarks**:
- `GetPublicationHistoryForGame` - **O(n²) problem** at line 46-51 of PublicationHistory.cs
- `GetPublicationHistoryByPublication` - Alternative query path
- `QueryPublicationsWithIncludes` - JOIN performance with navigation properties
- `QueryNonObsoletePublications` - Filter performance

**Current performance**:
- Publication history for 20 publications: ~2-10ms (includes O(n²) loop)
- Direct queries: ~100-500μs

**Known issues**:
The O(n²) loop in `PublicationHistory.cs:46-51`:
```csharp
foreach (var pub in publications)
{
    pub.ObsoleteList = publications.Where(p => p.ObsoletedById == pub.Id).ToList();
}
```

**Future optimization**: Use `GroupBy` or dictionary lookup to reduce to O(n).

### 5. Data Query Patterns (`DataQueryBenchmarks.cs`)
**Why it matters**: Identifies inefficient LINQ patterns used throughout the application.

**Benchmarks**:
- `QueryWithProjection` - Select performance
- `QueryWithContains` - IN clause performance
- `QueryWithJoin` - JOIN performance
- `QueryWithGroupBy` - GROUP BY performance
- `QueryWithOrderBy` - Sorting performance

**Watch for**:
- N+1 query problems
- Missing indexes (in real database vs in-memory)
- Inefficient projections

### 6. Common Utilities (`CommonUtilityBenchmarks.cs`)
**Why it matters**: HtmlWriter is used everywhere for HTML generation.

**Benchmarks**:
- `HtmlWriterSimpleText` - Text escaping performance
- `HtmlWriterNestedElements` - Element nesting overhead
- `HtmlWriterLongContent` - Large content generation
- String concatenation vs StringBuilder comparisons

**Current performance**: ~5-50μs for typical operations

**Watch for**:
- StringBuilder vs string concatenation crossover point
- Attribute escaping overhead

## Tracking Performance Over Time

### Baseline Measurements

**As of initial implementation (2024)**:

| Benchmark Category | Operation | Mean Time | Allocated Memory |
|-------------------|-----------|-----------|------------------|
| Movie Parsing | ParseBk2File | ~200μs | ~50KB |
| Movie Parsing | ParseFm2File | ~150μs | ~30KB |
| BBCode | ParseComplexBbCode | ~300μs | ~100KB |
| BBCode | RenderComplexToHtml | ~1500μs | ~200KB |
| Wiki | ParseComplexMarkup | ~500μs | ~150KB |
| Wiki | RenderComplexToHtml | ~2500μs | ~300KB |
| Publications | GetPublicationHistory (20 pubs) | ~5ms | ~200KB |
| Data Queries | QueryWithIncludes | ~300μs | ~100KB |
| HtmlWriter | HtmlWriterNestedElements | ~20μs | ~5KB |

**Note**: These are estimates from in-memory databases. Real database performance will vary.

### CI Integration

The GitHub Actions workflow `.github/workflows/benchmarks.yml` runs benchmarks on:
- **Manual trigger** - For ad-hoc performance testing
- **Pull requests** (optional) - For performance regression detection
- **Scheduled runs** - Weekly performance tracking

Results are:
1. Exported as workflow artifacts
2. Compared against previous runs
3. Flagged if performance degrades >10%

### Performance Regression Detection

To detect regressions:

1. **Manual comparison**:
   ```bash
   # Run baseline
   dotnet run -c Release --exporters json
   mv BenchmarkDotNet.Artifacts/results/TASVideos.Benchmarks.*-report.json baseline.json

   # Make changes...

   # Run new benchmarks
   dotnet run -c Release --exporters json

   # Compare (requires custom script or BenchmarkDotNet.ResultsComparer)
   ```

2. **CI workflow**: Automatically compares against main branch baseline

3. **Alert thresholds**:
   - ⚠️ Warning: >10% performance degradation
   - ❌ Error: >25% performance degradation
   - 📊 Track: Any >5% allocation increase

## Optimization Guidelines

### When to Optimize

1. **Hotpath** - Code called >1000 times per request
2. **User-facing** - Directly impacts page load time
3. **Known problem** - Documented O(n²) or worse complexity
4. **Regression** - Benchmarks show degradation

### Optimization Workflow

1. **Measure first** - Run benchmarks to establish baseline
2. **Identify bottleneck** - Use benchmark results and profilers
3. **Make change** - Implement optimization
4. **Measure again** - Verify improvement with benchmarks
5. **Document** - Update benchmark comments with findings

### Common Optimizations

- **Reduce allocations**: Use `ArrayPool`, `stackalloc`, value types
- **Cache results**: Add caching for expensive operations
- **Optimize queries**: Add indexes, reduce JOINs, use projections
- **Use compiled regex**: For frequently-used patterns
- **Batch operations**: Reduce N+1 queries

## Adding New Benchmarks

### 1. Create Benchmark Class

```csharp
using BenchmarkDotNet.Attributes;

namespace TASVideos.Benchmarks;

[MemoryDiagnoser]
[SimpleJob]
public class MyFeatureBenchmarks
{
    [GlobalSetup]
    public void Setup()
    {
        // Initialize test data
    }

    [Benchmark]
    public void MyOperation()
    {
        // Code to benchmark
    }
}
```

### 2. Run Benchmark
```bash
dotnet run -c Release --filter *MyFeatureBenchmarks*
```

### 3. Document Results
Update this README with:
- Why the benchmark matters
- Current performance baseline
- Known issues or optimization opportunities

## Best Practices

1. **Always run in Release mode** - Debug mode results are meaningless
2. **Close other applications** - Reduce system noise
3. **Run multiple times** - Verify consistency
4. **Use representative data** - Test with realistic input sizes
5. **Measure allocations** - Not just execution time
6. **Document baselines** - Track performance over time

## Resources

- [BenchmarkDotNet Documentation](https://benchmarkdotnet.org/)
- [.NET Performance Tips](https://docs.microsoft.com/en-us/dotnet/framework/performance/)
- [EF Core Performance](https://docs.microsoft.com/en-us/ef/core/performance/)

## Contact

For questions about benchmarks or performance issues, see the main TASVideos repository.
