using System;
using System.Collections.Generic;
using Xunit;

namespace CG.Web.MegaApiClient.Tests.Context
{
  using System.Dynamic;
  using System.IO;
  using Newtonsoft.Json;

  [CollectionDefinition(nameof(AuthenticatedTestContext))]
  public class AuthenticatedLoginTestsCollection : ICollectionFixture<AuthenticatedTestContext>, ICollectionFixture<AuthenticatedAsyncTestContext> { }

  public class AuthenticatedTestContext : TestContext, IDisposable
  {
    internal static readonly string InputsJson = File.ReadAllText("Data\\inputs.json");

    static AuthenticatedTestContext()
    {
      Inputs = JsonConvert.DeserializeObject<ExpandoObject>(InputsJson);
    }

    private const string MegaApiClientPasswordEnvironment = "MEGAAPICLIENT_PASSWORD";
    internal static readonly string Password = Environment.GetEnvironmentVariable(MegaApiClientPasswordEnvironment);

    internal static dynamic Inputs { get; }

    public virtual void Dispose()
    {
      Client.Logout();
    }

    protected override void ConnectClient(IMegaApiClient client)
    {
      Assert.False(string.IsNullOrEmpty(Password), $"Environment variable {MegaApiClientPasswordEnvironment} not set.");
      client.Login(Inputs.UsernameAccount, Password);
    }

    protected override IEnumerable<string> GetProtectedNodes()
    {
      return new string[]
      {
        Inputs.Root.Id,
        Inputs.Trash.Id,
        Inputs.Inbox.Id,
        Inputs.SharedFolder.Id,
        Inputs.SharedSubFolder.Id,
        Inputs.SharedFile.Id,
        Inputs.SharedFileUpSideDown.Id,
        Inputs.SampleZipFile.Id,
        Inputs.SharedRemoteFolder.Id,
        Inputs.SharedRemoteSubFolder.Id,
        Inputs.SharedRemoteSubSubFolder.Id,
        Inputs.SharedRemoteFile.Id,
        Inputs.SharedRemoteSubFile.Id
      };
    }

    protected override IEnumerable<string> GetPermanentNodes()
    {
      return new string[] { Inputs.SharedFolder.Id, Inputs.SampleZipFile.Id };
    }
  }
}
