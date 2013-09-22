MegaApiClient
=============

C# library to access http://mega.co.nz API

This library is based on highly valuable articles from http://julien-marchand.fr



Usage example:
---
```
MegaApiClient client = new MegaApiClient();

client.Login("megaclient@yopmail.com", "megaclient"); // or client.LoginAnonymous();
var nodes = client.GetNodes();

Node root = nodes.Single(n => n.Type == NodeType.Root);
Node myFolder = client.CreateFolder("Upload", root);

Node myFile = client.Upload("MyFile.ext", myFolder);

Uri downloadUrl = client.GetDownloadLink(myFile);
Console.WriteLine(downloadUrl);
```