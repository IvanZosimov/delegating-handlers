using System;
using Polly.Retry;

namespace Polly;

public static class AsyncRetryTResultSyntax
{
	/// <summary>
	///     Builds an <see cref="AsyncRetryPolicy{TResult}" /> that will wait and retry <paramref name="retryCount" /> times
	///     calling <paramref name="onRetry" /> on each retry with the handled exception or result, the current sleep duration, retry count, and context data.
	///     On each retry, the duration to wait is calculated by calling <paramref name="sleepDurationProvider" /> with
	///     the current retry number (1 for first retry, 2 for second etc), result of previous execution, and execution context.
	/// </summary>
	/// <param name="policyBuilder">The policy builder.</param>
	/// <param name="retryCount">The retry count.</param>
	/// <param name="sleepDurationProvider">The function that provides the duration to wait for for a particular retry attempt.</param>
	/// <param name="onRetry">The action to call on each retry.</param>
	/// <returns>The policy instance.</returns>
	/// <exception cref="ArgumentOutOfRangeException">retryCount;Value must be greater than or equal to zero.</exception>
	/// <exception cref="ArgumentNullException">
	///     sleepDurationProvider
	///     or
	///     onRetryAsync
	/// </exception>
	/// <note>
	/// issue: https://github.com/App-vNext/Polly/issues/908
	/// </note>
	public static AsyncRetryPolicy<TResult> WaitAndRetryAsync<TResult>(this PolicyBuilder<TResult> policyBuilder, int retryCount,
		Func<int, DelegateResult<TResult>, Context, TimeSpan> sleepDurationProvider, Action<DelegateResult<TResult>, TimeSpan, int, Context> onRetry)
	{
		if (onRetry == null) throw new ArgumentNullException(nameof(onRetry));

		return policyBuilder.WaitAndRetryAsync(
			retryCount,
			sleepDurationProvider,
#pragma warning disable 1998 // async method has no awaits, will run synchronously
			onRetryAsync: async (outcome, timespan, i, ctx) => onRetry(outcome, timespan, i, ctx)
#pragma warning restore 1998
		);
	}
}
