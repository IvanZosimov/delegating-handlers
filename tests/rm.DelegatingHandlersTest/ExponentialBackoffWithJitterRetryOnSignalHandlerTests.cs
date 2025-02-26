﻿using System.Net;
using AutoFixture;
using AutoFixture.AutoMoq;
using NUnit.Framework;
using rm.DelegatingHandlers;

namespace rm.DelegatingHandlersTest;

[TestFixture]
public class ExponentialBackoffWithJitterRetryOnSignalHandlerTests
{
	[Test]
	public async Task Retries_On_Signal()
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		var shortCircuitingCannedActionsHandler = new ShortCircuitingCannedActionsHandler(
			(request) => new HttpResponseMessage() { StatusCode = (HttpStatusCode)404 }, // retry
			(request) => new HttpResponseMessage() { StatusCode = (HttpStatusCode)200, Content = new StringContent("yawn!") }, // retry
			(request) => throw new TaskCanceledException("timeout!"), // retry
			(request) => new HttpResponseMessage() { StatusCode = (HttpStatusCode)200 }, // NO retry
			(request) => new HttpResponseMessage() { StatusCode = (HttpStatusCode)200 }, // not used
			(request) => new HttpResponseMessage() { StatusCode = (HttpStatusCode)200 }  // not used
			);
		var retrySignalingOnConditionHandler = new RetrySignalingOnConditionHandler();
		var retryAttempt = -1;
		var delegateHandler = new DelegateHandler(
			(request, ct) =>
			{
				retryAttempt++;
				return Task.CompletedTask;
			});
		var retryHandler = new ExponentialBackoffWithJitterRetryOnSignalHandler(
			new RetrySettings
			{
				RetryCount = 5,
				RetryDelayInMilliseconds = 0,
			});

		using var invoker = HttpMessageInvokerFactory.Create(
			retryHandler, delegateHandler, retrySignalingOnConditionHandler, shortCircuitingCannedActionsHandler);

		using var requestMessage = fixture.Create<HttpRequestMessage>();
		using var _ = await invoker.SendAsync(requestMessage, CancellationToken.None);

		Assert.AreEqual(3, retryAttempt);
	}

	[Test]
	public async Task Does_Not_Retry_If_No_Signal()
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		var shortCircuitingCannedActionsHandler = new ShortCircuitingCannedActionsHandler(
			(request) => new HttpResponseMessage() { StatusCode = (HttpStatusCode)200 } // NO retry
			);
		var retrySignalingOnConditionHandler = new RetrySignalingOnConditionHandler();
		var retryAttempt = -1;
		var delegateHandler = new DelegateHandler(
			(request, ct) =>
			{
				retryAttempt++;
				return Task.CompletedTask;
			});
		var retryHandler = new ExponentialBackoffWithJitterRetryOnSignalHandler(
			new RetrySettings
			{
				RetryCount = 5,
				RetryDelayInMilliseconds = 0,
			});

		using var invoker = HttpMessageInvokerFactory.Create(
			retryHandler, delegateHandler, retrySignalingOnConditionHandler, shortCircuitingCannedActionsHandler);

		using var requestMessage = fixture.Create<HttpRequestMessage>();
		using var _ = await invoker.SendAsync(requestMessage, CancellationToken.None);

		Assert.AreEqual(0, retryAttempt);
	}

	[Test]
	public async Task When_0_Retries_PollyRetryAttempt_Property_Is_Not_Present()
	{
		var fixture = new Fixture().Customize(new AutoMoqCustomization());

		var shortCircuitingCannedActionsHandler = new ShortCircuitingCannedActionsHandler(
			(request) => new HttpResponseMessage() { StatusCode = (HttpStatusCode)404 });
		var retrySignalingOnConditionHandler = new RetrySignalingOnConditionHandler();
		var retryHandler = new ExponentialBackoffWithJitterRetryOnSignalHandler(
			new RetrySettings
			{
				RetryCount = 0,
				RetryDelayInMilliseconds = 0,
			});

		using var invoker = HttpMessageInvokerFactory.Create(
			retryHandler, retrySignalingOnConditionHandler, shortCircuitingCannedActionsHandler);

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

		var shortCircuitingCannedActionsHandler = new ShortCircuitingCannedActionsHandler(
			(request) => new HttpResponseMessage() { StatusCode = (HttpStatusCode)404 },
			(request) => new HttpResponseMessage() { StatusCode = (HttpStatusCode)200, Content = new StringContent("yawn!") },
			(request) => new HttpResponseMessage() { StatusCode = (HttpStatusCode)200 });
		var retrySignalingOnConditionHandler = new RetrySignalingOnConditionHandler();
		var retryHandler = new ExponentialBackoffWithJitterRetryOnSignalHandler(
			new RetrySettings
			{
				RetryCount = retryCount,
				RetryDelayInMilliseconds = 0,
			});

		using var invoker = HttpMessageInvokerFactory.Create(
			retryHandler, retrySignalingOnConditionHandler, shortCircuitingCannedActionsHandler);

		using var requestMessage = fixture.Create<HttpRequestMessage>();
		using var _ = await invoker.SendAsync(requestMessage, CancellationToken.None);

#pragma warning disable CS0618 // Type or member is obsolete
		Assert.AreEqual(retryCount, requestMessage.Properties[RequestProperties.PollyRetryAttempt]);
#pragma warning restore CS0618 // Type or member is obsolete
	}
}
