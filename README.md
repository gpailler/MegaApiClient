MegaApiClient
=============

[![Build status](https://ci.appveyor.com/api/projects/status/a87jre98xr1wiryt/branch/develop?svg=true)](https://ci.appveyor.com/project/gpailler/megaapiclient/branch/develop)
[![Coverage Status](https://coveralls.io/repos/gpailler/MegaApiClient/badge.svg?branch=develop)](https://coveralls.io/r/gpailler/MegaApiClient?branch=develop)
[![NuGet version](https://badge.fury.io/nu/MegaApiClient.svg)](https://badge.fury.io/nu/MegaApiClient)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/gpailler/MegaApiClient/blob/develop/LICENSE)

C# library to access http://mega.co.nz API


Installation:
---
MegaApiClient is available on [NuGet](https://www.nuget.org/packages/MegaApiClient)
or from the [releases](https://github.com/gpailler/MegaApiClient/releases) section   (NewtonSoft.Json 6.0.8 or higher is required)


Usage example:
---
```csharp
MegaApiClient client = new MegaApiClient();

client.Login("megaclient@yopmail.com", "megaclient");
var nodes = client.GetNodes();

INode root = nodes.Single(n => n.Type == NodeType.Root);
INode myFolder = client.CreateFolder("Upload", root);

INode myFile = client.UploadFile("MyFile.ext", myFolder);

Uri downloadUrl = client.GetDownloadLink(myFile);
Console.WriteLine(downloadUrl);
```


API functions:
---
```csharp
void Login(string email, string password)
void Login(AuthInfos authInfos)
void LoginAnonymous()
void Logout()

IEnumerable<INode> GetNodes()
IEnumerable<INode> GetNodes(INode parent)
INode CreateFolder(string name, INode parent)
void Delete(INode node, bool moveToTrash = true)
INode Move(INode node, INode destinationParentNode)
INodePublic GetNodeFromLink(Uri uri)

Uri GetDownloadLink(INode node)
void DownloadFile(INode node, string outputFile)
void DownloadFile(Uri uri, string outputFile)
Stream Download(INode node)
Stream Download(Uri uri)

INode UploadFile(string filename, INode parent)
INode Upload(Stream stream, string name, INode parent)

static AuthInfos GenerateAuthInfos(string email, string password)
```

API async functions (.Net 4.5 version)
---
```csharp
Task LoginAsync(string email, string password);
Task LoginAsync(MegaApiClient.AuthInfos authInfos);
Task LoginAnonymousAsync();
Task LogoutAsync();

Task<IEnumerable<INode>> GetNodesAsync();
Task<IEnumerable<INode>> GetNodesAsync(INode parent);
Task<INode> CreateFolderAsync(string name, INode parent);
Task DeleteAsync(INode node, bool moveToTrash = true);
Task<INode> MoveAsync(INode sourceNode, INode destinationParentNode);
Task<INodePublic> GetNodeFromLinkAsync(Uri uri);

Task<Uri> GetDownloadLinkAsync(INode node);
Task DownloadFileAsync(INode node, string outputFile, IProgress<double> progress);
Task DownloadFileAsync(Uri uri, string outputFile, IProgress<double> progress);
Task<Stream> DownloadAsync(INode node, IProgress<double> progress);
Task<Stream> DownloadAsync(Uri uri, IProgress<double> progress);

Task<INode> UploadFileAsync(string filename, INode parent, IProgress<double> progress);
Task<INode> UploadAsync(Stream stream, string name, INode parent, IProgress<double> progress);
```
