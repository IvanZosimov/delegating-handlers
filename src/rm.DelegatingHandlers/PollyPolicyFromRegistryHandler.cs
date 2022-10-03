using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using Polly.Registry;

namespace rm.DelegatingHandlers
{
	/// <summary>
	/// Executes the polly policy fetched from policy registry.
	/// </summary>
	public class PollyPolicyFromRegistryHandler : DelegatingHandler
	{
		private readonly IAsyncPolicy<HttpResponseMessage> policy;

		/// <inheritdoc cref="PollyPolicyFromRegistryHandler" />
		public PollyPolicyFromRegistryHandler(
			PolicyRegistry policyRegistry,
			IPollyPolicyFromRegistryHandlerSettings pollyPolicyFromRegistryHandlerSettings)
		{
			_ = policyRegistry
				?? throw new ArgumentNullException(nameof(policyRegistry));
			_ = pollyPolicyFromRegistryHandlerSettings
				?? throw new ArgumentNullException(nameof(pollyPolicyFromRegistryHandlerSettings));

			policy = policyRegistry.Get<IAsyncPolicy<HttpResponseMessage>>(pollyPolicyFromRegistryHandlerSettings.PolicyKey);
		}

		protected override async Task<HttpResponseMessage> SendAsync(
			HttpRequestMessage request,
			CancellationToken cancellationToken)
		{
			return await policy.ExecuteAsync(
				async (ct) => await base.SendAsync(request, ct).ConfigureAwait(false),
				cancellationToken)
					.ConfigureAwait(false);
		}
	}

	public interface IPollyPolicyFromRegistryHandlerSettings
	{
		string PolicyKey { get; }
	}

	public class PollyPolicyFromRegistryHandlerSettings : IPollyPolicyFromRegistryHandlerSettings
	{
		public string PolicyKey { get; init; }
	}
}
