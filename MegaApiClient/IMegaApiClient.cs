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

#if (NET45)
    using System.Threading.Tasks;
#endif

namespace CG.Web.MegaApiClient
{
    public interface IMegaApiClient
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

        INode Upload(string filename, INode parent);

        INode Upload(Stream stream, string name, INode parent);

        INode Move(INode node, INode destinationParentNode);

        #if (NET45)

        Task LoginAsync(string email, string password);

        Task LoginAsync(MegaApiClient.AuthInfos authInfos);

        Task LoginAnonymousAsync();

        Task LogoutAsync();

        Task<IEnumerable<INode>> GetNodesAsync();

        Task<IEnumerable<INode>> GetNodesAsync(INode parent);

        Task<INode> CreateFolderAsync(string name, INode parent);

        Task DeleteAsync(INode node, bool moveToTrash = true);

        Task<INode> MoveAsync(INode sourceNode, INode destinationParentNode);

        Task<Uri> GetDownloadLinkAsync(INode node);

        Task DownloadFileAsync(INode node, string outputFile, IProgress<int> progress);

        Task DownloadFileAsync(Uri uri, string outputFile, IProgress<int> progress);

        Task<INode> UploadAsync(string filename, INode parent, IProgress<int> progress);

        Task<INode> UploadAsync(Stream stream, string name, INode parent, IProgress<int> progress);

        Task<INodePublic> GetNodeFromLinkAsync(Uri uri);

        #endif
    }
}