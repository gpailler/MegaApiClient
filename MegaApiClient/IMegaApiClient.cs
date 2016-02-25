#region License

/*
The MIT License (MIT)

Copyright (c) 2015 Gregoire Pailler

Permission is hereby granted, free of charge, to any person obtaining a copy of
this software and associated documentation files (the "Software"), to deal in
the Software without restriction, including without limitation the rights to
use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
the Software, and to permit persons to whom the Software is furnished to do so,
subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

#endregion

using System;
using System.Collections.Generic;
using System.IO;

namespace CG.Web.MegaApiClient
{
    public partial interface IMegaApiClient
    {
        void Login(string email, string password);

        void Login(MegaApiClient.AuthInfos authInfos);

        void LoginAnonymous();

        void Logout();

        IEnumerable<INode> GetNodes();

        IEnumerable<INode> GetNodes(INode parent);

        void Delete(INode node, bool moveToTrash = true);

        INode CreateFolder(string name, INode parent);

        Uri GetDownloadLink(INode node);

        void DownloadFile(INode node, string outputFile);

        void DownloadFile(Uri uri, string outputFile);

        Stream Download(INode node);

        Stream Download(Uri uri);

        INodePublic GetNodeFromLink(Uri uri);

        INode UploadFile(string filename, INode parent);

        INode Upload(Stream stream, string name, INode parent);

        INode Move(INode node, INode destinationParentNode);
    }
}