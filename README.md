MegaApiClient
=============

[![Build Status](https://travis-ci.org/gpailler/MegaApiClient.svg?branch=develop)](https://travis-ci.org/gpailler/MegaApiClient)
[![NuGet version](https://img.shields.io/nuget/v/MegaApiClient.svg)](https://img.shields.io/nuget/v/MegaApiClient.svg)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](https://img.shields.io/badge/license-MIT-blue.svg)

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
void Login(AuthInfos authInfos)
void LoginAnonymous()
void Logout()

IEnumerable<Node> GetNodes()
IEnumerable<Node> GetNodes(Node parent)
Node CreateFolder(string name, Node parent)
void Delete(Node node, bool moveToTrash = true)
Node Move(Node node, Node destinationParentNode)

Uri GetDownloadLink(Node node)
void DownloadFile(Node node, string outputFile)
void DownloadFile(Uri uri, string outputFile)
Stream Download(Node node)
Stream Download(Uri uri)

Node Upload(string filename, Node parent)
Node Upload(Stream stream, string name, Node parent)

static AuthInfos GenerateAuthInfos(string email, string password)
```
