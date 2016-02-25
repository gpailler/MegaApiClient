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
using System.Threading.Tasks;

namespace CG.Web.MegaApiClient
{
    public partial interface IMegaApiClient
    {
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
        
        Task<Stream> DownloadAsync(INode node, IProgress<double> progress);

        Task<Stream> DownloadAsync(Uri uri, IProgress<double> progress);

        Task DownloadFileAsync(INode node, string outputFile, IProgress<double> progress);

        Task DownloadFileAsync(Uri uri, string outputFile, IProgress<double> progress);

        Task<INode> UploadAsync(Stream stream, string name, INode parent, IProgress<double> progress);

        Task<INode> UploadFileAsync(string filename, INode parent, IProgress<double> progress);

        Task<INodePublic> GetNodeFromLinkAsync(Uri uri);
    }
}