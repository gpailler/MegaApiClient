using System;
using System.Collections.Generic;
using System.Linq;
using CG.Web.MegaApiClient.Tests.Context;
using Xunit;
using Xunit.Abstractions;

namespace CG.Web.MegaApiClient.Tests
{
  public abstract class TestsBase : IDisposable
  {
    protected readonly ITestContext context;

    protected TestsBase(ITestContext context, ITestOutputHelper testOutputHelper)
    {
      this.context = context;
      this.context.SetLogger(testOutputHelper);

      if (this.context.Client.IsLoggedIn)
      {
        this.SanitizeStorage();
      }
    }

    public virtual void Dispose()
    {
      this.context.ClearLogger();
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

    protected void SanitizeStorage(IEnumerable<string> protectedNodes = null)
    {
      IEnumerable<INode> nodes = this.context.Client.GetNodes().ToArray();

      IEnumerable<INode> nodesToRemove = nodes.Where(x => this.IsProtectedNode(protectedNodes ?? this.context.ProtectedNodes, x) == false);
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

      Assert.Equal((protectedNodes ?? this.context.ProtectedNodes).Count(), this.context.Client.GetNodes().Count());
    }

    private bool IsProtectedNode(IEnumerable<string> protectedNodes, INode node)
    {
      return node.Type == NodeType.Inbox
        || node.Type == NodeType.Root
        || node.Type == NodeType.Trash
        || protectedNodes.Any(x => x == node.Id);
    }
  }
}
