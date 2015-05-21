using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace CG.Web.MegaApiClient
{
    [DebuggerDisplay("Type: {Type} - Name: {Name} - Id: {Id}")]
    internal class Node : INode, INodeCrypto
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

        [JsonProperty("t")]
        public NodeType Type { get; private set; }

        [JsonProperty("s")]
        public long Size { get; private set; }

        [JsonIgnore]
        public string Name { get; private set; }

        [JsonIgnore]
        public DateTime LastModificationDate { get; private set; }

        [JsonIgnore]
        public byte[] DecryptedKey { get; private set; }

        [JsonIgnore]
        public byte[] Key { get; private set; }

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
            byte[] masterKey = (byte[])((object[])ctx.Context)[0];
            GetNodesResponse nodesResponse = (GetNodesResponse)((object[])ctx.Context)[1];

            this.LastModificationDate = OriginalDateTime.AddSeconds(this.SerializedLastModificationDate).ToLocalTime();

            if (this.Type == NodeType.File || this.Type == NodeType.Directory)
            {
                int splitPosition = this.SerializedKey.IndexOf(":", StringComparison.InvariantCulture);
                byte[] encryptedKey = this.SerializedKey.Substring(splitPosition + 1).FromBase64();

                this.DecryptedKey = Crypto.DecryptKey(encryptedKey, masterKey);
                this.Key = this.DecryptedKey;

                // If node is shared, we need to retrieve shared masterkey
                if (nodesResponse.SharedKeys != null)
                {
                    string owner = this.SerializedKey.Substring(0, splitPosition);
                    GetNodesResponse.SharedKey sharedKey = nodesResponse.SharedKeys.FirstOrDefault(x => x.Id == owner);
                    if (sharedKey != null)
                    {
                        masterKey = Crypto.DecryptKey(sharedKey.Key.FromBase64(), masterKey);

                        if (this.Type == NodeType.Directory)
                        {
                            this.DecryptedKey = masterKey;
                        }
                        else
                        {
                            this.DecryptedKey = Crypto.DecryptKey(encryptedKey, masterKey);
                        }

                        this.Key = Crypto.DecryptKey(encryptedKey, masterKey);
                    }
                }

                if (this.Type == NodeType.File)
                {
                    byte[] iv, metaMac, fileKey;
                    Crypto.GetPartsFromDecryptedKey(this.DecryptedKey, out iv, out metaMac, out fileKey);

                    this.Iv = iv;
                    this.MetaMac = metaMac;
                    this.Key = fileKey;
                }

                Attributes attributes = Crypto.DecryptAttributes(this.SerializedAttributes.FromBase64(), this.Key);
                this.Name = attributes.Name;
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

}
