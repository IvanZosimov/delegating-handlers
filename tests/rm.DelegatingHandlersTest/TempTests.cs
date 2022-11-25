using System.Diagnostics;
using NUnit.Framework;

namespace rm.DelegatingHandlersTest
{
	public class TempTests
	{
		[Test]
		[TestCase(25)]
		[TestCase(5)]
		[TestCase(0)]
		[TestCase(50)]
		[TestCase(100)]
		public async Task __(int delayInMilliseconds)
		{
			const int iterations = 10;
			for (int i = 0; i < iterations; i++)
			{
				var stopwatch = Stopwatch.StartNew();
				await Task.Delay(delayInMilliseconds).ConfigureAwait(false);
				stopwatch.Stop();

				Assert.GreaterOrEqual(stopwatch.ElapsedMilliseconds, delayInMilliseconds);
			}
		}
	}
}
