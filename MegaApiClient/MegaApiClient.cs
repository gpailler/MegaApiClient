namespace CG.Web.MegaApiClient
{
  using System;
  using System.Collections.Generic;
  using System.Globalization;
  using System.IO;
  using System.Linq;
  using System.Security.Cryptography;
  using System.Text.RegularExpressions;
  using System.Threading;
  using System.Threading.Tasks;

  using CG.Web.MegaApiClient.Serialization;

  using Medo.Security.Cryptography;

  using Newtonsoft.Json;
  using Newtonsoft.Json.Linq;

  public partial class MegaApiClient : IMegaApiClient
  {
    private static readonly Uri BaseApiUri = new Uri("https://g.api.mega.co.nz/cs");
    private static readonly Uri BaseUri = new Uri("https://mega.nz");

    private readonly Options options;
    private readonly IWebClient webClient;

    private readonly Object apiRequestLocker = new Object();

    private Node trashNode;
    private string sessionId;
    private byte[] masterKey;
    private uint sequenceIndex = (uint)(uint.MaxValue * new Random().NextDouble());
    private bool authenticatedLogin;

    #region Constructors

    /// <summary>
    /// Instantiate a new <see cref="MegaApiClient" /> object with default <see cref="Options"/> and default <see cref="IWebClient"/>
    /// </summary>
    public MegaApiClient()
        : this(new Options(), new WebClient())
    {
    }

    /// <summary>
    /// Instantiate a new <see cref="MegaApiClient" /> object with custom <see cref="Options" /> and default <see cref="IWebClient"/>
    /// </summary>
    public MegaApiClient(Options options)
        : this(options, new WebClient())
    {
    }

    /// <summary>
    /// Instantiate a new <see cref="MegaApiClient" /> object with default <see cref="Options" /> and custom <see cref="IWebClient"/>
    /// </summary>
    public MegaApiClient(IWebClient webClient)
        : this(new Options(), webClient)
    {
    }

    /// <summary>
    /// Instantiate a new <see cref="MegaApiClient" /> object with custom <see cref="Options"/> and custom <see cref="IWebClient" />
    /// </summary>
    public MegaApiClient(Options options, IWebClient webClient)
    {
      if (options == null)
      {
        throw new ArgumentNullException(nameof(options));
      }

      if (webClient == null)
      {
        throw new ArgumentNullException(nameof(webClient));
      }

      this.options = options;
      this.webClient = webClient;
      this.webClient.BufferSize = options.BufferSize;
    }

    #endregion

    #region Public API

    /// <summary>
    /// Generate authentication informations and store them in a serializable object to allow persistence
    /// </summary>
    /// <param name="email">email</param>
    /// <param name="password">password</param>
    /// <returns><see cref="AuthInfos" /> object containing encrypted data</returns>
    /// <exception cref="ArgumentNullException">email or password is null</exception>
    public AuthInfos GenerateAuthInfos(string email, string password)
    {
      if (string.IsNullOrEmpty(email))
      {
        throw new ArgumentNullException("email");
      }

      if (string.IsNullOrEmpty(password))
      {
        throw new ArgumentNullException("password");
      }

      // Prelogin to retrieve account version
      PreLoginRequest preLoginRequest = new PreLoginRequest(email);
      PreLoginResponse preLoginResponse = this.Request<PreLoginResponse>(preLoginRequest);

      if (preLoginResponse.Version == 2 && !string.IsNullOrEmpty(preLoginResponse.Salt))
      {
        // Mega uses a new way to hash password based on a salt sent by Mega during prelogin
        var saltBytes = preLoginResponse.Salt.FromBase64();
        var passwordBytes = password.ToBytesPassword();
        const int Iterations = 100000;

        var derivedKeyBytes = new byte[32];
        using (var hmac = new HMACSHA512())
        {
          var pbkdf2 = new Pbkdf2(hmac, passwordBytes, saltBytes, Iterations);
          derivedKeyBytes = pbkdf2.GetBytes(derivedKeyBytes.Length);
        }

        // Derived key contains master key (0-16) and password hash (16-32)
        return new AuthInfos(
          email,
          derivedKeyBytes.Skip(16).ToArray().ToBase64(),
          derivedKeyBytes.Take(16).ToArray());
      }
      else if (preLoginResponse.Version == 1)
      {
        // Retrieve password as UTF8 byte array
        byte[] passwordBytes = password.ToBytesPassword();

        // Encrypt password to use password as key for the hash
        byte[] passwordAesKey = PrepareKey(passwordBytes);

        // Hash email and password to decrypt master key on Mega servers
        string hash = GenerateHash(email.ToLowerInvariant(), passwordAesKey);

        return new AuthInfos(email, hash, passwordAesKey);
      }
      else
      {
        throw new NotSupportedException("Version of account not supported");
      }
    }

    public event EventHandler<ApiRequestFailedEventArgs> ApiRequestFailed;

    public bool IsLoggedIn
    {
      get { return this.sessionId != null; }
    }

    /// <summary>
    /// Login to Mega.co.nz service using email/password credentials
    /// </summary>
    /// <param name="email">email</param>
    /// <param name="password">password</param>
    /// <exception cref="ApiException">Service is not available or credentials are invalid</exception>
    /// <exception cref="ArgumentNullException">email or password is null</exception>
    /// <exception cref="NotSupportedException">Already logged in</exception>
    public LogonSessionToken Login(string email, string password)
    {
      return this.Login(GenerateAuthInfos(email, password));
    }

    /// <summary>
    /// Login to Mega.co.nz service using hashed credentials
    /// </summary>
    /// <param name="authInfos">Authentication informations generated by <see cref="GenerateAuthInfos"/> method</param>
    /// <exception cref="ApiException">Service is not available or authInfos is invalid</exception>
    /// <exception cref="ArgumentNullException">authInfos is null</exception>
    /// <exception cref="NotSupportedException">Already logged in</exception>
    public LogonSessionToken Login(AuthInfos authInfos)
    {
      if (authInfos == null)
      {
        throw new ArgumentNullException("authInfos");
      }

      this.EnsureLoggedOut();
      this.authenticatedLogin = true;

      // Request Mega Api
      LoginRequest request = new LoginRequest(authInfos.Email, authInfos.Hash);
      LoginResponse response = this.Request<LoginResponse>(request);

      // Decrypt master key using our password key
      byte[] cryptedMasterKey = response.MasterKey.FromBase64();
      this.masterKey = Crypto.DecryptKey(cryptedMasterKey, authInfos.PasswordAesKey);

      // Decrypt RSA private key using decrypted master key
      byte[] cryptedRsaPrivateKey = response.PrivateKey.FromBase64();
      BigInteger[] rsaPrivateKeyComponents = Crypto.GetRsaPrivateKeyComponents(cryptedRsaPrivateKey, this.masterKey);

      // Decrypt session id
      byte[] encryptedSid = response.SessionId.FromBase64();
      byte[] sid = Crypto.RsaDecrypt(encryptedSid.FromMPINumber(), rsaPrivateKeyComponents[0], rsaPrivateKeyComponents[1], rsaPrivateKeyComponents[2]);

      // Session id contains only the first 43 bytes
      this.sessionId = sid.Take(43).ToArray().ToBase64();

      return new LogonSessionToken(this.sessionId, this.masterKey);
    }

    public void Login(LogonSessionToken logonSessionToken)
    {
      this.EnsureLoggedOut();
      this.authenticatedLogin = true;

      this.sessionId = logonSessionToken.SessionId;
      this.masterKey = logonSessionToken.MasterKey;
    }

    /// <summary>
    /// Login anonymously to Mega.co.nz service
    /// </summary>
    /// <exception cref="ApiException">Throws if service is not available</exception>
    public void Login()
    {
      this.LoginAnonymous();
    }

    /// <summary>
    /// Login anonymously to Mega.co.nz service
    /// </summary>
    /// <exception cref="ApiException">Throws if service is not available</exception>
    public void LoginAnonymous()
    {
      this.EnsureLoggedOut();
      this.authenticatedLogin = false;

      Random random = new Random();

      // Generate random master key
      this.masterKey = new byte[16];
      random.NextBytes(this.masterKey);

      // Generate a random password used to encrypt the master key
      byte[] passwordAesKey = new byte[16];
      random.NextBytes(passwordAesKey);

      // Generate a random session challenge
      byte[] sessionChallenge = new byte[16];
      random.NextBytes(sessionChallenge);

      byte[] encryptedMasterKey = Crypto.EncryptAes(this.masterKey, passwordAesKey);

      // Encrypt the session challenge with our generated master key
      byte[] encryptedSessionChallenge = Crypto.EncryptAes(sessionChallenge, this.masterKey);
      byte[] encryptedSession = new byte[32];
      Array.Copy(sessionChallenge, 0, encryptedSession, 0, 16);
      Array.Copy(encryptedSessionChallenge, 0, encryptedSession, 16, encryptedSessionChallenge.Length);

      // Request Mega Api to obtain a temporary user handle
      AnonymousLoginRequest request = new AnonymousLoginRequest(encryptedMasterKey.ToBase64(), encryptedSession.ToBase64());
      string userHandle = this.Request(request);

      // Request Mega Api to retrieve our temporary session id
      LoginRequest request2 = new LoginRequest(userHandle, null);
      LoginResponse response2 = this.Request<LoginResponse>(request2);

      this.sessionId = response2.TemporarySessionId;
    }

    /// <summary>
    /// Logout from Mega.co.nz service
    /// </summary>
    /// <exception cref="NotSupportedException">Not logged in</exception>
    public void Logout()
    {
      this.EnsureLoggedIn();

      if (this.authenticatedLogin == true)
      {
        this.Request(new LogoutRequest());
      }

      // Reset values retrieved by Login methods
      this.masterKey = null;
      this.sessionId = null;
    }

    /// <summary>
    /// Retrieve account (quota) information
    /// </summary>
    /// <returns>An object containing account information</returns>
    /// <exception cref="NotSupportedException">Not logged in</exception>
    /// <exception cref="ApiException">Mega.co.nz service reports an error</exception>
    public IAccountInformation GetAccountInformation()
    {
      this.EnsureLoggedIn();

      AccountInformationRequest request = new AccountInformationRequest();
      return this.Request<AccountInformationResponse>(request);
    }

    /// <summary>
    /// Retrieve session history
    /// </summary>
    /// <returns>A collection of sessions</returns>
    /// <exception cref="NotSupportedException">Not logged in</exception>
    /// <exception cref="ApiException">Mega.co.nz service reports an error</exception>
    public IEnumerable<ISession> GetSessionsHistory()
    {
      this.EnsureLoggedIn();

      SessionHistoryRequest request = new SessionHistoryRequest();
      return this.Request<SessionHistoryResponse>(request);
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
      GetNodesResponse response = this.Request<GetNodesResponse>(request, this.masterKey);

      Node[] nodes = response.Nodes;
      if (this.trashNode == null)
      {
        this.trashNode = nodes.First(n => n.Type == NodeType.Trash);
      }

      return nodes.Distinct().OfType<INode>();
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
        this.Move(node, this.trashNode);
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
      byte[] encryptedKey = Crypto.EncryptAes(key, this.masterKey);

      CreateNodeRequest request = CreateNodeRequest.CreateFolderNodeRequest(parent, attributes.ToBase64(), encryptedKey.ToBase64(), key);
      GetNodesResponse response = this.Request<GetNodesResponse>(request, this.masterKey);
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

      this.EnsureLoggedIn();

      if (node.Type == NodeType.Directory)
      {
        // Request an export share on the node or we will receive an AccessDenied
        this.Request(new ShareNodeRequest(node, this.masterKey, this.GetNodes()));

        node = this.GetNodes().First(x => x.Equals(node));
      }

      INodeCrypto nodeCrypto = node as INodeCrypto;
      if (nodeCrypto == null)
      {
        throw new ArgumentException("node must implement INodeCrypto");
      }

      GetDownloadLinkRequest request = new GetDownloadLinkRequest(node);
      string response = this.Request<string>(request);

      return new Uri(BaseUri, string.Format(
          "/#{0}!{1}!{2}",
          node.Type == NodeType.Directory ? "F" : string.Empty,
          response,
          node.Type == NodeType.Directory ? nodeCrypto.SharedKey.ToBase64() : nodeCrypto.FullKey.ToBase64()));
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
    public void DownloadFile(INode node, string outputFile, CancellationToken? cancellationToken = null)
    {
      if (node == null)
      {
        throw new ArgumentNullException("node");
      }

      if (string.IsNullOrEmpty(outputFile))
      {
        throw new ArgumentNullException("outputFile");
      }

      using (Stream stream = this.Download(node, cancellationToken))
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
    public void DownloadFile(Uri uri, string outputFile, CancellationToken? cancellationToken = null)
    {
      if (uri == null)
      {
        throw new ArgumentNullException("uri");
      }

      if (string.IsNullOrEmpty(outputFile))
      {
        throw new ArgumentNullException("outputFile");
      }

      using (Stream stream = this.Download(uri, cancellationToken))
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
    public Stream Download(INode node, CancellationToken? cancellationToken = null)
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

      Stream dataStream = this.webClient.GetRequestRaw(new Uri(downloadResponse.Url));

      Stream resultStream = new MegaAesCtrStreamDecrypter(dataStream, downloadResponse.Size, nodeCrypto.Key, nodeCrypto.Iv, nodeCrypto.MetaMac);

      if (cancellationToken.HasValue)
      {
        resultStream = new CancellableStream(resultStream, cancellationToken.Value);
      }

      return resultStream;
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
    public Stream Download(Uri uri, CancellationToken? cancellationToken = null)
    {
      if (uri == null)
      {
        throw new ArgumentNullException("uri");
      }

      this.EnsureLoggedIn();

      string id;
      byte[] iv, metaMac, key;
      this.GetPartsFromUri(uri, out id, out iv, out metaMac, out key);

      // Retrieve download URL
      DownloadUrlRequestFromId downloadRequest = new DownloadUrlRequestFromId(id);
      DownloadUrlResponse downloadResponse = this.Request<DownloadUrlResponse>(downloadRequest);

      Stream dataStream = this.webClient.GetRequestRaw(new Uri(downloadResponse.Url));

      Stream resultStream = new MegaAesCtrStreamDecrypter(dataStream, downloadResponse.Size, key, iv, metaMac);

      if (cancellationToken.HasValue)
      {
        resultStream = new CancellableStream(resultStream, cancellationToken.Value);
      }

      return resultStream;
    }

    /// <summary>
    /// Retrieve public properties of a file from a specified Uri
    /// </summary>
    /// <param name="uri">Uri to retrive properties</param>
    /// <exception cref="NotSupportedException">Not logged in</exception>
    /// <exception cref="ApiException">Mega.co.nz service reports an error</exception>
    /// <exception cref="ArgumentNullException">uri is null</exception>
    /// <exception cref="ArgumentException">Uri is not valid (id and key are required)</exception>
    public INodeInfo GetNodeFromLink(Uri uri)
    {
      if (uri == null)
      {
        throw new ArgumentNullException("uri");
      }

      this.EnsureLoggedIn();

      string id;
      byte[] iv, metaMac, key;
      this.GetPartsFromUri(uri, out id, out iv, out metaMac, out key);

      // Retrieve attributes
      DownloadUrlRequestFromId downloadRequest = new DownloadUrlRequestFromId(id);
      DownloadUrlResponse downloadResponse = this.Request<DownloadUrlResponse>(downloadRequest);

      return new NodeInfo(id, downloadResponse, key);
    }


    /// <summary>
    /// Retrieve list of nodes from a specified Uri
    /// </summary>
    /// <param name="uri">Uri</param>
    /// <exception cref="NotSupportedException">Not logged in</exception>
    /// <exception cref="ApiException">Mega.co.nz service reports an error</exception>
    /// <exception cref="ArgumentNullException">uri is null</exception>
    /// <exception cref="ArgumentException">Uri is not valid (id and key are required)</exception>
    public IEnumerable<INode> GetNodesFromLink(Uri uri)
    {
      if (uri == null)
      {
        throw new ArgumentNullException("uri");
      }

      this.EnsureLoggedIn();

      string shareId;
      byte[] iv, metaMac, key;
      this.GetPartsFromUri(uri, out shareId, out iv, out metaMac, out key);

      // Retrieve attributes
      GetNodesRequest getNodesRequest = new GetNodesRequest(shareId);
      GetNodesResponse getNodesResponse = this.Request<GetNodesResponse>(getNodesRequest, key);

      return getNodesResponse.Nodes.Select(x => new PublicNode(x, shareId)).OfType<INode>();
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
    public INode UploadFile(string filename, INode parent, CancellationToken? cancellationToken = null)
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

      DateTime modificationDate = File.GetLastWriteTime(filename);
      using (FileStream fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read))
      {
        return this.Upload(fileStream, Path.GetFileName(filename), parent, modificationDate, cancellationToken);
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
    public INode Upload(Stream stream, string name, INode parent, DateTime? modificationDate = null, CancellationToken? cancellationToken = null)
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

      if (cancellationToken.HasValue)
      {
        stream = new CancellableStream(stream, cancellationToken.Value);
      }

      string completionHandle = string.Empty;
      int requestDelay = this.options.ApiRequestDelay;
      int remainingRetry = this.options.ApiRequestAttempts;
      while (remainingRetry-- > 0)
      {
        // Retrieve upload URL
        UploadUrlRequest uploadRequest = new UploadUrlRequest(stream.Length);
        UploadUrlResponse uploadResponse = this.Request<UploadUrlResponse>(uploadRequest);

        ApiResultCode apiResult = ApiResultCode.Ok;
        using (MegaAesCtrStreamCrypter encryptedStream = new MegaAesCtrStreamCrypter(stream))
        {
          long chunkStartPosition = 0;
          var chunksSizesToUpload = this.ComputeChunksSizesToUpload(encryptedStream.ChunksPositions, encryptedStream.Length).ToArray();
          Uri uri = null;
          for (int i = 0; i < chunksSizesToUpload.Length; i++)
          {
            completionHandle = string.Empty;

            int chunkSize = chunksSizesToUpload[i];
            byte[] chunkBuffer = new byte[chunkSize];
            encryptedStream.Read(chunkBuffer, 0, chunkSize);

            using (MemoryStream chunkStream = new MemoryStream(chunkBuffer))
            {
              uri = new Uri(uploadResponse.Url + "/" + chunkStartPosition);
              chunkStartPosition += chunkSize;
              try
              {
                completionHandle = this.webClient.PostRequestRaw(uri, chunkStream);
                if (string.IsNullOrEmpty(completionHandle))
                {
                  apiResult = ApiResultCode.Ok;
                  continue;
                }

                long retCode;
                if (completionHandle.FromBase64().Length != 27 && long.TryParse(completionHandle, out retCode))
                {
                  apiResult = (ApiResultCode)retCode;
                  break;
                }
              }
              catch (Exception ex)
              {
                apiResult = ApiResultCode.RequestFailedRetry;
                this.ApiRequestFailed?.Invoke(this, new ApiRequestFailedEventArgs(uri, remainingRetry, requestDelay, apiResult, ex));

                break;
              }
            }
          }

          if (apiResult != ApiResultCode.Ok)
          {
            this.ApiRequestFailed?.Invoke(this, new ApiRequestFailedEventArgs(uri, remainingRetry, requestDelay, apiResult, completionHandle));

            if (apiResult == ApiResultCode.RequestFailedRetry || apiResult == ApiResultCode.RequestFailedPermanetly || apiResult == ApiResultCode.TooManyRequests)
            {
              // Restart upload from the beginning
              requestDelay = this.Wait(requestDelay);

              // Reset steam position
              stream.Seek(0, SeekOrigin.Begin);

              continue;
            }

            throw new ApiException(apiResult);
          }

          // Encrypt attributes
          byte[] cryptedAttributes = Crypto.EncryptAttributes(new Attributes(name, stream, modificationDate), encryptedStream.FileKey);

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

          byte[] encryptedKey = Crypto.EncryptKey(fileKey, this.masterKey);

          CreateNodeRequest createNodeRequest = CreateNodeRequest.CreateFileNodeRequest(parent, cryptedAttributes.ToBase64(), encryptedKey.ToBase64(), fileKey, completionHandle);
          GetNodesResponse createNodeResponse = this.Request<GetNodesResponse>(createNodeRequest, this.masterKey);
          return createNodeResponse.Nodes[0];
        }
      }

      throw new UploadException(completionHandle);
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

    public INode Rename(INode node, string newName)
    {
      if (node == null)
      {
        throw new ArgumentNullException("node");
      }

      if (node.Type != NodeType.Directory && node.Type != NodeType.File)
      {
        throw new ArgumentException("Invalid node type");
      }

      if (string.IsNullOrEmpty(newName))
      {
        throw new ArgumentNullException("newName");
      }

      INodeCrypto nodeCrypto = node as INodeCrypto;
      if (nodeCrypto == null)
      {
        throw new ArgumentException("node must implement INodeCrypto");
      }

      this.EnsureLoggedIn();

      byte[] encryptedAttributes = Crypto.EncryptAttributes(new Attributes(newName, ((Node)node).Attributes), nodeCrypto.Key);
      this.Request(new RenameRequest(node, encryptedAttributes.ToBase64()));
      return this.GetNodes().First(n => n.Equals(node));
    }

#endregion

#region Private static methods

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
      using (var encryptor = Crypto.CreateAesEncryptor(passwordAesKey))
      {
        for (int it = 0; it < 16384; it++)
        {
          hash = Crypto.EncryptAes(hash, encryptor);
        }
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

#endregion

#region Web

    private string Request(RequestBase request)
    {
      return this.Request<string>(request);
    }

    private TResponse Request<TResponse>(RequestBase request, byte[] key= null)
            where TResponse : class
    {
      if (this.options.SynchronizeApiRequests)
      {
        lock (this.apiRequestLocker)
        {
          return this.RequestCore<TResponse>(request, key);
        }
      }
      else
      {
        return this.RequestCore<TResponse>(request, key);
      }
    }

    private TResponse RequestCore<TResponse>(RequestBase request, byte[] key)
        where TResponse : class
    {
      string dataRequest = JsonConvert.SerializeObject(new object[] { request });
      Uri uri = this.GenerateUrl(request.QueryArguments);
      object jsonData = null;
      int requestDelay = this.options.ApiRequestDelay;
      int remainingRetry = this.options.ApiRequestAttempts;
      while (remainingRetry-- > 0)
      {
        string dataResult = this.webClient.PostRequestJson(uri, dataRequest);

        if (string.IsNullOrEmpty(dataResult)
          || (jsonData = JsonConvert.DeserializeObject(dataResult)) == null
          || jsonData is long
          || (jsonData is JArray && ((JArray)jsonData)[0].Type == JTokenType.Integer))
        {
          ApiResultCode apiCode = jsonData == null
            ? ApiResultCode.RequestFailedRetry
            : jsonData is long
              ?(ApiResultCode)Enum.ToObject(typeof(ApiResultCode), jsonData)
              : (ApiResultCode)((JArray)jsonData)[0].Value<int>();

          if (apiCode != ApiResultCode.Ok)
          {
            this.ApiRequestFailed?.Invoke(this, new ApiRequestFailedEventArgs(uri, this.options.ApiRequestAttempts - remainingRetry, requestDelay, apiCode, dataResult));
          }

          if (apiCode == ApiResultCode.RequestFailedRetry)
          {
            requestDelay = this.Wait(requestDelay);
            continue;
          }

          if (apiCode != ApiResultCode.Ok)
          {
            throw new ApiException(apiCode);
          }
        }

        break;
      }

      string data = ((JArray)jsonData)[0].ToString();
      return (typeof(TResponse) == typeof(string)) ? data as TResponse : JsonConvert.DeserializeObject<TResponse>(data, new GetNodesResponseConverter(key));
    }

    private int Wait(int requestDelay)
    {
      requestDelay = (int) Math.Round(requestDelay * this.options.ApiRequestDelayExponentialFactor);
#if NET40
      Thread.Sleep(requestDelay);
#else
      Task
        .Delay(requestDelay)
        .Wait();
#endif

      return requestDelay;
    }

    private Uri GenerateUrl(Dictionary<string, string> queryArguments)
    {
      var query = new Dictionary<string, string>(queryArguments);
      query["id"] = (this.sequenceIndex++ % uint.MaxValue).ToString(CultureInfo.InvariantCulture);
      query["ak"] = this.options.ApplicationKey;

      if (!string.IsNullOrEmpty(this.sessionId))
      {
        query["sid"] = this.sessionId;
      }

#if NETCORE
      return new Uri(Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(BaseApiUri.AbsoluteUri, query));
#else
      UriBuilder builder = new UriBuilder(BaseApiUri);
      var arguments = System.Web.HttpUtility.ParseQueryString(builder.Query);
      foreach (var item in query)
      {
        arguments.Add(item.Key, item.Value);
      }

      builder.Query = arguments.ToString();
      return builder.Uri;
#endif
    }

    private void SaveStream(Stream stream, string outputFile)
    {
      using (FileStream fs = new FileStream(outputFile, FileMode.CreateNew, FileAccess.Write))
      {
        stream.CopyTo(fs, this.options.BufferSize);
      }
    }

#endregion

#region Private methods

    private void EnsureLoggedIn()
    {
      if (this.sessionId == null)
      {
        throw new NotSupportedException("Not logged in");
      }
    }

    private void EnsureLoggedOut()
    {
      if (this.sessionId != null)
      {
        throw new NotSupportedException("Already logged in");
      }
    }

    private void GetPartsFromUri(Uri uri, out string id, out byte[] iv, out byte[] metaMac, out byte[] key)
    {
      Regex uriRegex = new Regex("#(?<type>F?)!(?<id>.+)!(?<key>.+)");
      Match match = uriRegex.Match(uri.Fragment);
      if (match.Success == false)
      {
        throw new ArgumentException(string.Format("Invalid uri. Unable to extract Id and Key from the uri {0}", uri));
      }

      id = match.Groups["id"].Value;
      byte[] decryptedKey = match.Groups["key"].Value.FromBase64();
      var isFolder = match.Groups["type"].Value == "F";

      if (isFolder)
      {
        iv = null;
        metaMac = null;
        key = decryptedKey;
      }
      else
      {
        Crypto.GetPartsFromDecryptedKey(decryptedKey, out iv, out metaMac, out key);
      }
    }

    private IEnumerable<int> ComputeChunksSizesToUpload(long[] chunksPositions, long streamLength)
    {
      for (int i = 0; i < chunksPositions.Length; i++)
      {
        long currentChunkPosition = chunksPositions[i];
        long nextChunkPosition = i == chunksPositions.Length - 1
          ? streamLength
          : chunksPositions[i + 1];

        // Pack multiple chunks in a single upload
        while (((int)(nextChunkPosition - currentChunkPosition) < this.options.ChunksPackSize || this.options.ChunksPackSize == -1) && i < chunksPositions.Length - 1)
        {
          i++;
          nextChunkPosition = i == chunksPositions.Length - 1
            ? streamLength
            : chunksPositions[i + 1];
        }

        yield return (int)(nextChunkPosition - currentChunkPosition);
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

    public class LogonSessionToken : IEquatable<LogonSessionToken>
    {
      [JsonProperty]
      public string SessionId { get; }

      [JsonProperty]
      public byte[] MasterKey { get; }

      private LogonSessionToken()
      {
      }

      public LogonSessionToken(string sessionId, byte[] masterKey)
      {
        this.SessionId = sessionId;
        this.MasterKey = masterKey;
      }

      public bool Equals(LogonSessionToken other)
      {
        if (other == null)
        {
          return false;
        }

        if (this.SessionId == null || other.SessionId == null || string.Compare(this.SessionId, other.SessionId) != 0)
        {
            return false;
        }

        if (this.MasterKey == null || other.MasterKey == null || !Enumerable.SequenceEqual(MasterKey, other.MasterKey))
        {
            return false;
        }

        return true;
      }
    }

#endregion

  }
}
