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
    protected readonly ITestContext Context;

    protected TestsBase(ITestContext context, ITestOutputHelper testOutputHelper)
    {
      Context = context;
      Context.SetLogger(testOutputHelper);

      if (Context.Client.IsLoggedIn)
      {
        SanitizeStorage();
      }
    }

    public virtual void Dispose()
    {
      Context.ClearLogger();
    }

    protected INode GetNode(NodeType nodeType)
    {
      var node = Context.Client.GetNodes().SingleOrDefault(x => x.Type == nodeType);
      Assert.NotNull(node);

      return node;
    }

    protected INode GetNode(string nodeId)
    {
      var node = Context.Client.GetNodes().Single(x => x.Id == nodeId);
      Assert.NotNull(node);

      return node;
    }

    protected INode CreateFolderNode(INode parentNode, string name = "NodeName")
    {
      return Context.Client.CreateFolder(name, parentNode);
    }

    protected void SanitizeStorage(IEnumerable<string> protectedNodes = null)
    {
      IEnumerable<INode> nodes = Context.Client.GetNodes().ToArray();

      var nodesToRemove = nodes.Where(x => IsProtectedNode(protectedNodes ?? Context.ProtectedNodes, x) == false);
      foreach (var node in nodesToRemove)
      {
        try
        {
          Context.Client.Delete(node, false);
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

      Assert.Equal((protectedNodes ?? Context.ProtectedNodes).Count(), Context.Client.GetNodes().Count());
    }

    private static bool IsProtectedNode(IEnumerable<string> protectedNodes, INode node)
    {
      return node.Type == NodeType.Inbox
        || node.Type == NodeType.Root
        || node.Type == NodeType.Trash
        || protectedNodes.Any(x => x == node.Id);
    }
  }
}
