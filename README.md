MegaApiClient
=============

[![Build status](https://ci.appveyor.com/api/projects/status/a87jre98xr1wiryt/branch/develop?svg=true)](https://ci.appveyor.com/project/gpailler/megaapiclient/branch/develop)
[![Coverage Status](https://coveralls.io/repos/gpailler/MegaApiClient/badge.svg?branch=develop)](https://coveralls.io/r/gpailler/MegaApiClient?branch=develop)
[![NuGet version](https://img.shields.io/nuget/v/MegaApiClient.svg)](https://www.nuget.org/packages/MegaApiClient)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/gpailler/MegaApiClient/blob/develop/LICENSE)

C# library to access http://mega.co.nz API

This library is based on highly valuable articles from http://julien-marchand.fr



Usage example:
---
```
MegaApiClient client = new MegaApiClient();

client.Login("megaclient@yopmail.com", "megaclient");
var nodes = client.GetNodes();

INode root = nodes.Single(n => n.Type == NodeType.Root);
INode myFolder = client.CreateFolder("Upload", root);

INode myFile = client.Upload("MyFile.ext", myFolder);

Uri downloadUrl = client.GetDownloadLink(myFile);
Console.WriteLine(downloadUrl);
```


API functions:
---
```
void Login(string email, string password)
void Login(AuthInfos authInfos)
void LoginAnonymous()
void Logout()

IEnumerable<INode> GetNodes()
IEnumerable<INode> GetNodes(INode parent)
INode CreateFolder(string name, INode parent)
void Delete(INode node, bool moveToTrash = true)
INode Move(INode node, INode destinationParentNode)

Uri GetDownloadLink(INode node)
void DownloadFile(INode node, string outputFile)
void DownloadFile(Uri uri, string outputFile)
Stream Download(INode node)
Stream Download(Uri uri)

INode Upload(string filename, INode parent)
INode Upload(Stream stream, string name, INode parent)

static AuthInfos GenerateAuthInfos(string email, string password)
```
