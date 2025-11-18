# ADR-0005: Attribute-Based Movie Parser Plugin System

## Status

Accepted

## Date

2024-11-18 (Documented retrospectively)

## Decision Makers

* TASVideos Development Team

## Context

TASVideos accepts Tool-Assisted Speedrun (TAS) submissions in 23+ different file formats from various emulators:
- BizHawk (.bk2, .bkm)
- FCEUX (.fm2, .fm3)
- Lsnes (.lsmv, .ltm)
- Snes9x (.smv)
- PCSX (.pxm)
- Dolphin (.dtm)
- And many more...

Each format has:
- Different file structures (binary, text, zip archives, XML)
- Different metadata (ROM hash, frame count, author, rerecord count)
- Different platform identifiers
- Legacy versions requiring backward compatibility

Requirements:
- Parse metadata from uploaded movie files
- Validate file format correctness
- Extract: platform, ROM hash, frame count, rerecord count, annotations
- Support adding new formats without modifying core code
- Handle malformed files gracefully
- Performance: Parse files quickly (some are MB+ in size)

Adding new emulator support should be:
1. **Easy:** Just implement parsing logic
2. **Self-contained:** No registration code needed
3. **Discoverable:** Automatically detected by system

## Decision

Implement a **reflection-based plugin system** using C# attributes for movie file parser discovery.

### Architecture

**Plugin Interface:** TASVideos.Parsers/IMovieParser.cs

```csharp
public interface IParser
{
    Task<IParseResult> Parse(Stream file, long length);
}

[AttributeUsage(AttributeTargets.Class)]
public class FileExtensionAttribute : Attribute
{
    public FileExtensionAttribute(string extension)
    {
        Extension = extension;
    }

    public string Extension { get; }
}
```

**Example Parser Implementation:**

```csharp
[FileExtension("bk2")]  // Attribute marks this parser
internal class Bk2 : Parser, IParser
{
    public async Task<IParseResult> Parse(Stream file, long length)
    {
        // BizHawk .bk2 format is a zip archive
        using var archive = new ZipArchive(file, ZipArchiveMode.Read);

        // Extract header
        var headerEntry = archive.GetEntry("Header.txt");
        var header = await ParseHeaderFile(headerEntry);

        // Extract input log
        var inputEntry = archive.GetEntry("Input Log.txt");
        var frames = await CountFrames(inputEntry);

        return new SuccessResult
        {
            SystemCode = header["emuVersion"],
            FrameCount = frames,
            RerecordCount = int.Parse(header["rerecordCount"]),
            // ... more metadata
        };
    }
}
```

### Plugin Discovery

**Automatic Registration:** TASVideos.Parsers/MovieParser.cs

```csharp
private static readonly ICollection<Type> ParserTypes =
    typeof(IParser).Assembly
        .GetTypes()
        .Where(t => typeof(IParser).IsAssignableFrom(t))
        .Where(t => t.IsClass && !t.IsAbstract)
        .Where(t => t.GetCustomAttributes()
                     .OfType<FileExtensionAttribute>()
                     .Any())
        .ToList();

public async Task<IParseResult> Parse(Stream file, string extension, long length)
{
    // Find parser by extension
    var parserType = ParserTypes
        .FirstOrDefault(t => GetExtension(t).Equals(
            extension,
            StringComparison.OrdinalIgnoreCase));

    if (parserType == null)
    {
        return new ErrorResult("Unsupported file format");
    }

    // Instantiate and parse
    var parser = (IParser)Activator.CreateInstance(parserType)!;
    return await parser.Parse(file, length);
}
```

### Supported Formats (23+)

| Extension | Emulator | Format Type |
|-----------|----------|-------------|
| .bk2 | BizHawk | Zip archive |
| .bkm | BizHawk (old) | Text |
| .fm2, .fm3 | FCEUX | Text |
| .lsmv | Lsnes | Zip archive |
| .ltm | Lsnes | Text |
| .smv | Snes9x | Binary |
| .pxm | PCSX | Binary |
| .dtm | Dolphin | Binary |
| .vbm | VisualBoyAdvance | Binary |
| .jrsr | JPC-RR | Zip archive |
| ... | ... | ... |

### Base Parser Class

**TASVideos.Parsers/Base/Parser.cs** provides common utilities:

```csharp
public abstract class Parser
{
    protected static string RemoveSuffix(string str, string suffix);
    protected static string GetString(byte[] bytes, int start, int length);
    protected static int LittleEndian(byte[] bytes, int start, int length);
    protected static int BigEndian(byte[] bytes, int start, int length);
    // ... more helper methods
}
```

### Result Types

```csharp
public interface IParseResult
{
    bool Success { get; }
    string? ErrorMessage { get; }
    // Metadata fields...
}

public class SuccessResult : IParseResult
{
    public bool Success => true;
    public string? SystemCode { get; init; }
    public int FrameCount { get; init; }
    public int RerecordCount { get; init; }
    public string? RomHash { get; init; }
    public string? Annotations { get; init; }
    // ... more fields
}

public class ErrorResult : IParseResult
{
    public bool Success => false;
    public string? ErrorMessage { get; init; }
}
```

### Service Registration

**TASVideos.Parsers/ServiceCollectionExtensions.cs**

```csharp
public static IServiceCollection AddTasvideosMovieParsers(
    this IServiceCollection services)
{
    return services.AddSingleton<IMovieParser, MovieParser>();
}
```

Singleton lifetime is appropriate because parsers are stateless.

## Alternatives Considered

### Manual Registration
**Example:**
```csharp
services.AddParser(".bk2", typeof(Bk2));
services.AddParser(".fm2", typeof(Fm2));
// ... 23 more lines
```

**Pros:**
- Explicit and clear
- Easier to debug

**Cons:**
- Boilerplate code
- Easy to forget registration
- Requires modification of central file

**Why not chosen:** Attribute-based discovery eliminates boilerplate and makes adding parsers trivial.

### Configuration-Based Registration
**Example (appsettings.json):**
```json
{
  "parsers": [
    { "extension": "bk2", "type": "TASVideos.Parsers.Bk2" },
    { "extension": "fm2", "type": "TASVideos.Parsers.Fm2" }
  ]
}
```

**Pros:**
- Runtime configuration
- No recompilation needed

**Cons:**
- Stringly-typed (typos not caught at compile time)
- Separate configuration to maintain
- Adds deployment complexity

**Why not chosen:** Parsers are code, not configuration. Compile-time safety is important.

### Convention-Based Discovery
**Example:** Classes named `*Parser` in `Parsers/` directory

**Pros:**
- No attributes needed
- Simple convention

**Cons:**
- Ambiguous (what if class doesn't follow convention?)
- How to specify file extension?
- Less explicit than attributes

**Why not chosen:** Attributes make intent explicit and allow specifying the extension clearly.

### Separate NuGet Packages Per Parser
**Example:** TASVideos.Parsers.BizHawk, TASVideos.Parsers.FCEUX, etc.

**Pros:**
- Modular
- Optional dependencies

**Cons:**
- Over-engineering for this use case
- Deployment complexity (23+ packages)
- Most formats are simple (<100 lines)

**Why not chosen:** All parsers are maintained by TASVideos team. Single package is simpler.

### Dynamic Loading from DLLs
**Example:** Load parsers from `Plugins/` directory at runtime

**Pros:**
- True plugin system
- Third-party plugins possible

**Cons:**
- Complex security implications
- Versioning and compatibility issues
- Deployment and testing complexity
- Unnecessary for TASVideos (no third-party parsers)

**Why not chosen:** All parsers are first-party. Compile-time linking is safer and simpler.

## Consequences

### Positive

* **Ease of adding parsers:** Create class with attribute, implement `IParser`, done
* **No registration boilerplate:** Reflection discovers parsers automatically
* **Compile-time safety:** Type checking ensures correct implementation
* **Self-documenting:** Attribute makes purpose explicit
* **Singleton efficiency:** Parsers instantiated once and reused
* **Testable:** Each parser is independently testable
* **Base class utilities:** Common parsing functions shared
* **Error handling:** Uniform error reporting via `IParseResult`

### Negative

* **Reflection overhead:** Discovery uses reflection (one-time cost at startup)
* **Magic discovery:** Not immediately obvious how parsers are found
* **Debugging difficulty:** Attribute-based registration harder to trace
* **No runtime addition:** Can't add parsers without recompiling
* **Performance:** Each parse instantiates parser (mitigated: parsers are lightweight)

### Neutral

* **Learning curve:** Developers must understand attribute-based pattern
* **Extension coupling:** File extension tied to class via attribute
* **Assembly scanning:** Only scans TASVideos.Parsers assembly

## Future Considerations

1. **Caching parser instances:** Consider pooling/caching parser instances if performance profiling shows instantiation overhead
2. **Async discovery:** If assembly scanning is slow, move to lazy initialization
3. **Parser versioning:** If emulator formats change, support multiple versions (e.g., `[FileExtension("bk2", Version = 2)]`)
4. **Validation hooks:** Add optional `Validate()` method to `IParser` for pre-parse validation

## Links

* Code: [IMovieParser.cs](../../TASVideos.Parsers/IMovieParser.cs)
* Code: [MovieParser.cs](../../TASVideos.Parsers/MovieParser.cs)
* Code: [Parser implementations](../../TASVideos.Parsers/Parsers/)
* Code: [ServiceCollectionExtensions.cs](../../TASVideos.Parsers/ServiceCollectionExtensions.cs)
* Related ADRs: [ADR-0001](./ADR-0001-dotnet-aspnetcore.md) - .NET and ASP.NET Core
