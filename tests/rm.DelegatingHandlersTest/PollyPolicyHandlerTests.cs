using System.Net;
using AutoFixture;
using AutoFixture.AutoMoq;
using NUnit.Framework;
using Polly;
using rm.DelegatingHandlers;

namespace rm.DelegatingHandlersTest
{
	[TestFixture]
	public class PollyPolicyHandlerTests
	{
		[Test]
		public async Task Executes_Policy_NoOp()
		{
			var fixture = new Fixture().Customize(new AutoMoqCustomization());

			var noOpPolicy = Policy.NoOpAsync<HttpResponseMessage>();
			var pollyPolicyHandler = new PollyPolicyHandler(noOpPolicy);

			using var invoker = HttpMessageInvokerFactory.Create(
				fixture.Create<HttpMessageHandler>(), pollyPolicyHandler);

			using var requestMessage = fixture.Create<HttpRequestMessage>();
			using var response = await invoker.SendAsync(requestMessage, CancellationToken.None);

			Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
		}

		[Test]
		public async Task Executes_Policy_Retry()
		{
			var fixture = new Fixture().Customize(new AutoMoqCustomization());

			var retryCount = 2;
			var retryPolicy = Policy
				.HandleResult<HttpResponseMessage>(r => true)
				.RetryAsync(retryCount);
			var pollyPolicyHandler = new PollyPolicyHandler(retryPolicy);
			var i = 0;
			var delegateHandler = new DelegateHandler(
				preDelegate: (request, ct) =>
				{
					i++;
					return Task.CompletedTask;
				});
			var shortCircuitingResponseHandler = new ShortCircuitingResponseHandler(
				new ShortCircuitingResponseHandlerSettings
				{
					StatusCode = (HttpStatusCode)200,
				});

			using var invoker = HttpMessageInvokerFactory.Create(
				pollyPolicyHandler, delegateHandler, shortCircuitingResponseHandler);

			using var requestMessage = fixture.Create<HttpRequestMessage>();
			using var response = await invoker.SendAsync(requestMessage, CancellationToken.None);

			Assert.AreEqual(1 + retryCount, i);
		}
	}
}
