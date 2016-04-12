using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace CG.Web.MegaApiClient.Tests
{
    public abstract class TestsBase
    {
        private const string Username = "megaapiclient@yopmail.com";
        private const string Password = "megaapiclient";
        private const int WebTimeout = 60000;

        protected const int MaxRetry = 5;

        /*
        Storage layout

        +-Root                                      (bsxVBKLL)
        |   +-SharedFolder                          (SsRDGA4Y) (Outgoing Share)
        |       |-SharedFile.jpg                    (KshlkSIK)
        |       +-SharedSubFolder                   (u95ATDYA) (Outgoing Share)
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
            "SsRDGA4Y", // SharedFolder
        };

        private readonly string[] _permanentFoldersNodes =
        {
            "u95ATDYA", // SharedSubFolder
        };

        private readonly string[] _permanentRemoteFoldersNodes =
        {
            "b0I0QDhA", // SharedRemoteFolder
            "KhZSWI7C", // SharedRemoteSubFolder
            "z1YCibCT", // SharedRemoteSubSubFolder
        };

        private readonly string[] _permanentFilesNodes =
        {
            "KshlkSIK", // SharedFile.jpg
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
            Clean =  1 << 3,
            AsyncWrapper = 1 << 4
        }

        protected TestsBase(Options options)
        {
            this._options = options;
        }

        [SetUp]
        public void Setup()
        {
            this.Client = new MegaApiClient(new WebClient(WebTimeout));
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
                this.ExecuteWithRetry(this.SanitizeStorage, MaxRetry);
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
            get { return this._permanentFilesNodes[0]; }
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

        private void ExecuteWithRetry(Action action, int maxRetry)
        {
            Exception lastException = null;
            int remainingRetry = maxRetry;
            while (remainingRetry > 0)
            {
                try
                {
                    action();
                    return;
                }
                catch (ApiException ex)
                {
                    lastException = ex;
                    if (ex.ApiResultCode == ApiResultCode.BadSessionId && this._options.HasFlag(Options.LoginAuthenticated))
                    {
                        this.ReconnectOnException(ex, maxRetry);
                    }
                }
                catch (NotSupportedException ex)
                {
                    lastException = ex;
                    if (this._options.HasFlag(Options.LoginAuthenticated))
                    {
                        this.ReconnectOnException(ex, maxRetry);
                    }
                }
                catch (Exception e)
                {
                    lastException = e;
                }

                remainingRetry--;
            }

            throw lastException;
        }

        private void ReconnectOnException(Exception exception, int remainingRetry)
        {
            Console.WriteLine(exception.Message);
            Console.WriteLine("New login attempt (remaining retry: {0})", remainingRetry);
            Thread.Sleep(500);
            this.Client.Logout();
            this.Client.Login(Username, Password);
        }
    }
}
