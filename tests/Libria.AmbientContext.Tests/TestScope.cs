namespace Libria.AmbientContext.Tests
{
	public class TestScope : BaseScope<TestScope, string>
	{
		public string ScopeValue
		{
			get { return ScopeData; }
			set { ScopeData = value; }
		}

		public TestScope(ScopeOption option = ScopeOption.Required) : base(null, option)
		{
		}
	}
}