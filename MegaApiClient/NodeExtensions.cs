namespace CG.Web.MegaApiClient
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
#if !NET40
  using System.Threading.Tasks;
#endif

  public static class NodeExtensions
  {
    public static long GetFolderSize(this INode node, IMegaApiClient client)
    {
      var allNodes = client.GetNodes();
      return node.GetFolderSize(allNodes);
    }

    public static long GetFolderSize(this INode node, IEnumerable<INode> allNodes)
    {
      if (node.Type == NodeType.File)
      {
        throw new InvalidOperationException("node is not a Directory");
      }

      long folderSize = 0;
      var children = allNodes.Where(x => x.ParentId == node.Id);
      foreach (var childNode in children)
      {
        if (childNode.Type == NodeType.File)
        {
          folderSize += childNode.Size;
        }
        else if (childNode.Type == NodeType.Directory)
        {
          var size = childNode.GetFolderSize(allNodes);
          folderSize += size;
        }
      }

      return folderSize;
    }

#if !NET40

    public static async Task<long> GetFolderSizeAsync(this INode node, IMegaApiClient client)
    {
      var allNodes = await client.GetNodesAsync();
      return await node.GetFolderSizeAsync(allNodes);
    }

    public static async Task<long> GetFolderSizeAsync(this INode node, IEnumerable<INode> allNodes)
    {
      if (node.Type == NodeType.File)
      {
        throw new InvalidOperationException("node is not a Directory");
      }

      long folderSize = 0;
      var children = allNodes.Where(x => x.ParentId == node.Id);
      foreach (var childNode in children)
      {
        if (childNode.Type == NodeType.File)
        {
          folderSize += childNode.Size;
        }
        else if (childNode.Type == NodeType.Directory)
        {
          var size = await childNode.GetFolderSizeAsync(allNodes);
          folderSize += size;
        }
      }

      return folderSize;
    }

#endif
  }
}
