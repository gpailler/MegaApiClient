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
or from the [releases](https://github.com/gpailler/MegaApiClient/releases) section
(NewtonSoft.Json 6.0.8 or higher is required)


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


Available API functions:
---
```csharp
// Authentication
void Login(string email, string password)
void Login(AuthInfos authInfos)
void LoginAnonymous()
void Logout()

// A AuthInfos object is a JSon serializable object containing encrypted password and key.
// It allows to store encrypted credentials in your application settings instead login and password
static AuthInfos GenerateAuthInfos(string email, string password)

// Gives total and used quota for the logged user
IAccountInformation GetAccountInformation()

// Nodes management
IEnumerable<INode> GetNodes()
IEnumerable<INode> GetNodes(INode parent)
INode CreateFolder(string name, INode parent)
void Delete(INode node, bool moveToTrash = true)
INode Move(INode node, INode destinationParentNode)
INode Rename(INode node, string newName)
INodePublic GetNodeFromLink(Uri uri)

// Download
Uri GetDownloadLink(INode node)
void DownloadFile(INode node, string outputFile)
void DownloadFile(Uri uri, string outputFile)
Stream Download(INode node)
Stream Download(Uri uri)

// Upload
INode UploadFile(string filename, INode parent)
INode Upload(Stream stream, string name, INode parent)
```

Async methods and download/upload progression:
---
- If your application targets .Net 4.5 or higher, async versions of the above methods are available (suffixed by `Async`. For example `Task LoginAsync(string email, string password)`). The Download/Upload methods have an additional `IProgress<double>` argument to be notified about operation progression.
- If your application targets .Net 3.5 or 4.0, you can retrieve Download/Upload progression by implementing your own stream. You can find an example on the [wiki](../../wiki/Retrieve-progression-of-an-upload-or-download).

Customization
---
- MegaApiClient constructor can use a optional IWebClient implementation to customize network layer (retry, timeout, transfer cancellation...).
- The default download/upload buffer size is set to 8192 bytes and can be changed by editing `MegaApiClient.BufferSize` instance variable. This can improve transfers if you have high bandwidth (but you have less progression reports).
- Files are splitted in chunks and uploaded chunk by chunk (to support large uploads). MegaApiClient tries to pack chunks in 1MB uploads fragments by default. You can edit `MegaApiClient.ChunksPackSize` to specify a custom value to pack more or less chunks in a single upload.
- For .Net 4.5 and higher, the progression in Download/Upload methods is reported on each 50KB downloaded or uploaded block. This can be changed by editing `MegaApiClient.ReportProgressChunkSize` instance variable. This value should be higher or equals to `MegaApiClient.BufferSize` or you will have a less smooth progression report.

Donations :gift:
---
If you like this project, but don't know anything about PR, Git or cryptography, don't worry! You can still contribute by a donation :smile:

https://www.paypal.me/gpailler
