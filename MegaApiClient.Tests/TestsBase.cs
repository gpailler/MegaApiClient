using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace CG.Web.MegaApiClient.Tests
{
    public abstract class TestsBase
    {
        private const string Username = "megaapiclient@yopmail.com";
        private const string Password = "megaapiclient";

        private readonly Options _options;

        protected MegaApiClient Client;

        [Flags]
        protected enum Options
        {
            None = 0,
            Login = 1 << 0,
            LoginAuthenticated = Login | 1 << 1,
            LoginAnonymous = Login | 1 << 2,
            Clean = LoginAuthenticated | 1 << 3
        }

        protected TestsBase(Options options)
        {
            this._options = options;
        }

        [SetUp]
        public void Setup()
        {
            this.Client = new MegaApiClient();
            if ((this._options & Options.LoginAuthenticated) == Options.LoginAuthenticated)
            {
                this.Client.Login(Username, Password);
            }

            if ((this._options & Options.LoginAnonymous) == Options.LoginAnonymous)
            {
                this.Client.LoginAnonymous();
            }

            if ((this._options & Options.Clean) == Options.Clean)
            {
                IEnumerable<INode> nodes = this.Client.GetNodes().ToArray();
                INode root = nodes.Single(x => x.Type == NodeType.Root);
                INode inbox = nodes.Single(x => x.Type == NodeType.Inbox);
                INode trash = nodes.Single(x => x.Type == NodeType.Trash);
                IEnumerable<INode> nodesToRemove = nodes.Where(x => x.ParentId == root.Id || x.ParentId == inbox.Id || x.ParentId == trash.Id);
                foreach (INode node in nodesToRemove)
                {
                    this.Client.Delete(node, false);
                }
            }
        }

        [TearDown]
        public void Teardown()
        {
            if ((this._options & Options.Login) == Options.Login)
            {
                this.Client.Logout();
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
    }
}
