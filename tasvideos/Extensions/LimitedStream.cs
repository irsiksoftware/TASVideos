namespace TASVideos.Extensions;

/// <summary>
/// A stream wrapper that enforces a maximum size limit to prevent zip bomb attacks.
/// Throws an exception if the size limit is exceeded during write operations.
/// </summary>
public class LimitedStream : Stream
{
	private readonly Stream _baseStream;
	private readonly long _maxSize;
	private long _totalBytesWritten;

	public LimitedStream(Stream baseStream, long maxSize)
	{
		_baseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
		_maxSize = maxSize;
		_totalBytesWritten = 0;

		if (maxSize <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(maxSize), "Maximum size must be greater than zero.");
		}
	}

	public override bool CanRead => _baseStream.CanRead;
	public override bool CanSeek => _baseStream.CanSeek;
	public override bool CanWrite => _baseStream.CanWrite;
	public override long Length => _baseStream.Length;
	public override long Position
	{
		get => _baseStream.Position;
		set => _baseStream.Position = value;
	}

	public override void Flush() => _baseStream.Flush();

	public override Task FlushAsync(CancellationToken cancellationToken) => _baseStream.FlushAsync(cancellationToken);

	public override int Read(byte[] buffer, int offset, int count) => _baseStream.Read(buffer, offset, count);

	public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		=> _baseStream.ReadAsync(buffer, offset, count, cancellationToken);

	public override long Seek(long offset, SeekOrigin origin) => _baseStream.Seek(offset, origin);

	public override void SetLength(long value) => _baseStream.SetLength(value);

	public override void Write(byte[] buffer, int offset, int count)
	{
		CheckSizeLimit(count);
		_baseStream.Write(buffer, offset, count);
		_totalBytesWritten += count;
	}

	public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		CheckSizeLimit(count);
		await _baseStream.WriteAsync(buffer, offset, count, cancellationToken);
		_totalBytesWritten += count;
	}

	public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
	{
		CheckSizeLimit(buffer.Length);
		await _baseStream.WriteAsync(buffer, cancellationToken);
		_totalBytesWritten += buffer.Length;
	}

	private void CheckSizeLimit(int bytesToWrite)
	{
		if (_totalBytesWritten + bytesToWrite > _maxSize)
		{
			throw new InvalidOperationException(
				$"Decompressed data exceeds maximum allowed size of {_maxSize:N0} bytes. " +
				$"This may indicate a zip bomb attack. Total bytes written: {_totalBytesWritten:N0}, " +
				$"attempted to write: {bytesToWrite:N0} more bytes.");
		}
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_baseStream.Dispose();
		}

		base.Dispose(disposing);
	}

	public override async ValueTask DisposeAsync()
	{
		await _baseStream.DisposeAsync();
		await base.DisposeAsync();
	}
}
