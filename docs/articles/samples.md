---
uid: samples
---

### List all files / folders on your Mega account
```csharp
using CG.Web.MegaApiClient;

void Main()
{
  MegaApiClient client = new MegaApiClient();
  client.Login("username@domain.com", "passw0rd");

  // GetNodes retrieves all files/folders metadata from Mega
  // so this method can be time consuming
  IEnumerable<INode> nodes = client.GetNodes();

  INode parent = nodes.Single(n => n.Type == NodeType.Root);
  DisplayNodesRecursive(nodes, parent);

  client.Logout();
}

void DisplayNodesRecursive(IEnumerable<INode> nodes, INode parent, int level = 0)
{
  IEnumerable<INode> children = nodes.Where(x => x.ParentId == parent.Id);
  foreach (INode child in children)
  {
    string infos = $"- {child.Name} - {child.Size} bytes - {child.CreationDate}";
    Console.WriteLine(infos.PadLeft(infos.Length + level, '\t'));
    if (child.Type == NodeType.Directory)
    {
      DisplayNodesRecursive(nodes, child, level + 1);
    }
  }
}
```


### Download file from a Mega link
```csharp
using CG.Web.MegaApiClient;

void Main()
{
  MegaApiClient client = new MegaApiClient();
  client.LoginAnonymous();

  Uri fileLink = new Uri("https://mega.nz/file/W0UAgJaK#XOYyTETrIy8daz3_dw3fdh6Hh8EFEdrnbyoop1r9R6g");
  INode node = client.GetNodeFromLink(fileLink);

  Console.WriteLine($"Downloading {node.Name}");
  client.DownloadFile(fileLink, node.Name);

  client.Logout();
}
```


### Download folder content from a Mega link
```csharp
using CG.Web.MegaApiClient;

void Main()
{
  MegaApiClient client = new MegaApiClient();
  client.LoginAnonymous();

  Uri folderLink = new Uri("https://mega.nz/folder/e4diDZ7T#iJnegBO_m6OXBQp27lHCrg");
  IEnumerable<INode> nodes = client.GetNodesFromLink(folderLink);
  foreach (INode node in nodes.Where(x => x.Type == NodeType.File))
  {
    string parents = GetParents(node, nodes);
    Directory.CreateDirectory(parents);
    Console.WriteLine($"Downloading {parents}\\{node.Name}");
    client.DownloadFile(node, Path.Combine(parents, node.Name));
  }

  client.Logout();
}

string GetParents(INode node, IEnumerable<INode> nodes)
{
    List<string> parents = new List<string>();
    while (node.ParentId != null)
    {
        INode parentNode = nodes.Single(x => x.Id == node.ParentId);
        parents.Insert(0, parentNode.Name);
        node = parentNode;
    }

    return string.Join('\\', parents);
}
```


### Upload a file to your Mega account and retrieve public download link
```csharp
using CG.Web.MegaApiClient;

void Main()
{
  MegaApiClient client = new MegaApiClient();
  client.Login("username@domain.com", "passw0rd");

  IEnumerable<INode> nodes = client.GetNodes();

  INode root = nodes.Single(x => x.Type == NodeType.Root);
  INode myFolder = client.CreateFolder("Upload", root);

  INode myFile = client.UploadFile("MyFile.ext", myFolder);
  Uri downloadLink = client.GetDownloadLink(myFile);
  Console.WriteLine(downloadLink);

  client.Logout();
}
```


