using System;

namespace CG.Web.MegaApiClient
{
  public class UploadProgress : EventArgs
  {
    public UploadProgress(long progress, long uploaded, long fileSize)
    {
      Progress = progress;
      Uploaded = uploaded;
      FileSize = fileSize;
    }
    public long Progress { get; set; }
    public long Uploaded { get; set; }
    public long FileSize { get; set; }
  }
}
