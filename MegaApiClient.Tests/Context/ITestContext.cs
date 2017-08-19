using System.Collections.Generic;
using Xunit.Abstractions;

namespace CG.Web.MegaApiClient.Tests.Context
{
  public interface ITestContext
  {
    IMegaApiClient Client { get; }

    IWebClient WebClient { get; }

    Options Options { get; }

    int WebTimeout { get; }

    IEnumerable<string> ProtectedNodes { get; }

    IEnumerable<string> PermanentRootNodes { get; }

    void AssignLogger(ITestOutputHelper testOutputHelper);
  }
}
