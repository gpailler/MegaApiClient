﻿#region License

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
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

#if (NET40)
using System.Threading.Tasks;
#endif

namespace CG.Web.MegaApiClient
{
    public class MegaApiClient
    {
        private readonly IWebClient _webClient;

        private const uint BufferSize = 8192;
        private const int ApiRequestAttempts = 10;
        private const int ApiRequestDelay = 200;

        private static readonly Uri BaseApiUri = new Uri("https://g.api.mega.co.nz/cs");
        private static readonly Uri BaseUri = new Uri("https://mega.co.nz");

        private Node _trashNode;

        private AuthInfos _authInfos;
        private string _sessionId;
        private byte[] _masterKey;
        private uint _sequenceIndex = (uint)(uint.MaxValue * new Random().NextDouble());
        private int progress;

        #region Properties
#if (NET40)

        public int Progress
        {
            get
            {
                return progress;
            }
        }
        
#endif
        #endregion Properties

        #region Constructors

        /// <summary>
        /// Instantiate a new <see cref="MegaApiClient" /> object
        /// </summary>
        public MegaApiClient()
            : this(new WebClient())
        {
        }

        /// <summary>
        /// Instantiate a new <see cref="MegaApiClient" /> object with the provided <see cref="IWebClient" />
        /// </summary>
        public MegaApiClient(IWebClient webClient)
        {
            if (webClient == null)
            {
                throw new ArgumentNullException("webClient");
            }

            this._webClient = webClient;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Login to Mega.co.nz service using email/password credentials
        /// </summary>
        /// <param name="email">email</param>
        /// <param name="password">password</param>
        /// <exception cref="ApiException">Service is not available or credentials are invalid</exception>
        /// <exception cref="ArgumentNullException">email or password is null</exception>
        /// <exception cref="NotSupportedException">Already logged in</exception>
        public void Login(string email, string password)
        {
            this.Login(GenerateAuthInfos(email, password));
        }

        /// <summary>
        /// Generate authentication informations and store them in a serializable object to allow persistence
        /// </summary>
        /// <param name="email">email</param>
        /// <param name="password">password</param>
        /// <returns><see cref="AuthInfos" /> object containing encrypted data</returns>
        /// <exception cref="ArgumentNullException">email or password is null</exception>
        public static AuthInfos GenerateAuthInfos(string email, string password)
        {
            if (string.IsNullOrEmpty(email))
            {
                throw new ArgumentNullException("email");
            }

            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentNullException("password");
            }

            // Retrieve password as UTF8 byte array
            byte[] passwordBytes = password.ToBytes();

            // Encrypt password to use password as key for the hash
            byte[] passwordAesKey = PrepareKey(passwordBytes);

            // Hash email and password to decrypt master key on Mega servers
            string hash = GenerateHash(email.ToLowerInvariant(), passwordAesKey);

            return new AuthInfos(email, hash, passwordAesKey);
        }

        /// <summary>
        /// Login to Mega.co.nz service using hashed credentials
        /// </summary>
        /// <param name="authInfos">Authentication informations generated by <see cref="GenerateAuthInfos"/> method</param>
        /// <exception cref="ApiException">Service is not available or authInfos is invalid</exception>
        /// <exception cref="ArgumentNullException">authInfos is null</exception>
        /// <exception cref="NotSupportedException">Already logged in</exception>
        public void Login(AuthInfos authInfos)
        {
            if (authInfos == null)
            {
                throw new ArgumentNullException("authInfos");
            }

            this.EnsureLoggedOut();

            // Store authInfos to relogin if required
            this._authInfos = authInfos;

            // Request Mega Api
            LoginRequest request = new LoginRequest(authInfos.Email, authInfos.Hash);
            LoginResponse response = this.Request<LoginResponse>(request);

            // Decrypt master key using our password key
            byte[] cryptedMasterKey = response.MasterKey.FromBase64();
            this._masterKey = Crypto.DecryptKey(cryptedMasterKey, authInfos.PasswordAesKey);

            // Decrypt RSA private key using decrypted master key
            byte[] cryptedRsaPrivateKey = response.PrivateKey.FromBase64();
            BigInteger[] rsaPrivateKeyComponents = Crypto.GetRsaPrivateKeyComponents(cryptedRsaPrivateKey, this._masterKey);

            // Decrypt session id
            byte[] encryptedSid = response.SessionId.FromBase64();
            byte[] sid = Crypto.RsaDecrypt(encryptedSid.FromMPINumber(), rsaPrivateKeyComponents[0], rsaPrivateKeyComponents[1], rsaPrivateKeyComponents[2]);

            // Session id contains only the first 43 decrypted bytes
            this._sessionId = sid.CopySubArray(43).ToBase64();
        }

        /// <summary>
        /// Login anonymously to Mega.co.nz service
        /// </summary>
        /// <exception cref="ApiException">Throws if service is not available</exception>
        public void LoginAnonymous()
        {
            this.EnsureLoggedOut();

            Random random = new Random();

            // Generate random master key
            this._masterKey = new byte[16];
            random.NextBytes(this._masterKey);

            // Generate a random password used to encrypt the master key
            byte[] passwordAesKey = new byte[16];
            random.NextBytes(passwordAesKey);

            // Generate a random session challenge
            byte[] sessionChallenge = new byte[16];
            random.NextBytes(sessionChallenge);

            byte[] encryptedMasterKey = Crypto.EncryptAes(this._masterKey, passwordAesKey);

            // Encrypt the session challenge with our generated master key
            byte[] encryptedSessionChallenge = Crypto.EncryptAes(sessionChallenge, this._masterKey);
            byte[] encryptedSession = new byte[32];
            Array.Copy(sessionChallenge, 0, encryptedSession, 0, 16);
            Array.Copy(encryptedSessionChallenge, 0, encryptedSession, 16, encryptedSessionChallenge.Length);

            // Request Mega Api to obtain a temporary user handle
            AnonymousLoginRequest request = new AnonymousLoginRequest(encryptedMasterKey.ToBase64(), encryptedSession.ToBase64());
            string userHandle = this.Request(request);

            // Request Mega Api to retrieve our temporary session id
            LoginRequest request2 = new LoginRequest(userHandle, null);
            LoginResponse response2 = this.Request<LoginResponse>(request2);

            this._sessionId = response2.TemporarySessionId;
        }

        /// <summary>
        /// Logout from Mega.co.nz service
        /// </summary>
        /// <exception cref="NotSupportedException">Not logged in</exception>
        public void Logout()
        {
            this.EnsureLoggedIn();

            // Reset values retrieved by Login methods
            this._masterKey = null;
            this._sessionId = null;
        }

        /// <summary>
        /// Retrieve all filesystem nodes
        /// </summary>
        /// <returns>Flat representation of all the filesystem nodes</returns>
        /// <exception cref="NotSupportedException">Not logged in</exception>
        /// <exception cref="ApiException">Mega.co.nz service reports an error</exception>
        public IEnumerable<INode> GetNodes()
        {
            this.EnsureLoggedIn();

            GetNodesRequest request = new GetNodesRequest();
            GetNodesResponse response = this.Request<GetNodesResponse>(request, this._masterKey);

            Node[] nodes = response.Nodes;
            if (this._trashNode == null)
            {
                this._trashNode = nodes.First(n => n.Type == NodeType.Trash);
            }

            return nodes.Distinct().Cast<INode>();
        }
        /// <summary>
        /// Retrieve children nodes of a parent node
        /// </summary>
        /// <returns>Flat representation of children nodes</returns>
        /// <exception cref="NotSupportedException">Not logged in</exception>
        /// <exception cref="ApiException">Mega.co.nz service reports an error</exception>
        /// <exception cref="ArgumentNullException">Parent node is null</exception>
        public IEnumerable<INode> GetNodes(INode parent)
        {
            if (parent == null)
            {
                throw new ArgumentNullException("parent");
            }
            
            return this.GetNodes().Where(n => n.ParentId == parent.Id);
        }

        /// <summary>
        /// Delete a node from the filesytem
        /// </summary>
        /// <remarks>
        /// You can only delete <see cref="NodeType.Directory" /> or <see cref="NodeType.File" /> node
        /// </remarks>
        /// <param name="node">Node to delete</param>
        /// <param name="moveToTrash">Moved to trash if true, Permanently deleted if false</param>
        /// <exception cref="NotSupportedException">Not logged in</exception>
        /// <exception cref="ApiException">Mega.co.nz service reports an error</exception>
        /// <exception cref="ArgumentNullException">node is null</exception>
        /// <exception cref="ArgumentException">node is not a directory or a file</exception>
        public void Delete(INode node, bool moveToTrash = true)
        {
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }

            if (node.Type != NodeType.Directory && node.Type != NodeType.File)
            {
                throw new ArgumentException("Invalid node type");
            }

            this.EnsureLoggedIn();

            if (moveToTrash)
            {
                this.Move(node, this._trashNode);
            }
            else
            {
                this.Request(new DeleteRequest(node));
            }
        }

        /// <summary>
        /// Create a folder on the filesytem
        /// </summary>
        /// <param name="name">Folder name</param>
        /// <param name="parent">Parent node to attach created folder</param>
        /// <exception cref="NotSupportedException">Not logged in</exception>
        /// <exception cref="ApiException">Mega.co.nz service reports an error</exception>
        /// <exception cref="ArgumentNullException">name or parent is null</exception>
        /// <exception cref="ArgumentException">parent is not valid (all types are allowed expect <see cref="NodeType.File" />)</exception>
        public INode CreateFolder(string name, INode parent)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }

            if (parent == null)
            {
                throw new ArgumentNullException("parent");
            }

            if (parent.Type == NodeType.File)
            {
                throw new ArgumentException("Invalid parent node");
            }

            this.EnsureLoggedIn();

            byte[] key = Crypto.CreateAesKey();
            byte[] attributes = Crypto.EncryptAttributes(new Attributes(name), key);
            byte[] encryptedKey = Crypto.EncryptAes(key, this._masterKey);

            CreateNodeRequest request = CreateNodeRequest.CreateFolderNodeRequest(parent, attributes.ToBase64(), encryptedKey.ToBase64(), key);
            GetNodesResponse response = this.Request<GetNodesResponse>(request, this._masterKey);
            return response.Nodes[0];
        }

        /// <summary>
        /// Retrieve an url to download specified node
        /// </summary>
        /// <param name="node">Node to retrieve the download link (only <see cref="NodeType.File" /> or <see cref="NodeType.Directory" /> can be downloaded)</param>
        /// <returns>Download link to retrieve the node with associated key</returns>
        /// <exception cref="NotSupportedException">Not logged in</exception>
        /// <exception cref="ApiException">Mega.co.nz service reports an error</exception>
        /// <exception cref="ArgumentNullException">node is null</exception>
        /// <exception cref="ArgumentException">node is not valid (only <see cref="NodeType.File" /> or <see cref="NodeType.Directory" /> can be downloaded)</exception>
        public Uri GetDownloadLink(INode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }

            if (node.Type != NodeType.File && node.Type != NodeType.Directory)
            {
                throw new ArgumentException("Invalid node");
            }

            INodeCrypto nodeCrypto = node as INodeCrypto;
            if (nodeCrypto == null)
            {
                throw new ArgumentException("node must implement INodeCrypto");
            }

            this.EnsureLoggedIn();

            GetDownloadLinkRequest request = new GetDownloadLinkRequest(node);
            string response = this.Request<string>(request);

            return new Uri(BaseUri, string.Format(
                "/#{0}!{1}!{2}",
                node.Type == NodeType.Directory ? "F" : string.Empty,
                response,
                nodeCrypto.FullKey.ToBase64()));
        }

        /// <summary>
        /// Download a specified node and save it to the specified file
        /// </summary>
        /// <param name="node">Node to download (only <see cref="NodeType.File" /> can be downloaded)</param>
        /// <param name="outputFile">File to save the node to</param>
        /// <exception cref="NotSupportedException">Not logged in</exception>
        /// <exception cref="ApiException">Mega.co.nz service reports an error</exception>
        /// <exception cref="ArgumentNullException">node or outputFile is null</exception>
        /// <exception cref="ArgumentException">node is not valid (only <see cref="NodeType.File" /> can be downloaded)</exception>
        /// <exception cref="DownloadException">Checksum is invalid. Downloaded data are corrupted</exception>
        public void DownloadFile(INode node, string outputFile)
        {
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }

            if (string.IsNullOrEmpty(outputFile))
            {
                throw new ArgumentNullException("outputFile");
            }

            using (Stream stream = this.Download(node))
            {
                this.SaveStream(stream, outputFile);
            }
        }

        /// <summary>
        /// Download a specified Uri from Mega and save it to the specified file
        /// </summary>
        /// <param name="uri">Uri to download</param>
        /// <param name="outputFile">File to save the Uri to</param>
        /// <exception cref="NotSupportedException">Not logged in</exception>
        /// <exception cref="ApiException">Mega.co.nz service reports an error</exception>
        /// <exception cref="ArgumentNullException">uri or outputFile is null</exception>
        /// <exception cref="ArgumentException">Uri is not valid (id and key are required)</exception>
        /// <exception cref="DownloadException">Checksum is invalid. Downloaded data are corrupted</exception>
        public void DownloadFile(Uri uri, string outputFile)
        {
            if (uri == null)
            {
                throw new ArgumentNullException("uri");
            }

            if (string.IsNullOrEmpty(outputFile))
            {
                throw new ArgumentNullException("outputFile");
            }

            using (Stream stream = this.Download(uri))
            {
                this.SaveStream(stream, outputFile);
            }
        }

        /// <summary>
        /// Retrieve a Stream to download and decrypt the specified node
        /// </summary>
        /// <param name="node">Node to download (only <see cref="NodeType.File" /> can be downloaded)</param>
        /// <exception cref="NotSupportedException">Not logged in</exception>
        /// <exception cref="ApiException">Mega.co.nz service reports an error</exception>
        /// <exception cref="ArgumentNullException">node or outputFile is null</exception>
        /// <exception cref="ArgumentException">node is not valid (only <see cref="NodeType.File" /> can be downloaded)</exception>
        /// <exception cref="DownloadException">Checksum is invalid. Downloaded data are corrupted</exception>
        public Stream Download(INode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }

            if (node.Type != NodeType.File)
            {
                throw new ArgumentException("Invalid node");
            }

            INodeCrypto nodeCrypto = node as INodeCrypto;
            if (nodeCrypto == null)
            {
                throw new ArgumentException("node must implement INodeCrypto");
            }

            this.EnsureLoggedIn();

            // Retrieve download URL
            DownloadUrlRequest downloadRequest = new DownloadUrlRequest(node);
            DownloadUrlResponse downloadResponse = this.Request<DownloadUrlResponse>(downloadRequest);

            Stream dataStream = this._webClient.GetRequestRaw(new Uri(downloadResponse.Url));
            return new MegaAesCtrStreamDecrypter(dataStream, downloadResponse.Size, nodeCrypto.Key, nodeCrypto.Iv, nodeCrypto.MetaMac);
        }

        /// <summary>
        /// Retrieve a Stream to download and decrypt the specified Uri
        /// </summary>
        /// <param name="uri">Uri to download</param>
        /// <exception cref="NotSupportedException">Not logged in</exception>
        /// <exception cref="ApiException">Mega.co.nz service reports an error</exception>
        /// <exception cref="ArgumentNullException">uri is null</exception>
        /// <exception cref="ArgumentException">Uri is not valid (id and key are required)</exception>
        /// <exception cref="DownloadException">Checksum is invalid. Downloaded data are corrupted</exception>
        public Stream Download(Uri uri)
        {
            if (uri == null)
            {
                throw new ArgumentNullException("uri");
            }

            this.EnsureLoggedIn();
            
            Regex uriRegex = new Regex("#!(?<id>.+)!(?<key>.+)");
            Match match = uriRegex.Match(uri.Fragment);
            if (match.Success == false)
            {
                throw new ArgumentException(string.Format("Invalid uri. Unable to extract Id and Key from the uri {0}", uri));
            }

            string id = match.Groups["id"].Value;
            byte[] decryptedKey = match.Groups["key"].Value.FromBase64();

            byte[] iv;
            byte[] metaMac;
            byte[] fileKey;
            Crypto.GetPartsFromDecryptedKey(decryptedKey, out iv, out metaMac, out fileKey);

            // Retrieve download URL
            DownloadUrlRequestFromId downloadRequest = new DownloadUrlRequestFromId(id);
            DownloadUrlResponse downloadResponse = this.Request<DownloadUrlResponse>(downloadRequest);

            Stream dataStream = this._webClient.GetRequestRaw(new Uri(downloadResponse.Url));
            return new MegaAesCtrStreamDecrypter(dataStream, downloadResponse.Size, fileKey, iv, metaMac);
        }

        /// <summary>
        /// Upload a file on Mega.co.nz and attach created node to selected parent
        /// </summary>
        /// <param name="filename">File to upload</param>
        /// <param name="parent">Node to attach the uploaded file (all types except <see cref="NodeType.File" /> are supported)</param>
        /// <returns>Created node</returns>
        /// <exception cref="NotSupportedException">Not logged in</exception>
        /// <exception cref="ApiException">Mega.co.nz service reports an error</exception>
        /// <exception cref="ArgumentNullException">filename or parent is null</exception>
        /// <exception cref="FileNotFoundException">filename is not found</exception>
        /// <exception cref="ArgumentException">parent is not valid (all types except <see cref="NodeType.File" /> are supported)</exception>
        public INode Upload(string filename, INode parent)
        {
            if (string.IsNullOrEmpty(filename))
            {
                throw new ArgumentNullException("filename");
            }

            if (parent == null)
            {
                throw new ArgumentNullException("parent");
            }

            if (!File.Exists(filename))
            {
                throw new FileNotFoundException(filename);
            }

            this.EnsureLoggedIn();

            using (FileStream fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                return this.Upload(fileStream, Path.GetFileName(filename), parent);
            }
        }

        /// <summary>
        /// Upload a stream on Mega.co.nz and attach created node to selected parent
        /// </summary>
        /// <param name="stream">Data to upload</param>
        /// <param name="name">Created node name</param>
        /// <param name="parent">Node to attach the uploaded file (all types except <see cref="NodeType.File" /> are supported)</param>
        /// <returns>Created node</returns>
        /// <exception cref="NotSupportedException">Not logged in</exception>
        /// <exception cref="ApiException">Mega.co.nz service reports an error</exception>
        /// <exception cref="ArgumentNullException">stream or name or parent is null</exception>
        /// <exception cref="ArgumentException">parent is not valid (all types except <see cref="NodeType.File" /> are supported)</exception>
        public INode Upload(Stream stream, string name, INode parent)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }

            if (parent == null)
            {
                throw new ArgumentNullException("parent");
            }

            if (parent.Type == NodeType.File)
            {
                throw new ArgumentException("Invalid parent node");
            }

            this.EnsureLoggedIn();

            // Retrieve upload URL
            UploadUrlRequest uploadRequest = new UploadUrlRequest(stream.Length);
            UploadUrlResponse uploadResponse = this.Request<UploadUrlResponse>(uploadRequest);

            using (MegaAesCtrStreamCrypter encryptedStream = new MegaAesCtrStreamCrypter(stream))
            {
                string completionHandle = this._webClient.PostRequestRaw(new Uri(uploadResponse.Url), encryptedStream);

                // Encrypt attributes
                byte[] cryptedAttributes = Crypto.EncryptAttributes(new Attributes(name), encryptedStream.FileKey);

                // Compute the file key
                byte[] fileKey = new byte[32];
                for (int i = 0; i < 8; i++)
                {
                    fileKey[i] = (byte)(encryptedStream.FileKey[i] ^ encryptedStream.Iv[i]);
                    fileKey[i + 16] = encryptedStream.Iv[i];
                }

                for (int i = 8; i < 16; i++)
                {
                    fileKey[i] = (byte)(encryptedStream.FileKey[i] ^ encryptedStream.MetaMac[i - 8]);
                    fileKey[i + 16] = encryptedStream.MetaMac[i - 8];
                }

                byte[] encryptedKey = Crypto.EncryptKey(fileKey, this._masterKey);

                CreateNodeRequest createNodeRequest = CreateNodeRequest.CreateFileNodeRequest(parent, cryptedAttributes.ToBase64(), encryptedKey.ToBase64(), fileKey, completionHandle);
                GetNodesResponse createNodeResponse = this.Request<GetNodesResponse>(createNodeRequest, this._masterKey);
                return createNodeResponse.Nodes[0];
            }
        }

        /// <summary>
        /// Change node parent
        /// </summary>
        /// <param name="node">Node to move</param>
        /// <param name="destinationParentNode">New parent</param>
        /// <returns>Moved node</returns>
        /// <exception cref="NotSupportedException">Not logged in</exception>
        /// <exception cref="ApiException">Mega.co.nz service reports an error</exception>
        /// <exception cref="ArgumentNullException">node or destinationParentNode is null</exception>
        /// <exception cref="ArgumentException">node is not valid (only <see cref="NodeType.Directory" /> and <see cref="NodeType.File" /> are supported)</exception>
        /// <exception cref="ArgumentException">parent is not valid (all types except <see cref="NodeType.File" /> are supported)</exception>
        public INode Move(INode node, INode destinationParentNode)
        {
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }

            if (destinationParentNode == null)
            {
                throw new ArgumentNullException("destinationParentNode");
            }

            if (node.Type != NodeType.Directory && node.Type != NodeType.File)
            {
                throw new ArgumentException("Invalid node type");
            }

            if (destinationParentNode.Type == NodeType.File)
            {
                throw new ArgumentException("Invalid destination parent node");
            }

            this.EnsureLoggedIn();

            this.Request(new MoveRequest(node, destinationParentNode));
            return this.GetNodes().First(n => n.Equals(node));
        }

#if(NET40)

        /// <summary>
        /// Retrieve a Stream to download and decrypt the specified Uri
        /// </summary>
        /// <param name="uri">Uri to download</param>
        /// <param name="dataSize">Fill</param>
        /// <exception cref="NotSupportedException">Not logged in</exception>
        /// <exception cref="ApiException">Mega.co.nz service reports an error</exception>
        /// <exception cref="ArgumentNullException">uri is null</exception>
        /// <exception cref="ArgumentException">Uri is not valid (id and key are required)</exception>
        /// <exception cref="DownloadException">Checksum is invalid. Downloaded data are corrupted</exception>
        public Stream Download(Uri uri, ref long dataSize)
        {
            if (uri == null)
            {
                throw new ArgumentNullException("uri");
            }

            this.EnsureLoggedIn();

            Regex uriRegex = new Regex("#!(?<id>.+)!(?<key>.+)");
            Match match = uriRegex.Match(uri.Fragment);
            if (match.Success == false)
            {
                throw new ArgumentException(string.Format("Invalid uri. Unable to extract Id and Key from the uri {0}", uri));
            }

            string id = match.Groups["id"].Value;
            byte[] decryptedKey = match.Groups["key"].Value.FromBase64();

            byte[] iv;
            byte[] metaMac;
            byte[] fileKey;
            Crypto.GetPartsFromDecryptedKey(decryptedKey, out iv, out metaMac, out fileKey);

            // Retrieve download URL
            DownloadUrlRequestFromId downloadRequest = new DownloadUrlRequestFromId(id);
            DownloadUrlResponse downloadResponse = this.Request<DownloadUrlResponse>(downloadRequest);

            Stream dataStream = this._webClient.GetRequestRaw(new Uri(downloadResponse.Url));
            dataSize = downloadResponse.Size;
            return new MegaAesCtrStreamDecrypter(dataStream, downloadResponse.Size, fileKey, iv, metaMac);
        }

#endif

        #endregion

        #region Public async methods
#if (NET40)


        public Task LoginAsync(string email, string password)
        {
            return LoginAsync(GenerateAuthInfos(email, password));
        }

        public Task LoginAsync(AuthInfos authInfos)
        {
            return Task.Run(() => Login(authInfos));
        }

        public Task LoginAnonymousAsync()
        {
            return Task.Run(() => LoginAnonymous());
        }

        public Task LogoutAsync()
        {
            return Task.Run(() => Logout());
        }

        public Task<IEnumerable<INode>> GetNodesAsync()
        {
            return Task<IEnumerable<INode>>.Run(() => GetNodes());
        }

        public Task<IEnumerable<INode>> GetNodesAsync(INode parent)
        {
            return Task<IEnumerable<INode>>.Run(() => GetNodes(parent));
        }

        public Task<INode> CreateFolderAsync(string name, INode parent)
        {
            return Task<INode>.Run(() => CreateFolder(name, parent));
        }

        public Task DeleteAsync(INode node, bool moveToTrash = true)
        {
            return Task.Run(() => Delete(node, moveToTrash));
        }

        public Task<INode> MoveAsync(INode sourceNode, INode destinationParentNode)
        {
            return Task<INode>.Run(() => Move(sourceNode, destinationParentNode));
        }

        public Task<Uri> GetDownloadLinkAsync(INode node)
        {
            return Task<Uri>.Run(() => GetDownloadLink(node));
        }

        public Task DownloadFileAsync(INode node, string outputFile)
        {
            return Task.Run(() =>
            {
                if (node == null)
                {
                    throw new ArgumentNullException("node");
                }

                if (string.IsNullOrEmpty(outputFile))
                {
                    throw new ArgumentNullException("outputFile");
                }

                using (Stream stream = this.Download(node))
                {
                    SaveStreamReportProgress(stream, node.Size, outputFile);
                }
            });
        }

        public Task DownloadFileAsync(Uri uri, string outputFile)
        {
            return Task.Run(() =>
            {
                long dataSize = 0;
                if (uri == null)
                {
                    throw new ArgumentNullException("uri");
                }

                if (string.IsNullOrEmpty(outputFile))
                {
                    throw new ArgumentNullException("outputFile");
                }

                using (Stream stream = this.Download(uri, ref dataSize))
                {
                    SaveStreamReportProgress(stream, dataSize, outputFile);
                }
            });
        }

        public Task<INode> UploadAsync(string filename, INode parent)
        {
            return Task<INode>.Run(() =>
            {
                if (string.IsNullOrEmpty(filename))
                {
                    throw new ArgumentNullException("filename");
                }

                if (parent == null)
                {
                    throw new ArgumentNullException("parent");
                }

                if (!File.Exists(filename))
                {
                    throw new FileNotFoundException(filename);
                }

                this.EnsureLoggedIn();

                using (FileStream fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read))
                {
                    return this.UploadAsync(fileStream, Path.GetFileName(filename), parent);
                }
            });
        }

        public Task<INode> UploadAsync(Stream stream, string name, INode parent)
        {
            return Task<INode>.Run(() =>
            {
                Task<INode> task = Task<INode>.Run(() => Upload(stream, name, parent));
                while (task.Status == TaskStatus.Running)
                {
                    progress = (int)(100 * stream.Position / stream.Length);
                }

                return task;
            });
        }


#endif
        #endregion

        #region Web

        private string Request(RequestBase request)
        {
            return this.Request<string>(request);
        }

        private TResponse Request<TResponse>(RequestBase request, object context = null)
            where TResponse : class
        {
            string dataRequest = JsonConvert.SerializeObject(new object[] { request });
            Uri uri = this.GenerateUrl();
            object jsonData = null;
            int currentAttempt = 0;
            while (true)
            {
                string dataResult = this._webClient.PostRequestJson(uri, dataRequest);

                jsonData = JsonConvert.DeserializeObject(dataResult);
                if (jsonData is long || (jsonData is JArray && ((JArray)jsonData)[0].Type == JTokenType.Integer))
                {
                    ApiResultCode apiCode = (jsonData is long)
                                                ? (ApiResultCode)Enum.ToObject(typeof(ApiResultCode), jsonData)
                                                : (ApiResultCode)((JArray)jsonData)[0].Value<int>();

                    if (apiCode == ApiResultCode.RequestFailedRetry)
                    {
                        if (currentAttempt == ApiRequestAttempts)
                        {
                            throw new NotSupportedException("Api not available");
                        }

                        Thread.Sleep(ApiRequestDelay);
                        currentAttempt++;
                        continue;
                    }

                    if (apiCode == ApiResultCode.BadSessionId && this._authInfos != null)
                    {
                        this.Logout();
                        this.Login(this._authInfos);
                    }

                    if (apiCode != ApiResultCode.Ok)
                    {
                        throw new ApiException(apiCode);
                    }
                }

                break;
            }

            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.Context = new StreamingContext(StreamingContextStates.All, context);

            string data = ((JArray)jsonData)[0].ToString();
            return (typeof(TResponse) == typeof(string)) ? data as TResponse : JsonConvert.DeserializeObject<TResponse>(data, settings);
        }

        private Uri GenerateUrl()
        {
            UriBuilder builder = new UriBuilder(BaseApiUri);
            NameValueCollection query = HttpUtility.ParseQueryString(builder.Query);
            query["id"] = (_sequenceIndex++ % uint.MaxValue).ToString(CultureInfo.InvariantCulture);

            if (!string.IsNullOrEmpty(this._sessionId))
            {
                query["sid"] = this._sessionId;
            }

            builder.Query = query.ToString();
            return builder.Uri;
        }

        private void SaveStream(Stream stream, string outputFile)
        {
            using (FileStream fs = new FileStream(outputFile, FileMode.CreateNew, FileAccess.Write))
            {
                byte[] buffer = new byte[BufferSize];
                int len;
                while ((len = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    fs.Write(buffer, 0, len);
                }
            }
        }

        #endregion

        #region Private methods

        private static string GenerateHash(string email, byte[] passwordAesKey)
        {
            byte[] emailBytes = email.ToBytes();
            byte[] hash = new byte[16];

            // Compute email in 16 bytes array
            for (int i = 0; i < emailBytes.Length; i++)
            {
                hash[i % 16] ^= emailBytes[i];
            }

            // Encrypt hash using password key
            for (int it = 0; it < 16384; it++)
            {
                hash = Crypto.EncryptAes(hash, passwordAesKey);
            }

            // Retrieve bytes 0-4 and 8-12 from the hash
            byte[] result = new byte[8];
            Array.Copy(hash, 0, result, 0, 4);
            Array.Copy(hash, 8, result, 4, 4);

            return result.ToBase64();
        }

        private static byte[] PrepareKey(byte[] data)
        {
            byte[] pkey = new byte[] { 0x93, 0xC4, 0x67, 0xE3, 0x7D, 0xB0, 0xC7, 0xA4, 0xD1, 0xBE, 0x3F, 0x81, 0x01, 0x52, 0xCB, 0x56 };

            for (int it = 0; it < 65536; it++)
            {
                for (int idx = 0; idx < data.Length; idx += 16)
                {
                    // Pad the data to 16 bytes blocks
                    byte[] key = data.CopySubArray(16, idx);

                    pkey = Crypto.EncryptAes(pkey, key);
                }
            }

            return pkey;
        }

        private void EnsureLoggedIn()
        {
            if (this._sessionId == null)
            {
                throw new NotSupportedException("Not logged in");
            }
        }

        private void EnsureLoggedOut()
        {
            if (this._sessionId != null)
            {
                throw new NotSupportedException("Already logged in");
            }
        }

        #endregion

        #region AuthInfos

        public class AuthInfos
        {
            public AuthInfos(string email, string hash, byte[] passwordAesKey)
            {
                this.Email = email;
                this.Hash = hash;
                this.PasswordAesKey = passwordAesKey;
            }

            [JsonProperty]
            public string Email { get; private set; }

            [JsonProperty]
            public string Hash { get; private set; }

            [JsonProperty]
            public byte[] PasswordAesKey { get; private set; }
        }

        #endregion
    }
}
