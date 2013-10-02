#region License

/*
The MIT License (MIT)

Copyright (c) 2013 Gregoire Pailler

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
using System.Diagnostics;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace CG.Web.MegaApiClient
{
    #region Base

    internal abstract class RequestBase
    {
        protected RequestBase(string action)
        {
            this.Action = action;
        }

        [JsonProperty("a")]
        public string Action { get; private set; }
    }

    #endregion


    #region Login

    internal class LoginRequest : RequestBase
    {
        public LoginRequest(string userHandle, string passwordHash)
            : base("us")
        {
            this.UserHandle = userHandle;
            this.PasswordHash = passwordHash;
        }

        [JsonProperty("user")]
        public string UserHandle { get; private set; }

        [JsonProperty("uh")]
        public string PasswordHash { get; private set; }
    }

    internal class LoginResponse
    {
        [JsonProperty("csid")]
        public string SessionId { get; private set; }

        [JsonProperty("tsid")]
        public string TemporarySessionId { get; private set; }

        [JsonProperty("privk")]
        public string PrivateKey { get; private set; }

        [JsonProperty("k")]
        public string MasterKey { get; private set; }
    }

    internal class AnonymousLoginRequest : RequestBase
    {
        public AnonymousLoginRequest(string masterKey, string temporarySession)
            : base("up")
        {
            this.MasterKey = masterKey;
            this.TemporarySession = temporarySession;
        }

        [JsonProperty("k")]
        public string MasterKey { get; set; }

        [JsonProperty("ts")]
        public string TemporarySession { get; set; }
    }

    #endregion


    #region Nodes

    internal class GetNodesRequest : RequestBase
    {
        public GetNodesRequest()
            : base("f")
        {
            this.c = 1;
        }

        public int c { get; private set; }
    }

    internal class GetNodesResponse
    {
        [JsonProperty("f")]
        public Node[] Nodes { get; private set; }
    }

    #endregion


    #region Delete

    internal class DeleteRequest : RequestBase
    {
        public DeleteRequest(Node node)
            : base("d")
        {
            this.Node = node.Id;
        }

        [JsonProperty("n")]
        public string Node { get; private set; }
    }

    #endregion


    #region Link

    internal class GetDownloadLinkRequest : RequestBase
    {
        public GetDownloadLinkRequest(Node node)
            : base("l")
        {
            this.Id = node.Id;
        }

        [JsonProperty("n")]
        public string Id { get; private set; }
    }

    #endregion


    #region Create node

    internal class CreateNodeRequest : RequestBase
    {
        private CreateNodeRequest(Node parentNode, NodeType type, string attributes, string key, string completionHandle)
            : base("p")
        {
            this.ParentId = parentNode.Id;
            this.Nodes = new []
                {
                    new CreateNodeRequestData
                        {
                            Attributes = attributes,
                            Key = key,
                            Type = type,
                            CompletionHandle = completionHandle
                        }
                };
        }

        public static CreateNodeRequest CreateFileNodeRequest(Node parentNode, string attributes, string key, string completionHandle)
        {
            return new CreateNodeRequest(parentNode, NodeType.File, attributes, key, completionHandle);
        }

        public static CreateNodeRequest CreateFolderNodeRequest(Node parentNode, string attributes, string key)
        {
            return new CreateNodeRequest(parentNode, NodeType.Directory, attributes, key, "xxxxxxxx");
        }

        [JsonProperty("t")]
        public string ParentId { get; private set; }

        [JsonProperty("n")]
        public CreateNodeRequestData[] Nodes { get; private set; }

        internal class CreateNodeRequestData
        {
            [JsonProperty("h")]
            public string CompletionHandle { get; set; }

            [JsonProperty("t")]
            public NodeType Type { get; set; }

            [JsonProperty("a")]
            public string Attributes { get; set; }

            [JsonProperty("k")]
            public string Key { get; set; }
        }
    }

    #endregion


    #region UploadRequest

    internal class UploadUrlRequest : RequestBase
    {
        public UploadUrlRequest(long fileSize)
            : base("u")
        {
            this.Size = fileSize;
        }

        [JsonProperty("s")]
        public long Size { get; private set; }
    }

    internal class UploadUrlResponse
    {
        [JsonProperty("p")]
        public string Url { get; private set; }
    }

    #endregion


    #region DownloadRequest

    internal class DownloadUrlRequest : RequestBase
    {
        public DownloadUrlRequest(Node node)
            : base("g")
        {
            this.Id = node.Id;
        }

        public int g { get { return 1; } }

        [JsonProperty("n")]
        public string Id { get; private set; }
    }

    internal class DownloadUrlResponse
    {
        [JsonProperty("g")]
        public string Url { get; private set; }

        [JsonProperty("s")]
        public long Size { get; private set; }

        [JsonProperty("at")]
        private string SerializedAttributes { get; set; }
    }

    #endregion

    #region Move

    internal class MoveRequest : RequestBase
    {
        public MoveRequest(Node node, Node destinationParentNode)
            : base("m")
        {
            this.Id = node.Id;
            this.DestinationParentId = destinationParentNode.Id;
        }

        [JsonProperty("n")]
        public string Id { get; private set; }

        [JsonProperty("t")]
        public string DestinationParentId { get; private set; }
    }

    #endregion

    #region Attributes

    internal class Attributes
    {
        public Attributes(string name)
        {
            this.Name = name;
        }

        [JsonProperty("n")]
        public string Name { get; set; }
    }

    #endregion


    #region Node

    [DebuggerDisplay("Type: {Type} - Name: {Name} - Id: {Id}")]
    public class Node : IEquatable<Node>
    {
        private static readonly DateTime OriginalDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);

        #region Public properties

        [JsonProperty("h")]
        public string Id { get; private set; }

        [JsonProperty("p")]
        public string ParentId { get; private set; }

        [JsonProperty("u")]
        public string Owner { get; private set; }

        [JsonProperty("t")]
        public NodeType Type { get; private set; }

        [JsonProperty("s")]
        public long Size { get; private set; }

        [JsonIgnore]
        public string Name { get; private set; }

        [JsonIgnore]
        public DateTime LastModificationDate { get; private set; }

        [JsonIgnore]
        internal byte[] DecryptedFileKey { get; private set; }

        [JsonIgnore]
        internal byte[] Key { get; private set; }

        [JsonIgnore]
        internal byte[] Iv { get; private set; }

        [JsonIgnore]
        internal byte[] MetaMac { get; private set; }

        #endregion

        #region Deserialization

        [JsonProperty("ts")]
        private long SerializedLastModificationDate { get; set; }

        [JsonProperty("a")]
        private string SerializedAttributes { get; set; }

        [JsonProperty("k")]
        private string SerializedKey { get; set; }

        [OnDeserialized]
        public void OnDeserialized(StreamingContext ctx)
        {
            byte[] masterKey = (byte[])ctx.Context;

            byte[] encryptedFileKey = this.SerializedKey.Substring(this.SerializedKey.IndexOf(":", StringComparison.InvariantCulture) + 1).FromBase64();
            this.DecryptedFileKey = Crypto.DecryptKey(encryptedFileKey, masterKey);
            this.LastModificationDate = OriginalDateTime.AddSeconds(this.SerializedLastModificationDate).ToLocalTime();

            if (this.Type == NodeType.File || this.Type == NodeType.Directory)
            {
                this.Key = this.DecryptedFileKey;

                if (this.Type == NodeType.File)
                {
                    // Extract Iv and MetaMac
                    byte[] iv = new byte[8];
                    byte[] metaMac = new byte[8];
                    Array.Copy(this.DecryptedFileKey, 16, iv, 0, 8);
                    Array.Copy(this.DecryptedFileKey, 24, metaMac, 0, 8);
                    this.Iv = iv;
                    this.MetaMac = metaMac;

                    // For files, key is 256 bits long. Compute the key to retrieve 128 AES key
                    this.Key = new byte[16];
                    for (int idx = 0; idx < 16; idx++)
                    {
                        this.Key[idx] = (byte)(this.DecryptedFileKey[idx] ^ this.DecryptedFileKey[idx + 16]);
                    }
                }

                Attributes attributes = Crypto.DecryptAttributes(this.SerializedAttributes.FromBase64(), this.Key);
                this.Name = attributes.Name;
            }
        }

        #endregion

        #region Equality

        public bool Equals(Node other)
        {
            return other != null && this.Id == other.Id;
        }

        #endregion
    }

    public enum NodeType
    {
        File = 0,
        Directory,
        Root,
        Inbox,
        Trash
    }

    #endregion
}
