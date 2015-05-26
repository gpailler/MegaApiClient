using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NUnit.Framework;

namespace CG.Web.MegaApiClient.Tests
{
    public abstract class TestsBase
    {
        private const string Username = "megaapiclient@yopmail.com";
        private const string Password = "megaapiclient";

        private readonly string[] _systemNodes =
        {
            "bsxVBKLL", // Root
            "j0wEGbTZ", // Trash
            "zhITTbIJ", // Inbox
        };

        private readonly string[] _permanentFoldersNodes =
        {
            "SsRDGA4Y", // SharedFolder
        };

        private readonly string[] _permanentFilesNodes =
        {
            "KshlkSIK" // SharedFile
        };

        private readonly Options _options;

        protected MegaApiClient Client;

        [Flags]
        protected enum Options
        {
            None = 0,
            Login = 1 << 0,
            LoginAuthenticated = Login | 1 << 1,
            LoginAnonymous = Login | 1 << 2,
            Clean =  1 << 3
        }

        protected TestsBase(Options options)
        {
            this._options = options;
        }

        [SetUp]
        public void Setup()
        {
            this.Client = new MegaApiClient(new PollyWebClient());
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
                this.SanitizeStorage();
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
                    ? this._permanentFoldersNodes.Length + this._permanentFilesNodes.Length
                    : 0;
            }
        }

        protected int PermanentFoldersNodesCount
        {
            get
            {
                return this._options.HasFlag(Options.LoginAuthenticated)
                    ? this._permanentFoldersNodes.Length
                    : 0;
            }
        }

        protected IEnumerable<ITestCaseData> GetCredentials()
        {
            yield return new TestCaseData(Username, Password);
        }

        protected INode GetNode(NodeType nodeType)
        {
            return this.Client.GetNodes().Single(x => x.Type == nodeType);
        }

        protected INode CreateFolderNode(INode parentNode, string name = "NodeName")
        {
            var createdNode = this.Client.CreateFolder(name, parentNode);

            return createdNode;
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
                .Concat(this._permanentFoldersNodes)
                .Concat(this._permanentFilesNodes)
                .Any(x => x == node.Id);
        }
    }
}
