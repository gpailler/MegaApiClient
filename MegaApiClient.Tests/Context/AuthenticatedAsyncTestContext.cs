﻿namespace CG.Web.MegaApiClient.Tests.Context
{
  public class AuthenticatedAsyncTestContext : AuthenticatedTestContext
  {
    public override void Dispose()
    {
      base.Dispose();
      ((MegaApiClientAsyncWrapper)Client).Dispose();
    }

    protected override IMegaApiClient CreateClient()
    {
      return new MegaApiClientAsyncWrapper(base.CreateClient());
    }
  }
}
