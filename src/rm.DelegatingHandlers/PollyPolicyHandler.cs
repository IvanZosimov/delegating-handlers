using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Polly;

namespace rm.DelegatingHandlers
{
	/// <summary>
	/// Executes the polly policy.
	/// </summary>
	public class PollyPolicyHandler : DelegatingHandler
	{
		private readonly IAsyncPolicy<HttpResponseMessage> policy;

		/// <inheritdoc cref="PollyPolicyHandler" />
		public PollyPolicyHandler(IAsyncPolicy<HttpResponseMessage> policy)
		{
			this.policy = policy
				?? throw new ArgumentNullException(nameof(policy));
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
}
