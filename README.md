MegaApiClient
=============

C# library to access http://mega.co.nz API

This library is based on highly valuable articles from http://julien-marchand.fr



Usage example:
---
```
MegaApiClient client = new MegaApiClient();

client.Login("megaclient@yopmail.com", "megaclient");
var nodes = client.GetNodes();

Node root = nodes.Single(n => n.Type == NodeType.Root);
Node myFolder = client.CreateFolder("Upload", root);

Node myFile = client.Upload("MyFile.ext", myFolder);

Uri downloadUrl = client.GetDownloadLink(myFile);
Console.WriteLine(downloadUrl);
```


API functions:
---
```
void Login(string email, string password)
void LoginAnonymous()
void Logout()

IEnumerable<Node> GetNodes()
Node CreateFolder(string name, Node parent)
void Delete(Node node, bool moveToTrash = true)
Node Move(Node node, Node destinationParentNode)

Uri GetDownloadLink(Node node)
void DownloadFile(Node node, string outputFile)
Stream Download(Node node)

Node Upload(string filename, Node parent)
Node Upload(Stream stream, string name, Node parent)
```