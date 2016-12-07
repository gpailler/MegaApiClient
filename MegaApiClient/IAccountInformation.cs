using System.Collections.Generic;
namespace CG.Web.MegaApiClient
{
  public interface IAccountInformation
  {
    long TotalQuota { get; }

    long UsedQuota { get; }

    Dictionary<string, int[]> StorageMetrics { get; }
  }
}