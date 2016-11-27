using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Libria.AmbientContext.Tests
{
	[TestClass]
	public class ScopeTest
	{

		[TestMethod]
		public async Task TestSuppressScopeAsync()
		{
			var ct = CancellationToken.None;

			using (var scope = new TestScope())
			{
				TestScope.Current.ScopeValue = "RootScope";

				using (var nestedScope = new TestScope(ScopeOption.Suppress))
				{
					Assert.AreEqual(TestScope.Current, null);

					using (var scope3 = new TestScope())
					{
						await Task.Run(() => TestScope.Current.ScopeValue = "scope3", ct);
						Assert.AreEqual(TestScope.Current.ScopeValue, "scope3");
					}
				}

				Assert.AreEqual(TestScope.Current.ScopeValue, "RootScope");
			}
		}

		[TestMethod]
		public async Task TestRequiredScopeAsync()
		{
			var ct = CancellationToken.None;

			using (var scope = new TestScope())
			{
				TestScope.Current.ScopeValue = "RootScope";

				using (var nestedScope = new TestScope())
				{
					await Task.Delay(500, ct);
					Assert.AreEqual(TestScope.Current.ScopeValue, "RootScope");
					TestScope.Current.ScopeValue = "NestedScope";
				}

				Assert.AreEqual(TestScope.Current.ScopeValue, "NestedScope");
			}
		}

		[TestMethod]
		public async Task TestRequiresNewScopeAsync()
		{
			var ct = CancellationToken.None;

			using (var scope = new TestScope())
			{
				TestScope.Current.ScopeValue = "RootScope";

				using (var nestedScope = new TestScope(ScopeOption.RequiresNew))
				{
					Assert.AreEqual(TestScope.Current.ScopeValue, null);
					await Task.Run(() => TestScope.Current.ScopeValue = "NestedScope", ct);
				}

				Assert.AreEqual(TestScope.Current.ScopeValue, "RootScope");
			}
		}

	}
}
