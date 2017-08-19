namespace CG.Web.MegaApiClient
{
  using System.Collections.Generic;

  public interface IAccountInformation
  {
    long TotalQuota { get; }

    long UsedQuota { get; }

    IEnumerable<IStorageMetrics> Metrics { get; }
  }

  public interface IStorageMetrics
  {
    string NodeId { get; }

    long BytesUsed { get; }

    long FilesCount { get; }

    long FoldersCount { get; }
  }
}