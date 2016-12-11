using System.Collections.Generic;
using System.Linq;
using CG.Web.MegaApiClient.Tests.Context;
using Xunit;

namespace CG.Web.MegaApiClient.Tests
{
  public abstract class TestsBase
  {
    protected readonly ITestContext context;

    protected TestsBase(ITestContext context)
    {
      this.context = context;
      this.SanitizeStorage();
    }

    protected INode GetNode(NodeType nodeType)
    {
      var node = this.context.Client.GetNodes().SingleOrDefault(x => x.Type == nodeType);
      Assert.NotNull(node);

      return node;
    }

    protected INode GetNode(string nodeId)
    {
      var node = this.context.Client.GetNodes().Single(x => x.Id == nodeId);
      Assert.NotNull(node);

      return node;
    }

    protected INode CreateFolderNode(INode parentNode, string name = "NodeName")
    {
      return this.context.Client.CreateFolder(name, parentNode);
    }

    private void SanitizeStorage()
    {
      IEnumerable<INode> nodes = this.context.Client.GetNodes().ToArray();

      IEnumerable<INode> nodesToRemove = nodes.Where(x => this.IsProtectedNode(x) == false);
      foreach (INode node in nodesToRemove)
      {
        try
        {
          this.context.Client.Delete(node, false);
        }
        catch (ApiException ex)
        {
          // Don't throw if node is already removed
          if (ex.ApiResultCode != ApiResultCode.AccessDenied)
          {
            throw;
          }
        }
      }

      Assert.Equal(this.context.ProtectedNodes.Count(), this.context.Client.GetNodes().Count());
    }

    private bool IsProtectedNode(INode node)
    {
      return node.Type == NodeType.Inbox
        || node.Type == NodeType.Root
        || node.Type == NodeType.Trash
        || this.context.ProtectedNodes.Any(x => x == node.Id);
    }

    //protected string GetAbsoluteFilePath(string relativeFilePath)
    //{
    //  return Path.Combine(NUnit.Framework.TestContext.CurrentContext.TestDirectory, relativeFilePath);
    //}
  }
}
