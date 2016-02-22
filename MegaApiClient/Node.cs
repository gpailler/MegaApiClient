using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace CG.Web.MegaApiClient
{
    [DebuggerDisplay("Type: {Type} - Name: {Name} - Id: {Id}")]
    internal class Node : NodePublic, INode, INodeCrypto
    {
        private static readonly DateTime OriginalDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);

        private Node()
        {
        }

        #region Public properties

        [JsonProperty("h")]
        public string Id { get; private set; }

        [JsonProperty("p")]
        public string ParentId { get; private set; }

        [JsonProperty("u")]
        public string Owner { get; private set; }

        [JsonProperty("su")]
        public string SharingId { get; private set; }

        [JsonProperty("sk")]
        private string SharingKey { get; set; }

        [JsonProperty("fa")]
        public string SerializedFileAttributes { get; private set; }

        [JsonIgnore]
        public DateTime LastModificationDate { get; private set; }

        [JsonIgnore]
        public byte[] Key { get; private set; }

        [JsonIgnore]
        public byte[] FullKey { get; private set; }

        [JsonIgnore]
        public byte[] SharedKey { get; private set; }

        [JsonIgnore]
        public byte[] Iv { get; private set; }

        [JsonIgnore]
        public byte[] MetaMac { get; private set; }

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
            object[] context = (object[])ctx.Context;
            GetNodesResponse nodesResponse = (GetNodesResponse)context[0];
            if (context.Length == 1)
            {
                // Add key from incoming sharing.
                if (this.SharingKey != null)
                {
                    nodesResponse.SharedKeys.Add(new GetNodesResponse.SharedKey(this.Id, this.SharingKey));
                }
                return;
            }
            else
            {
                byte[] masterKey = (byte[])context[1];

                this.LastModificationDate = OriginalDateTime.AddSeconds(this.SerializedLastModificationDate).ToLocalTime();

                if (this.Type == NodeType.File || this.Type == NodeType.Directory)
                {
                    // There are cases where the SerializedKey property contains multiple keys separated with /
                    // This can occur when a folder is shared and the parent is shared too.
                    // Both keys are working so we use the first one
                    string serializedKey = this.SerializedKey.Split('/')[0];
                    int splitPosition = serializedKey.IndexOf(":", StringComparison.InvariantCulture);
                    byte[] encryptedKey = serializedKey.Substring(splitPosition + 1).FromBase64();

                    // If node is shared, we need to retrieve shared masterkey
                    if (nodesResponse.SharedKeys != null)
                    {
                        string handle = serializedKey.Substring(0, splitPosition);
                        GetNodesResponse.SharedKey sharedKey = nodesResponse.SharedKeys.FirstOrDefault(x => x.Id == handle);
                        if (sharedKey != null)
                        {
                            masterKey = Crypto.DecryptKey(sharedKey.Key.FromBase64(), masterKey);
                            if (this.Type == NodeType.Directory)
                            {
                                this.SharedKey = masterKey;
                            }
                            else
                            {
                                this.SharedKey = Crypto.DecryptKey(encryptedKey, masterKey);
                            }
                        }
                    }

                    this.FullKey = Crypto.DecryptKey(encryptedKey, masterKey);

                    if (this.Type == NodeType.File)
                    {
                        byte[] iv, metaMac, fileKey;
                        Crypto.GetPartsFromDecryptedKey(this.FullKey, out iv, out metaMac, out fileKey);

                        this.Iv = iv;
                        this.MetaMac = metaMac;
                        this.Key = fileKey;
                    }
                    else
                    {
                        this.Key = this.FullKey;
                    }

                    Attributes attributes = Crypto.DecryptAttributes(this.SerializedAttributes.FromBase64(), this.Key);
                    this.Name = attributes.Name;
                }
            }
        }

        #endregion

        #region Equality

        public bool Equals(INode other)
        {
            return other != null && this.Id == other.Id;
        }

        public override int GetHashCode()
        {
            return this.Id.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as INode);
        }

        #endregion
    }

    internal class NodePublic : INodePublic
    {
        public NodePublic(DownloadUrlResponse downloadResponse, byte[] fileKey)
        {
            Attributes attributes = Crypto.DecryptAttributes(downloadResponse.SerializedAttributes.FromBase64(), fileKey);
            this.Name = attributes.Name;
            this.Size = downloadResponse.Size;
            this.Type = NodeType.File;
        }

        protected NodePublic()
        {
        }

        [JsonIgnore]
        public string Name { get; protected set; }

        [JsonProperty("s")]
        public long Size { get; protected set; }

        [JsonProperty("t")]
        public NodeType Type { get; protected set; }
    }
}
