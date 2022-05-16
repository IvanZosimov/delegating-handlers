using System.Net;
using AutoFixture;
using AutoFixture.AutoMoq;
using NUnit.Framework;
using Polly.Contrib.WaitAndRetry;
using rm.DelegatingHandlers;

namespace rm.DelegatingHandlersTest;

[TestFixture]
public class ExponentialBackoffWithJitterRetryHandlerTests
{
	private static readonly Exception[] handledExceptions =
		{
			new HttpRequestException(),
			new TimeoutExpiredException(),
		};

	[Test]
	[TestCase(500)]
	[TestCase(501)]
	[TestCase(502)]
	[TestCase(503)]
	[TestCase(504)]
	[TestCase(542)]
	public async Task Retries_On_5xx(int statusCode)
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		var content = fixture.Create<string>();
		var shortCircuitingResponseHandler = new ShortCircuitingResponseHandler(
			new ShortCircuitingResponseHandlerSettings
			{
				StatusCode = (HttpStatusCode)statusCode,
				Content = content,
			});
		var retryAttempt = -1;
		var delegateHandler = new DelegateHandler(
			(request, ct) =>
			{
				retryAttempt++;
				return Task.CompletedTask;
			});
		var retryHandler = new ExponentialBackoffWithJitterRetryHandler(
			new RetrySettings
			{
				RetryCount = 1,
				RetryDelayInMilliseconds = 0,
			});

		using var invoker = HttpMessageInvokerFactory.Create(
			retryHandler, delegateHandler, shortCircuitingResponseHandler);

		using var requestMessage = fixture.Create<HttpRequestMessage>();
		using var _ = await invoker.SendAsync(requestMessage, CancellationToken.None);

		Assert.AreEqual(1, retryAttempt);
	}

	[Test]
	[TestCaseSource(nameof(handledExceptions))]
	public void Retries_On_Exceptions(Exception handledException)
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		var throwingHandler = new ThrowingHandler(handledException);
		var retryAttempt = -1;
		var delegateHandler = new DelegateHandler(
			(request, ct) =>
			{
				retryAttempt++;
				return Task.CompletedTask;
			});
		var retryHandler = new ExponentialBackoffWithJitterRetryHandler(
			new RetrySettings
			{
				RetryCount = 1,
				RetryDelayInMilliseconds = 0,
			});

		using var invoker = HttpMessageInvokerFactory.Create(
			retryHandler, delegateHandler, throwingHandler);

		using var requestMessage = fixture.Create<HttpRequestMessage>();
		Assert.ThrowsAsync(Is.TypeOf(handledException.GetType()), async () =>
		{
			using var _ = await invoker.SendAsync(requestMessage, CancellationToken.None);
		});

		Assert.AreEqual(1, retryAttempt);
	}

	[Test]
	[TestCase(400)]
	[TestCase(401)]
	[TestCase(402)]
	[TestCase(403)]
	[TestCase(404)]
	[TestCase(420)] // calm down
	[TestCase(442)]
	public async Task Does_Not_Retry_On_4xx(int statusCode)
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		var content = fixture.Create<string>();
		var shortCircuitingResponseHandler = new ShortCircuitingResponseHandler(
			new ShortCircuitingResponseHandlerSettings
			{
				StatusCode = (HttpStatusCode)statusCode,
				Content = content,
			});
		var retryAttempt = -1;
		var delegateHandler = new DelegateHandler(
			(request, ct) =>
			{
				retryAttempt++;
				return Task.CompletedTask;
			});
		var retryHandler = new ExponentialBackoffWithJitterRetryHandler(
			new RetrySettings
			{
				RetryCount = 1,
				RetryDelayInMilliseconds = 0,
			});

		using var invoker = HttpMessageInvokerFactory.Create(
			retryHandler, delegateHandler, shortCircuitingResponseHandler);

		using var requestMessage = fixture.Create<HttpRequestMessage>();
		using var _ = await invoker.SendAsync(requestMessage, CancellationToken.None);

		Assert.AreEqual(0, retryAttempt);
	}

	[Test]
	public void Does_Not_Retry_On_TaskCanceledException()
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		var throwingHandler = new ThrowingHandler(new TaskCanceledException());
		var retryAttempt = -1;
		var delegateHandler = new DelegateHandler(
			(request, ct) =>
			{
				retryAttempt++;
				return Task.CompletedTask;
			});
		var retryHandler = new ExponentialBackoffWithJitterRetryHandler(
			new RetrySettings
			{
				RetryCount = 1,
				RetryDelayInMilliseconds = 0,
			});

		using var invoker = HttpMessageInvokerFactory.Create(
			retryHandler, delegateHandler, throwingHandler);

		using var requestMessage = fixture.Create<HttpRequestMessage>();
		Assert.ThrowsAsync<TaskCanceledException>(async () =>
		{
			using var _ = await invoker.SendAsync(requestMessage, CancellationToken.None);
		});

		Assert.AreEqual(0, retryAttempt);
	}

	[Test]
	public async Task When_0_Retries_PollyRetryAttempt_Property_Is_Not_Present()
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		var statusCode = (HttpStatusCode)542;
		var content = fixture.Create<string>();
		var shortCircuitingResponseHandler = new ShortCircuitingResponseHandler(
			new ShortCircuitingResponseHandlerSettings
			{
				StatusCode = statusCode,
				Content = content,
			});
		var retryHandler = new ExponentialBackoffWithJitterRetryHandler(
			new RetrySettings
			{
				RetryCount = 0,
				RetryDelayInMilliseconds = 0,
			});

		using var invoker = HttpMessageInvokerFactory.Create(
			retryHandler, shortCircuitingResponseHandler);

		using var requestMessage = fixture.Create<HttpRequestMessage>();
		using var _ = await invoker.SendAsync(requestMessage, CancellationToken.None);

#pragma warning disable CS0618 // Type or member is obsolete
		Assert.IsFalse(requestMessage.Properties.ContainsKey(RequestProperties.PollyRetryAttempt));
#pragma warning restore CS0618 // Type or member is obsolete
	}

	[Test]
	[TestCase(1)]
	[TestCase(2)]
	public async Task When_N_Retries_PollyRetryAttempt_Property_Is_Present(int retryCount)
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		var statusCode = (HttpStatusCode)542;
		var content = fixture.Create<string>();
		var shortCircuitingResponseHandler = new ShortCircuitingResponseHandler(
			new ShortCircuitingResponseHandlerSettings
			{
				StatusCode = statusCode,
				Content = content,
			});
		var retryHandler = new ExponentialBackoffWithJitterRetryHandler(
			new RetrySettings
			{
				RetryCount = retryCount,
				RetryDelayInMilliseconds = 0,
			});

		using var invoker = HttpMessageInvokerFactory.Create(
			retryHandler, shortCircuitingResponseHandler);

		using var requestMessage = fixture.Create<HttpRequestMessage>();
		using var _ = await invoker.SendAsync(requestMessage, CancellationToken.None);

#pragma warning disable CS0618 // Type or member is obsolete
		Assert.AreEqual(retryCount, requestMessage.Properties[RequestProperties.PollyRetryAttempt]);
#pragma warning restore CS0618 // Type or member is obsolete
	}

	[Test]
	[TestCase(503)]
	[TestCase(429)]
	public async Task Retries_When_RetryAfter_Date_Header(int statusCode)
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		var content = fixture.Create<string>();
		var shortCircuitingResponseHandler = new ShortCircuitingResponseHandler(
			new ShortCircuitingResponseHandlerSettings
			{
				StatusCode = (HttpStatusCode)statusCode,
				Content = content,
			});
		var date = DateTimeOffset.UtcNow.AddSeconds(0);
		var retryAfterDateHandler = new RetryAfterDateHandler(date);
		var retryAttempt = -1;
		var delegateHandler = new DelegateHandler(
			(request, ct) =>
			{
				retryAttempt++;
				return Task.CompletedTask;
			});
		var retryHandler = new ExponentialBackoffWithJitterRetryHandler(
			new RetrySettings
			{
				RetryCount = 1,
				RetryDelayInMilliseconds = 0,
			});

		using var invoker = HttpMessageInvokerFactory.Create(
			retryHandler, delegateHandler, retryAfterDateHandler, shortCircuitingResponseHandler);

		using var requestMessage = fixture.Create<HttpRequestMessage>();
		using var _ = await invoker.SendAsync(requestMessage, CancellationToken.None);

		Assert.AreEqual(1, retryAttempt);
	}

	[Test]
	[TestCase(503)]
	[TestCase(429)]
	public async Task Retries_When_RetryAfter_Delay_Header(int statusCode)
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		var content = fixture.Create<string>();
		var shortCircuitingResponseHandler = new ShortCircuitingResponseHandler(
			new ShortCircuitingResponseHandlerSettings
			{
				StatusCode = (HttpStatusCode)statusCode,
				Content = content,
			});
		var delayInSeconds = 0;
		var retryAfterDateHandler = new RetryAfterDelayHandler(delayInSeconds);
		var retryAttempt = -1;
		var delegateHandler = new DelegateHandler(
			(request, ct) =>
			{
				retryAttempt++;
				return Task.CompletedTask;
			});
		var retryHandler = new ExponentialBackoffWithJitterRetryHandler(
			new RetrySettings
			{
				RetryCount = 1,
				RetryDelayInMilliseconds = 0,
			});

		using var invoker = HttpMessageInvokerFactory.Create(
			retryHandler, delegateHandler, retryAfterDateHandler, shortCircuitingResponseHandler);

		using var requestMessage = fixture.Create<HttpRequestMessage>();
		using var _ = await invoker.SendAsync(requestMessage, CancellationToken.None);

		Assert.AreEqual(1, retryAttempt);
	}

	[Test]
	[TestCase(503)]
	[TestCase(429)]
	public async Task Does_Not_Retry_Retries_When_RetryAfter_Date_Header_High(int statusCode)
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		var content = fixture.Create<string>();
		var shortCircuitingResponseHandler = new ShortCircuitingResponseHandler(
			new ShortCircuitingResponseHandlerSettings
			{
				StatusCode = (HttpStatusCode)statusCode,
				Content = content,
			});
		var date = DateTimeOffset.UtcNow.AddSeconds(5);
		var retryAfterDateHandler = new RetryAfterDateHandler(date);
		var retryAttempt = -1;
		var delegateHandler = new DelegateHandler(
			(request, ct) =>
			{
				retryAttempt++;
				return Task.CompletedTask;
			});
		var retryHandler = new ExponentialBackoffWithJitterRetryHandler(
			new RetrySettings
			{
				RetryCount = 1,
				RetryDelayInMilliseconds = 0,
			});

		using var invoker = HttpMessageInvokerFactory.Create(
			retryHandler, delegateHandler, retryAfterDateHandler, shortCircuitingResponseHandler);

		using var requestMessage = fixture.Create<HttpRequestMessage>();
		using var _ = await invoker.SendAsync(requestMessage, CancellationToken.None);

		Assert.AreEqual(0, retryAttempt);
	}

	[Test]
	[TestCase(503)]
	[TestCase(429)]
	public async Task Does_Not_Retry_Retries_When_RetryAfter_Delay_Header_High(int statusCode)
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		var content = fixture.Create<string>();
		var shortCircuitingResponseHandler = new ShortCircuitingResponseHandler(
			new ShortCircuitingResponseHandlerSettings
			{
				StatusCode = (HttpStatusCode)statusCode,
				Content = content,
			});
		var delayInSeconds = 5;
		var retryAfterDateHandler = new RetryAfterDelayHandler(delayInSeconds);
		var retryAttempt = -1;
		var delegateHandler = new DelegateHandler(
			(request, ct) =>
			{
				retryAttempt++;
				return Task.CompletedTask;
			});
		var retryHandler = new ExponentialBackoffWithJitterRetryHandler(
			new RetrySettings
			{
				RetryCount = 1,
				RetryDelayInMilliseconds = 0,
			});

		using var invoker = HttpMessageInvokerFactory.Create(
			retryHandler, delegateHandler, retryAfterDateHandler, shortCircuitingResponseHandler);

		using var requestMessage = fixture.Create<HttpRequestMessage>();
		using var _ = await invoker.SendAsync(requestMessage, CancellationToken.None);

		Assert.AreEqual(0, retryAttempt);
	}

	[Explicit]
	[Test]
	public void Print_SleepDurations()
	{
		var retryDelayInMilliseconds = 500;
		var retryCount = 5;
		var sleepDurationsWithJitter = Backoff.DecorrelatedJitterBackoffV2(
			medianFirstRetryDelay: TimeSpan.FromMilliseconds(retryDelayInMilliseconds),
			retryCount: retryCount);

		Console.WriteLine($"retryCount: {retryCount}");
		foreach (var sleepDurationWithJitter in sleepDurationsWithJitter)
		{
			Console.WriteLine(sleepDurationWithJitter);
		}
	}

	[Explicit]
	[Test]
	[TestCase(503)]
	[TestCase(429)]
	[TestCase(500)] // DNC for retry-after
	public async Task Showcase_Retries_With_RetryAfter_Delay_Header(int statusCode)
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		var content = fixture.Create<string>();
		var shortCircuitingResponseHandler = new ShortCircuitingResponseHandler(
			new ShortCircuitingResponseHandlerSettings
			{
				StatusCode = (HttpStatusCode)statusCode,
				Content = content,
			});
		var delayInSeconds = 1;
		var retryAfterDateHandler = new RetryAfterDelayHandler(delayInSeconds);
		var retryAttempt = -1;
		var tsPrevious = default(TimeSpan);
		var delegateHandler = new DelegateHandler(
			(request, ct) =>
			{
				var tsCurrent = DateTime.Now.TimeOfDay;
				var delta = (tsCurrent - (tsPrevious != default ? tsPrevious : tsCurrent)).TotalMilliseconds;
				tsPrevious = tsCurrent;
				retryAttempt++;
				Console.WriteLine($"[{tsCurrent.ToString(@"hh\:mm\:ss\.fff")}] making attempt: {retryAttempt}, delta: {delta,7:F0}");
				return Task.CompletedTask;
			});
		var retryHandler = new ExponentialBackoffWithJitterRetryHandler(
			new RetrySettings
			{
				RetryCount = 5,
				RetryDelayInMilliseconds = 1000,
			});

		using var invoker = HttpMessageInvokerFactory.Create(
			retryHandler, delegateHandler, retryAfterDateHandler, shortCircuitingResponseHandler);

		Console.WriteLine($"retry-after: {delayInSeconds}");
		using var requestMessage = fixture.Create<HttpRequestMessage>();
		Console.WriteLine($"[{DateTime.Now.TimeOfDay.ToString(@"hh\:mm\:ss\.fff")}] starting");
		using var _ = await invoker.SendAsync(requestMessage, CancellationToken.None);
		Console.WriteLine($"[{DateTime.Now.TimeOfDay.ToString(@"hh\:mm\:ss\.fff")}] ending");
		Console.WriteLine($"retry-attempt: {retryAttempt}");
	}
}
