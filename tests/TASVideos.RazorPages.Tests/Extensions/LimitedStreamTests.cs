using TASVideos.Extensions;

namespace TASVideos.RazorPages.Tests.Extensions;

[TestClass]
public class LimitedStreamTests
{
	[TestMethod]
	public void Constructor_NullBaseStream_ThrowsArgumentNullException()
	{
		Assert.ThrowsException<ArgumentNullException>(() => new LimitedStream(null!, 100));
	}

	[TestMethod]
	public void Constructor_ZeroMaxSize_ThrowsArgumentOutOfRangeException()
	{
		using var ms = new MemoryStream();
		Assert.ThrowsException<ArgumentOutOfRangeException>(() => new LimitedStream(ms, 0));
	}

	[TestMethod]
	public void Constructor_NegativeMaxSize_ThrowsArgumentOutOfRangeException()
	{
		using var ms = new MemoryStream();
		Assert.ThrowsException<ArgumentOutOfRangeException>(() => new LimitedStream(ms, -1));
	}

	[TestMethod]
	public void Write_WithinLimit_Succeeds()
	{
		// Arrange
		using var baseStream = new MemoryStream();
		using var limitedStream = new LimitedStream(baseStream, 100);
		byte[] data = new byte[50];

		// Act
		limitedStream.Write(data, 0, data.Length);

		// Assert
		Assert.AreEqual(50, baseStream.Length);
	}

	[TestMethod]
	public async Task WriteAsync_WithinLimit_Succeeds()
	{
		// Arrange
		using var baseStream = new MemoryStream();
		using var limitedStream = new LimitedStream(baseStream, 100);
		byte[] data = new byte[50];

		// Act
		await limitedStream.WriteAsync(data, 0, data.Length);

		// Assert
		Assert.AreEqual(50, baseStream.Length);
	}

	[TestMethod]
	public void Write_ExceedsLimit_ThrowsInvalidOperationException()
	{
		// Arrange
		using var baseStream = new MemoryStream();
		using var limitedStream = new LimitedStream(baseStream, 100);
		byte[] data = new byte[150];

		// Act & Assert
		var exception = Assert.ThrowsException<InvalidOperationException>(() =>
			limitedStream.Write(data, 0, data.Length));

		Assert.IsTrue(exception.Message.Contains("zip bomb"));
		Assert.IsTrue(exception.Message.Contains("100"));
	}

	[TestMethod]
	public async Task WriteAsync_ExceedsLimit_ThrowsInvalidOperationException()
	{
		// Arrange
		using var baseStream = new MemoryStream();
		using var limitedStream = new LimitedStream(baseStream, 100);
		byte[] data = new byte[150];

		// Act & Assert
		var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
			await limitedStream.WriteAsync(data, 0, data.Length));

		Assert.IsTrue(exception.Message.Contains("zip bomb"));
		Assert.IsTrue(exception.Message.Contains("100"));
	}

	[TestMethod]
	public async Task WriteAsync_Memory_ExceedsLimit_ThrowsInvalidOperationException()
	{
		// Arrange
		using var baseStream = new MemoryStream();
		using var limitedStream = new LimitedStream(baseStream, 100);
		byte[] data = new byte[150];

		// Act & Assert
		var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
			await limitedStream.WriteAsync(new ReadOnlyMemory<byte>(data)));

		Assert.IsTrue(exception.Message.Contains("zip bomb"));
		Assert.IsTrue(exception.Message.Contains("100"));
	}

	[TestMethod]
	public void Write_MultipleWritesExceedLimit_ThrowsInvalidOperationException()
	{
		// Arrange
		using var baseStream = new MemoryStream();
		using var limitedStream = new LimitedStream(baseStream, 100);
		byte[] data = new byte[60];

		// Act
		limitedStream.Write(data, 0, data.Length); // 60 bytes - OK

		// Assert
		var exception = Assert.ThrowsException<InvalidOperationException>(() =>
			limitedStream.Write(data, 0, data.Length)); // 60 more bytes - exceeds 100 limit

		Assert.IsTrue(exception.Message.Contains("zip bomb"));
		Assert.IsTrue(exception.Message.Contains("Total bytes written: 60"));
	}

	[TestMethod]
	public async Task WriteAsync_MultipleWritesExceedLimit_ThrowsInvalidOperationException()
	{
		// Arrange
		using var baseStream = new MemoryStream();
		using var limitedStream = new LimitedStream(baseStream, 100);
		byte[] data = new byte[60];

		// Act
		await limitedStream.WriteAsync(data, 0, data.Length); // 60 bytes - OK

		// Assert
		var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
			await limitedStream.WriteAsync(data, 0, data.Length)); // 60 more bytes - exceeds 100 limit

		Assert.IsTrue(exception.Message.Contains("zip bomb"));
		Assert.IsTrue(exception.Message.Contains("Total bytes written: 60"));
	}

	[TestMethod]
	public void Read_AlwaysSucceeds()
	{
		// Arrange
		byte[] sourceData = new byte[50];
		for (int i = 0; i < sourceData.Length; i++)
		{
			sourceData[i] = (byte)(i % 256);
		}

		using var baseStream = new MemoryStream(sourceData);
		using var limitedStream = new LimitedStream(baseStream, 10); // Small limit
		byte[] buffer = new byte[50];

		// Act - Reading should not be limited
		int bytesRead = limitedStream.Read(buffer, 0, buffer.Length);

		// Assert
		Assert.AreEqual(50, bytesRead);
		CollectionAssert.AreEqual(sourceData, buffer);
	}

	[TestMethod]
	public async Task ReadAsync_AlwaysSucceeds()
	{
		// Arrange
		byte[] sourceData = new byte[50];
		for (int i = 0; i < sourceData.Length; i++)
		{
			sourceData[i] = (byte)(i % 256);
		}

		using var baseStream = new MemoryStream(sourceData);
		using var limitedStream = new LimitedStream(baseStream, 10); // Small limit
		byte[] buffer = new byte[50];

		// Act - Reading should not be limited
		int bytesRead = await limitedStream.ReadAsync(buffer, 0, buffer.Length);

		// Assert
		Assert.AreEqual(50, bytesRead);
		CollectionAssert.AreEqual(sourceData, buffer);
	}

	[TestMethod]
	public void Write_ExactlyAtLimit_Succeeds()
	{
		// Arrange
		using var baseStream = new MemoryStream();
		using var limitedStream = new LimitedStream(baseStream, 100);
		byte[] data = new byte[100];

		// Act
		limitedStream.Write(data, 0, data.Length);

		// Assert
		Assert.AreEqual(100, baseStream.Length);
	}

	[TestMethod]
	public async Task WriteAsync_ExactlyAtLimit_Succeeds()
	{
		// Arrange
		using var baseStream = new MemoryStream();
		using var limitedStream = new LimitedStream(baseStream, 100);
		byte[] data = new byte[100];

		// Act
		await limitedStream.WriteAsync(data, 0, data.Length);

		// Assert
		Assert.AreEqual(100, baseStream.Length);
	}
}
