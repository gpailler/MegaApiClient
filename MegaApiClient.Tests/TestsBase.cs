using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using Polly;

namespace CG.Web.MegaApiClient.Tests
{
  public abstract class TestsBase
  {
    private const string Username = "megaapiclient@yopmail.com";
    private const string Password = "megaapiclient";
    private const int WebTimeout = 30000;
    private const int MaxRetry = 3;

    /*
    Storage layout

    +-Root                                      (bsxVBKLL)
    |   +-SharedFolder                          (KoRAhTbQ) (Outgoing Share)
    |       |-SharedFile.jpg                    (eooj3IwY)
    |       +-SharedSubFolder                   (nxxWXJAb) (Outgoing Share)
    +-Trash                                     (j0wEGbTZ)
    +-Inbox                                     (zhITTbIJ)
    +-Contacts
        +-SharedRemoteFolder                    (b0I0QDhA) (Incoming Share)
            |-SharedRemoteFile.jpg              (e5wjkSJB)
            +-SharedRemoteSubFolder             (KhZSWI7C) (Incoming Share / Subfolder of SharedRemoteFolder)
                |-SharedRemoteSubFile.jpg       (HtonzYYY)
                +-SharedRemoteSubSubFolder      (z1YCibCT)

    */

    private readonly string[] _systemNodes =
    {
      "bsxVBKLL", // Root
      "j0wEGbTZ", // Trash
      "zhITTbIJ", // Inbox
    };

    private readonly string[] _permanentFoldersRootNodes =
    {
      "KoRAhTbQ" // SharedFolder
    };

    private readonly string[] _permanentFoldersNodes =
    {
      "nxxWXJAb", // SharedSubFolder
    };

    private readonly string[] _permanentRemoteFoldersNodes =
    {
      "b0I0QDhA", // SharedRemoteFolder
      "KhZSWI7C", // SharedRemoteSubFolder
      "z1YCibCT", // SharedRemoteSubSubFolder
    };

    private readonly string[] _permanentFilesNodes =
    {
      "eooj3IwY", // SharedFile.jpg
    };

    private readonly string[] _permanentRemoteFilesNodes =
    {
      "e5wjkSJB", // SharedRemoteFile.jpg
      "HtonzYYY", // SharedRemoteSubFile.jpg
    };

    private readonly Options _options;

    protected IMegaApiClient Client;

    [Flags]
    protected enum Options
    {
      None = 0,
      Login = 1 << 0,
      LoginAuthenticated = Login | 1 << 1,
      LoginAnonymous = Login | 1 << 2,
      Clean = 1 << 3,
      AsyncWrapper = 1 << 4
    }

    protected TestsBase(Options options)
    {
      this._options = options;
    }

    [SetUp]
    public void Setup()
    {
      this.Client = new MegaApiClient(new TestWebClient(new WebClient(WebTimeout), MaxRetry));
      if (this._options.HasFlag(Options.AsyncWrapper))
      {
        this.Client = new MegaApiClientAsyncWrapper(this.Client);
      }

      if (this._options.HasFlag(Options.LoginAuthenticated))
      {
        this.Client.Login(Username, Password);
      }

      if (this._options.HasFlag(Options.LoginAnonymous))
      {
        this.Client.LoginAnonymous();
      }

      if (this._options.HasFlag(Options.Clean))
      {
        Policy
          .Handle<ApiException>(ex => ex.ApiResultCode == ApiResultCode.BadSessionId && this._options.HasFlag(Options.LoginAuthenticated))
          .Or<NotSupportedException>(ex => this._options.HasFlag(Options.LoginAuthenticated))
          .WaitAndRetry(MaxRetry, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), this.ReconnectOnException)
          .Execute(this.SanitizeStorage);
      }
    }

    [TearDown]
    public void Teardown()
    {
      if (this._options.HasFlag(Options.Clean))
      {
        this.SanitizeStorage();
      }

      if (this._options.HasFlag(Options.Login))
      {
        this.Client.Logout();
      }
    }

    protected string PermanentFile
    {
      get
      {
        return this._permanentFilesNodes[0];
      }
    }

    protected int SystemNodesCount
    {
      get
      {
        return this._systemNodes.Length;
      }
    }

    protected int PermanentNodesCount
    {
      get
      {
        return this._options.HasFlag(Options.LoginAuthenticated)
          ? this._permanentFoldersRootNodes.Length
            + this._permanentFilesNodes.Length
            + this._permanentFilesNodes.Length
            + this._permanentRemoteFoldersNodes.Length
            + this._permanentRemoteFilesNodes.Length
          : 0;
      }
    }

    protected int PermanentFoldersRootNodesCount
    {
      get
      {
        return this._options.HasFlag(Options.LoginAuthenticated)
          ? this._permanentFoldersRootNodes.Length
          : 0;
      }
    }

    protected static IEnumerable<ITestCaseData> GetCredentials()
    {
      yield return new TestCaseData(Username, Password);
    }

    protected INode GetNode(NodeType nodeType)
    {
      return this.Client.GetNodes().Single(x => x.Type == nodeType);
    }

    protected INode CreateFolderNode(INode parentNode, string name = "NodeName")
    {
      return this.Client.CreateFolder(name, parentNode);
    }

    protected string GetAbsoluteFilePath(string relativeFilePath)
    {
      return Path.Combine(TestContext.CurrentContext.TestDirectory, relativeFilePath);
    }

    private void SanitizeStorage()
    {
      IEnumerable<INode> nodes = this.Client.GetNodes().ToArray();

      IEnumerable<INode> nodesToRemove = nodes.Where(x => this.IsProtectedNode(x) == false);
      foreach (INode node in nodesToRemove)
      {
        this.Client.Delete(node, false);
      }

      Assert.That(
        this.Client.GetNodes().ToArray(),
        Has.Length.EqualTo(this.SystemNodesCount + this.PermanentNodesCount));
    }

    private bool IsProtectedNode(INode node)
    {
      return this._systemNodes
        .Concat(this._permanentFoldersRootNodes)
        .Concat(this._permanentFoldersNodes)
        .Concat(this._permanentFilesNodes)
        .Concat(this._permanentRemoteFoldersNodes)
        .Concat(this._permanentRemoteFilesNodes)
        .Any(x => x == node.Id);
    }

    private void ReconnectOnException(Exception exception, TimeSpan waitDelay)
    {
      Console.WriteLine(exception.Message);
      Console.WriteLine("New login attempt (wait delay: {0})", waitDelay.TotalSeconds);

      this.Client.Logout();
      this.Client.Login(Username, Password);
    }
  }
}
