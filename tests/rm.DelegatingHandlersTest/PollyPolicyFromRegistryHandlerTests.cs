using System.Net;
using AutoFixture;
using AutoFixture.AutoMoq;
using NUnit.Framework;
using Polly;
using Polly.Registry;
using rm.DelegatingHandlers;

namespace rm.DelegatingHandlersTest
{
	[TestFixture]
	public class PollyPolicyFromRegistryHandlerTests
	{
		[Test]
		public async Task Executes_Policy_NoOp()
		{
			var fixture = new Fixture().Customize(new AutoMoqCustomization());

			var noOpPolicyKey = "noOp";
			var noOpPolicy = Policy.NoOpAsync<HttpResponseMessage>();
			var policyRegistry =
				new PolicyRegistry
				{
					{ noOpPolicyKey, noOpPolicy },
				};
			var pollyPolicyFromRegistryHandler =
				new PollyPolicyFromRegistryHandler(
					policyRegistry,
					new PollyPolicyFromRegistryHandlerSettings
					{
						PolicyKey = noOpPolicyKey,
					});

			using var invoker = HttpMessageInvokerFactory.Create(
				fixture.Create<HttpMessageHandler>(), pollyPolicyFromRegistryHandler);

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
			var retryPolicyKey = "retry";
			var noOpPolicy = Policy.NoOpAsync<HttpResponseMessage>();
			var policyRegistry =
				new PolicyRegistry
				{
					{ retryPolicyKey, retryPolicy },
				};
			var pollyPolicyFromRegistryHandler =
				new PollyPolicyFromRegistryHandler(
					policyRegistry,
					new PollyPolicyFromRegistryHandlerSettings
					{
						PolicyKey = retryPolicyKey,
					});
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
				pollyPolicyFromRegistryHandler, delegateHandler, shortCircuitingResponseHandler);

			using var requestMessage = fixture.Create<HttpRequestMessage>();
			using var response = await invoker.SendAsync(requestMessage, CancellationToken.None);

			Assert.AreEqual(1 + retryCount, i);
		}
	}
}
