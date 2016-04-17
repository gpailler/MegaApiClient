namespace CG.Web.MegaApiClient
{
  public interface IAccountInformation
  {
    long TotalQuota { get; }

    long UsedQuota { get; }
  }
}