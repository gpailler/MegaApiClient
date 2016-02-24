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
using System.Threading;
using System.Threading.Tasks;

namespace CG.Web.MegaApiClient
{
    public partial class MegaApiClient : IMegaApiClient
    {
        #region Public async methods

        public Task LoginAsync(string email, string password)
        {
            return Task.Run(() => this.Login(email, password));
        }

        public Task LoginAsync(AuthInfos authInfos)
        {
            return Task.Run(() => this.Login(authInfos));
        }

        public Task LoginAnonymousAsync()
        {
            return Task.Run(() => this.LoginAnonymous());
        }

        public Task LogoutAsync()
        {
            return Task.Run(() => this.Logout());
        }

        public Task<IEnumerable<INode>> GetNodesAsync()
        {
            return Task<IEnumerable<INode>>.Run(() => this.GetNodes());
        }

        public Task<IEnumerable<INode>> GetNodesAsync(INode parent)
        {
            return Task<IEnumerable<INode>>.Run(() => this.GetNodes(parent));
        }

        public Task<INode> CreateFolderAsync(string name, INode parent)
        {
            return Task<INode>.Run(() => this.CreateFolder(name, parent));
        }

        public Task DeleteAsync(INode node, bool moveToTrash = true)
        {
            return Task.Run(() => this.Delete(node, moveToTrash));
        }

        public Task<INode> MoveAsync(INode sourceNode, INode destinationParentNode)
        {
            return Task<INode>.Run(() => this.Move(sourceNode, destinationParentNode));
        }

        public Task<Uri> GetDownloadLinkAsync(INode node)
        {
            return Task<Uri>.Run(() => this.GetDownloadLink(node));
        }

        public Task DownloadFileAsync(INode node, string outputFile, IProgress<int> progress)
        {
            return Task.Run(() =>
            {
                using (Stream stream = this.Download(node))
                {
                    this.SaveStreamReportProgress(stream, node.Size, outputFile, progress);
                }
            });
        }

        public Task DownloadFileAsync(Uri uri, string outputFile, IProgress<int> progress)
        {
            return Task.Run(() =>
            {
                using (Stream stream = this.Download(uri))
                {
                    this.SaveStreamReportProgress(stream, stream.Length, outputFile, progress);
                }
            });
        }

        public Task<INode> UploadAsync(string filename, INode parent, IProgress<int> progress)
        {
            return Task<INode>.Run(() =>
            {
                using (FileStream fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read))
                {
                    Task<INode> task = Task<INode>.Run(() => this.Upload(fileStream, Path.GetFileName(filename), parent));
                    while (task.Status == TaskStatus.Running)
                    {
                        progress.Report((int)(100 * fileStream.Position / fileStream.Length));
                        Thread.Sleep(10);
                    }

                    progress.Report(100);

                    return task;
                }
            });
        }

        public Task<INode> UploadAsync(Stream stream, string name, INode parent, IProgress<int> progress)
        {
            return Task<INode>.Run(() =>
            {
                Task<INode> task = Task<INode>.Run(() => this.Upload(stream, name, parent));
                while (task.Status == TaskStatus.Running)
                {
                    progress.Report((int)(100 * stream.Position / stream.Length));
                    Thread.Sleep(10);
                }

                progress.Report(100);

                return task;
            });
        }

        public Task<INodePublic> GetNodeFromLinkAsync(Uri uri)
        {
            return Task<INodePublic>.Run(() => this.GetNodeFromLink(uri));
        }

        #endregion

        #region Web
        
        private void SaveStreamReportProgress(Stream stream, long dataSize, string outputFile, IProgress<int> progress)
        {
            using (FileStream fs = new FileStream(outputFile, FileMode.CreateNew, FileAccess.Write))
            {
                byte[] buffer = new byte[BufferSize];
                int len;
                while ((len = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    fs.Write(buffer, 0, len);
                    progress.Report((int)(100 * stream.Position / stream.Length));
                }

                progress.Report(100);
            }
        }

        #endregion
    }
}
