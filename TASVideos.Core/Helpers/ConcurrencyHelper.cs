using Microsoft.EntityFrameworkCore;

namespace TASVideos.Core.Helpers;

/// <summary>
/// Provides utility methods for handling database concurrency conflicts
/// with retry logic and exponential backoff
/// </summary>
public static class ConcurrencyHelper
{
	private const int DefaultMaxRetries = 3;
	private const int DefaultInitialDelayMs = 50;
	private const int DefaultMaxDelayMs = 1000;

	/// <summary>
	/// Executes an action with retry logic for handling DbUpdateConcurrencyException
	/// using exponential backoff strategy
	/// </summary>
	/// <param name="action">The action to execute that may throw concurrency exceptions</param>
	/// <param name="maxRetries">Maximum number of retry attempts (default: 3)</param>
	/// <param name="initialDelayMs">Initial delay in milliseconds before first retry (default: 50ms)</param>
	/// <param name="maxDelayMs">Maximum delay in milliseconds between retries (default: 1000ms)</param>
	/// <returns>True if the action succeeded, false if all retries were exhausted</returns>
	public static async Task<bool> ExecuteWithRetryAsync(
		Func<Task> action,
		int maxRetries = DefaultMaxRetries,
		int initialDelayMs = DefaultInitialDelayMs,
		int maxDelayMs = DefaultMaxDelayMs)
	{
		int retryCount = 0;
		int currentDelay = initialDelayMs;

		while (retryCount <= maxRetries)
		{
			try
			{
				await action();
				return true;
			}
			catch (DbUpdateConcurrencyException) when (retryCount < maxRetries)
			{
				retryCount++;
				await Task.Delay(currentDelay);

				// Exponential backoff: double the delay for next retry, capped at maxDelayMs
				currentDelay = Math.Min(currentDelay * 2, maxDelayMs);
			}
			catch (DbUpdateConcurrencyException)
			{
				// Final retry exhausted
				return false;
			}
		}

		return false;
	}

	/// <summary>
	/// Executes a function with retry logic for handling DbUpdateConcurrencyException
	/// using exponential backoff strategy
	/// </summary>
	/// <typeparam name="T">The return type of the function</typeparam>
	/// <param name="func">The function to execute that may throw concurrency exceptions</param>
	/// <param name="defaultValue">The value to return if all retries are exhausted</param>
	/// <param name="maxRetries">Maximum number of retry attempts (default: 3)</param>
	/// <param name="initialDelayMs">Initial delay in milliseconds before first retry (default: 50ms)</param>
	/// <param name="maxDelayMs">Maximum delay in milliseconds between retries (default: 1000ms)</param>
	/// <returns>The result of the function, or defaultValue if all retries were exhausted</returns>
	public static async Task<T> ExecuteWithRetryAsync<T>(
		Func<Task<T>> func,
		T defaultValue,
		int maxRetries = DefaultMaxRetries,
		int initialDelayMs = DefaultInitialDelayMs,
		int maxDelayMs = DefaultMaxDelayMs)
	{
		int retryCount = 0;
		int currentDelay = initialDelayMs;

		while (retryCount <= maxRetries)
		{
			try
			{
				return await func();
			}
			catch (DbUpdateConcurrencyException) when (retryCount < maxRetries)
			{
				retryCount++;
				await Task.Delay(currentDelay);

				// Exponential backoff: double the delay for next retry, capped at maxDelayMs
				currentDelay = Math.Min(currentDelay * 2, maxDelayMs);
			}
			catch (DbUpdateConcurrencyException)
			{
				// Final retry exhausted
				return defaultValue;
			}
		}

		return defaultValue;
	}

	/// <summary>
	/// Executes a function with retry logic that returns a result indicating success or failure
	/// </summary>
	/// <typeparam name="T">The return type of the function</typeparam>
	/// <param name="func">The function to execute that may throw concurrency exceptions</param>
	/// <param name="maxRetries">Maximum number of retry attempts (default: 3)</param>
	/// <param name="initialDelayMs">Initial delay in milliseconds before first retry (default: 50ms)</param>
	/// <param name="maxDelayMs">Maximum delay in milliseconds between retries (default: 1000ms)</param>
	/// <returns>A tuple indicating success status and the result value (or default if failed)</returns>
	public static async Task<(bool Success, T? Result)> TryExecuteWithRetryAsync<T>(
		Func<Task<T>> func,
		int maxRetries = DefaultMaxRetries,
		int initialDelayMs = DefaultInitialDelayMs,
		int maxDelayMs = DefaultMaxDelayMs)
	{
		int retryCount = 0;
		int currentDelay = initialDelayMs;

		while (retryCount <= maxRetries)
		{
			try
			{
				var result = await func();
				return (true, result);
			}
			catch (DbUpdateConcurrencyException) when (retryCount < maxRetries)
			{
				retryCount++;
				await Task.Delay(currentDelay);

				// Exponential backoff: double the delay for next retry, capped at maxDelayMs
				currentDelay = Math.Min(currentDelay * 2, maxDelayMs);
			}
			catch (DbUpdateConcurrencyException)
			{
				// Final retry exhausted
				return (false, default);
			}
		}

		return (false, default);
	}
}
