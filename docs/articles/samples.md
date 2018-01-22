---
uid: samples
---

### List all files / folders on your Mega account
```csharp
void Main()
{
  var client = new MegaApiClient();
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
void Main()
{
  var client = new MegaApiClient();
  client.LoginAnonymous();

  Uri fileLink = new Uri("https://mega.nz/#!bkwkHC7D!AWJuto8_fhleAI2WG0RvACtKkL_s9tAtvBXXDUp2bQk");
  INodeInfo node = client.GetNodeFromLink(fileLink);

  Console.WriteLine($"Downloading {node.Name}");
  client.DownloadFile(fileLink, node.Name);

  client.Logout();
}
```


### Download folder content from a Mega link
```csharp
void Main()
{
  var client = new MegaApiClient();
  client.LoginAnonymous();

  Uri folderLink = new Uri("https://mega.nz/#F!e1ogxQ7T!ee4Q_ocD1bSLmNeg9B6kBw");
  IEnumerable<INode> nodes = client.GetNodesFromLink(folderLink);
  foreach (INode node in nodes.Where(x => x.Type == NodeType.File))
  {
    Console.WriteLine($"Downloading {node.Name}");
    client.DownloadFile(node, node.Name);
  }

  client.Logout();
}
```


### Upload a file to your Mega account and retrieve public download link
```csharp
void Main()
{
  var client = new MegaApiClient();
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


