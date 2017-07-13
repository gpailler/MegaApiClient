using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

[assembly: TestCollectionOrderer("CG.Web.MegaApiClient.Tests.CustomTestCollectionOrderer", "MegaApiclient.Tests")]
[assembly: TestCaseOrderer("CG.Web.MegaApiClient.Tests.CustomTestCaseOrderer", "MegaApiclient.Tests")]

namespace CG.Web.MegaApiClient.Tests
{
	public class CustomTestCollectionOrderer : ITestCollectionOrderer
	{
		public IEnumerable<ITestCollection> OrderTestCollections(IEnumerable<ITestCollection> testCollections)
		{
      return testCollections.OrderBy(x => x.DisplayName);
		}
	}

	public class CustomTestCaseOrderer : ITestCaseOrderer
	{
		public IEnumerable<TTestCase> OrderTestCases<TTestCase>(IEnumerable<TTestCase> testCases)
			where TTestCase : ITestCase
		{
      return testCases.OrderBy(x => x.DisplayName);
		}
	}
}
